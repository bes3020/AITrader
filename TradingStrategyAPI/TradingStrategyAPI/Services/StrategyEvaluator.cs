using System.Text.RegularExpressions;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for evaluating trading strategy conditions and calculating technical indicators.
/// </summary>
public partial class StrategyEvaluator : IStrategyEvaluator
{
    private readonly ILogger<StrategyEvaluator> _logger;
    private readonly IErrorTracker _errorTracker;

    private const decimal EqualityTolerance = 0.01m;

    public StrategyEvaluator(ILogger<StrategyEvaluator> logger, IErrorTracker errorTracker)
    {
        _logger = logger;
        _errorTracker = errorTracker;
    }

    /// <inheritdoc/>
    public bool EvaluateEntry(Strategy strategy, Bar currentBar, List<Bar> historicalBars)
    {
        try
        {
            if (strategy.EntryConditions == null || !strategy.EntryConditions.Any())
            {
                _logger.LogWarning("Strategy {StrategyId} has no entry conditions", strategy.Id);
                return false;
            }

            if (!historicalBars.Any())
            {
                _logger.LogWarning("No historical bars provided for evaluation");
                return false;
            }

            _logger.LogDebug("Evaluating {Count} entry conditions for strategy {StrategyId} at {Timestamp}",
                strategy.EntryConditions.Count, strategy.Id, currentBar.Timestamp);

            // All conditions must pass (AND logic)
            foreach (var condition in strategy.EntryConditions)
            {
                var result = EvaluateCondition(condition, currentBar, historicalBars);

                _logger.LogDebug("Condition {Indicator} {Operator} {Value}: {Result}",
                    condition.Indicator, condition.Operator, condition.Value, result ? "PASS" : "FAIL");

                if (!result)
                {
                    return false; // Early exit on first failure
                }
            }

            _logger.LogInformation("All entry conditions passed for strategy {StrategyId} at {Timestamp}",
                strategy.Id, currentBar.Timestamp);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating entry conditions for strategy {StrategyId}", strategy.Id);

            // Log error to database for analysis
            _ = _errorTracker.LogErrorAsync(
                "Evaluation",
                $"Error evaluating entry conditions: {ex.Message}",
                ex,
                strategy.Id,
                context: new Dictionary<string, object>
                {
                    ["Timestamp"] = currentBar.Timestamp.ToString("O"),
                    ["Symbol"] = currentBar.Symbol,
                    ["ConditionCount"] = strategy.EntryConditions?.Count ?? 0
                }
            );

            return false;
        }
    }

    /// <inheritdoc/>
    public decimal CalculateIndicator(string indicator, List<Bar> bars, int period)
    {
        return indicator.ToLowerInvariant() switch
        {
            "rsi" => CalculateRSI(bars, period),
            "atr" => CalculateATR(bars, period),
            _ => throw new ArgumentException($"Unsupported indicator: {indicator}")
        };
    }

    /// <summary>
    /// Evaluates a single condition against the current market state.
    /// </summary>
    private bool EvaluateCondition(Condition condition, Bar currentBar, List<Bar> historicalBars)
    {
        try
        {
            var leftValue = GetIndicatorValue(condition.Indicator, currentBar, historicalBars);
            var rightValue = GetCompareValue(condition.Value, currentBar, historicalBars);

            _logger.LogDebug("Comparing {Indicator}={LeftValue} {Operator} {Value}={RightValue}",
                condition.Indicator, leftValue, condition.Operator, condition.Value, rightValue);

            return condition.Operator switch
            {
                ">" => leftValue > rightValue,
                "<" => leftValue < rightValue,
                ">=" => leftValue >= rightValue,
                "<=" => leftValue <= rightValue,
                "=" => Math.Abs(leftValue - rightValue) <= EqualityTolerance,
                "crosses_above" => EvaluateCrossesAbove(condition.Indicator, condition.Value, currentBar, historicalBars),
                "crosses_below" => EvaluateCrossesBelow(condition.Indicator, condition.Value, currentBar, historicalBars),
                _ => throw new ArgumentException($"Unsupported operator: {condition.Operator}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating condition: {Indicator} {Operator} {Value}",
                condition.Indicator, condition.Operator, condition.Value);

            // Log error to database for analysis with failed expression
            var failedExpression = $"{condition.Indicator} {condition.Operator} {condition.Value}";
            _ = _errorTracker.LogErrorAsync(
                "Evaluation",
                $"Error evaluating condition: {failedExpression}",
                ex,
                condition.StrategyId,
                failedExpression: condition.Value, // The problematic value that failed
                context: new Dictionary<string, object>
                {
                    ["Indicator"] = condition.Indicator,
                    ["Operator"] = condition.Operator,
                    ["FullCondition"] = failedExpression
                }
            );

            return false;
        }
    }

    /// <summary>
    /// Gets the value of an indicator for the current bar.
    /// </summary>
    private decimal GetIndicatorValue(string indicator, Bar currentBar, List<Bar> historicalBars)
    {
        var indicatorLower = indicator.ToLowerInvariant();

        // Basic price and volume indicators
        if (indicatorLower is "price" or "close") return currentBar.Close;
        if (indicatorLower == "open") return currentBar.Open;
        if (indicatorLower == "high") return currentBar.High;
        if (indicatorLower == "low") return currentBar.Low;
        if (indicatorLower == "volume") return currentBar.Volume;
        if (indicatorLower == "vwap") return currentBar.Vwap;

        // Pre-calculated EMAs
        if (indicatorLower is "ema_9" or "ema9") return currentBar.Ema9;
        if (indicatorLower is "ema_20" or "ema20") return currentBar.Ema20;
        if (indicatorLower is "ema_50" or "ema50") return currentBar.Ema50;

        // Single-value indicators
        if (indicatorLower == "rsi") return CalculateRSI(historicalBars, 14);
        if (indicatorLower == "atr") return CalculateATR(historicalBars, 14);
        if (indicatorLower == "adx") return CalculateADX(historicalBars, 14);
        if (indicatorLower == "cci") return CalculateCCI(historicalBars, 20);
        if (indicatorLower == "williams_r" || indicatorLower == "williamsr") return CalculateWilliamsR(historicalBars, 14);
        if (indicatorLower == "obv") return CalculateOBV(historicalBars);

        // Bollinger Bands
        if (indicatorLower.StartsWith("bb_"))
        {
            var bb = CalculateBollingerBands(historicalBars, 20, 2m, "close");
            return indicatorLower switch
            {
                "bb_upper" => bb.upper,
                "bb_middle" => bb.middle,
                "bb_lower" => bb.lower,
                _ => throw new ArgumentException($"Unsupported Bollinger Band component: {indicator}")
            };
        }

        // MACD
        if (indicatorLower.StartsWith("macd_"))
        {
            var macd = CalculateMACD(historicalBars);
            return indicatorLower switch
            {
                "macd_line" or "macd" => macd.macd,
                "macd_signal" or "macd_sig" => macd.signal,
                "macd_histogram" or "macd_hist" => macd.histogram,
                _ => throw new ArgumentException($"Unsupported MACD component: {indicator}")
            };
        }

        // Stochastic
        if (indicatorLower.StartsWith("stoch_"))
        {
            var stoch = CalculateStochastic(historicalBars);
            return indicatorLower switch
            {
                "stoch_k" => stoch.k,
                "stoch_d" => stoch.d,
                _ => throw new ArgumentException($"Unsupported Stochastic component: {indicator}")
            };
        }

        // Ichimoku Cloud
        if (indicatorLower.StartsWith("ichimoku_"))
        {
            var ichimoku = CalculateIchimoku(historicalBars);
            return indicatorLower switch
            {
                "ichimoku_tenkan" => ichimoku.Tenkan,
                "ichimoku_kijun" => ichimoku.Kijun,
                "ichimoku_senkou_a" or "ichimoku_senkoua" => ichimoku.SenkouA,
                "ichimoku_senkou_b" or "ichimoku_senkoub" => ichimoku.SenkouB,
                "ichimoku_chikou" => ichimoku.Chikou,
                _ => throw new ArgumentException($"Unsupported Ichimoku component: {indicator}")
            };
        }

        // Parabolic SAR
        if (indicatorLower == "psar" || indicatorLower == "parabolic_sar")
        {
            var sarValues = CalculateParabolicSAR(historicalBars);
            return sarValues.Last();
        }

        // Previous day high/low
        if (indicatorLower == "prev_day_high") return GetPreviousDayHigh(currentBar, historicalBars);
        if (indicatorLower == "prev_day_low") return GetPreviousDayLow(currentBar, historicalBars);

        // Time
        if (indicatorLower == "time") return GetTimeInMinutes(currentBar.Timestamp);

        throw new ArgumentException($"Unsupported indicator: {indicator}");
    }

    /// <summary>
    /// Parses and resolves the comparison value, which can be a number, indicator, or special format.
    /// </summary>
    private decimal GetCompareValue(string value, Bar currentBar, List<Bar> historicalBars)
    {
        // Try parsing as a direct number
        if (decimal.TryParse(value, out var numericValue))
        {
            return numericValue;
        }

        // Check for multiplier format (e.g., "1.5x_average")
        var multiplierMatch = MultiplierRegex().Match(value);
        if (multiplierMatch.Success)
        {
            var multiplier = decimal.Parse(multiplierMatch.Groups[1].Value);
            var baseValue = multiplierMatch.Groups[2].Value.ToLowerInvariant();

            return baseValue switch
            {
                "average" or "avg_volume" or "avgvolume20" => multiplier * currentBar.AvgVolume20,
                "vwap" => multiplier * currentBar.Vwap,
                _ => throw new ArgumentException($"Unsupported multiplier base: {baseValue}")
            };
        }

        // Check for time format (e.g., "10:00")
        var timeMatch = TimeRegex().Match(value);
        if (timeMatch.Success)
        {
            var hours = int.Parse(timeMatch.Groups[1].Value);
            var minutes = int.Parse(timeMatch.Groups[2].Value);
            return hours * 60 + minutes; // Convert to minutes since midnight
        }

        // Try resolving as an indicator name
        try
        {
            return GetIndicatorValue(value, currentBar, historicalBars);
        }
        catch (Exception ex)
        {
            // Enhanced error message for failed value resolution
            var errorMsg = $"Cannot resolve value: {value}";
            _logger.LogError(ex, errorMsg);
            throw new ArgumentException(errorMsg, ex);
        }
    }

    /// <summary>
    /// Evaluates if indicator crosses above the compare value.
    /// </summary>
    private bool EvaluateCrossesAbove(string indicator, string compareValue, Bar currentBar, List<Bar> historicalBars)
    {
        if (historicalBars.Count < 2)
        {
            return false;
        }

        var previousBar = historicalBars[^2];

        var currentIndicator = GetIndicatorValue(indicator, currentBar, historicalBars);
        var previousIndicator = GetIndicatorValue(indicator, previousBar, historicalBars.Take(historicalBars.Count - 1).ToList());

        var currentCompare = GetCompareValue(compareValue, currentBar, historicalBars);
        var previousCompare = GetCompareValue(compareValue, previousBar, historicalBars.Take(historicalBars.Count - 1).ToList());

        // Was below or equal, now above
        return previousIndicator <= previousCompare && currentIndicator > currentCompare;
    }

    /// <summary>
    /// Evaluates if indicator crosses below the compare value.
    /// </summary>
    private bool EvaluateCrossesBelow(string indicator, string compareValue, Bar currentBar, List<Bar> historicalBars)
    {
        if (historicalBars.Count < 2)
        {
            return false;
        }

        var previousBar = historicalBars[^2];

        var currentIndicator = GetIndicatorValue(indicator, currentBar, historicalBars);
        var previousIndicator = GetIndicatorValue(indicator, previousBar, historicalBars.Take(historicalBars.Count - 1).ToList());

        var currentCompare = GetCompareValue(compareValue, currentBar, historicalBars);
        var previousCompare = GetCompareValue(compareValue, previousBar, historicalBars.Take(historicalBars.Count - 1).ToList());

        // Was above or equal, now below
        return previousIndicator >= previousCompare && currentIndicator < currentCompare;
    }

    /// <summary>
    /// Calculates the Relative Strength Index (RSI).
    /// </summary>
    private decimal CalculateRSI(List<Bar> bars, int period)
    {
        if (bars.Count < period + 1)
        {
            _logger.LogWarning("Insufficient bars for RSI calculation. Need {Required}, have {Actual}",
                period + 1, bars.Count);
            return 50m; // Neutral RSI
        }

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        // Calculate price changes
        for (int i = 1; i < bars.Count; i++)
        {
            var change = bars[i].Close - bars[i - 1].Close;
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? Math.Abs(change) : 0);
        }

        // Use only the last 'period' changes
        var recentGains = gains.TakeLast(period).ToList();
        var recentLosses = losses.TakeLast(period).ToList();

        var avgGain = recentGains.Average();
        var avgLoss = recentLosses.Average();

        if (avgLoss == 0)
        {
            return 100m; // No losses, RSI = 100
        }

        var rs = avgGain / avgLoss;
        var rsi = 100m - (100m / (1m + rs));

        _logger.LogDebug("RSI({Period}) = {RSI:F2} (AvgGain={AvgGain:F4}, AvgLoss={AvgLoss:F4})",
            period, rsi, avgGain, avgLoss);

        return rsi;
    }

    /// <summary>
    /// Calculates the Average True Range (ATR).
    /// </summary>
    private decimal CalculateATR(List<Bar> bars, int period)
    {
        if (bars.Count < period + 1)
        {
            _logger.LogWarning("Insufficient bars for ATR calculation. Need {Required}, have {Actual}",
                period + 1, bars.Count);
            return 0m;
        }

        var trueRanges = new List<decimal>();

        // Calculate True Range for each bar
        for (int i = 1; i < bars.Count; i++)
        {
            var currentBar = bars[i];
            var previousClose = bars[i - 1].Close;

            var tr1 = currentBar.High - currentBar.Low;
            var tr2 = Math.Abs(currentBar.High - previousClose);
            var tr3 = Math.Abs(currentBar.Low - previousClose);

            var trueRange = Math.Max(tr1, Math.Max(tr2, tr3));
            trueRanges.Add(trueRange);
        }

        // Use only the last 'period' true ranges
        var recentTR = trueRanges.TakeLast(period).ToList();
        var atr = recentTR.Average();

        _logger.LogDebug("ATR({Period}) = {ATR:F2}", period, atr);

        return atr;
    }

    /// <summary>
    /// Gets the previous trading day's high.
    /// </summary>
    private decimal GetPreviousDayHigh(Bar currentBar, List<Bar> historicalBars)
    {
        var currentDate = currentBar.Timestamp.Date;
        var previousDayBars = historicalBars
            .Where(b => b.Timestamp.Date < currentDate)
            .OrderByDescending(b => b.Timestamp)
            .ToList();

        if (!previousDayBars.Any())
        {
            _logger.LogWarning("No previous day data found for {Date}", currentDate);
            return currentBar.High;
        }

        var previousDay = previousDayBars.First().Timestamp.Date;
        var previousDayData = previousDayBars.Where(b => b.Timestamp.Date == previousDay).ToList();

        return previousDayData.Max(b => b.High);
    }

    /// <summary>
    /// Gets the previous trading day's low.
    /// </summary>
    private decimal GetPreviousDayLow(Bar currentBar, List<Bar> historicalBars)
    {
        var currentDate = currentBar.Timestamp.Date;
        var previousDayBars = historicalBars
            .Where(b => b.Timestamp.Date < currentDate)
            .OrderByDescending(b => b.Timestamp)
            .ToList();

        if (!previousDayBars.Any())
        {
            _logger.LogWarning("No previous day data found for {Date}", currentDate);
            return currentBar.Low;
        }

        var previousDay = previousDayBars.First().Timestamp.Date;
        var previousDayData = previousDayBars.Where(b => b.Timestamp.Date == previousDay).ToList();

        return previousDayData.Min(b => b.Low);
    }

    /// <summary>
    /// Gets the time in minutes since midnight.
    /// </summary>
    private static decimal GetTimeInMinutes(DateTime timestamp)
    {
        return timestamp.Hour * 60 + timestamp.Minute;
    }

    /// <summary>
    /// Regex for parsing multiplier format (e.g., "1.5x_average").
    /// </summary>
    [GeneratedRegex(@"^([\d.]+)x_(\w+)$", RegexOptions.IgnoreCase)]
    private static partial Regex MultiplierRegex();

    /// <summary>
    /// Regex for parsing time format (e.g., "10:00").
    /// </summary>
    [GeneratedRegex(@"^(\d{1,2}):(\d{2})$")]
    private static partial Regex TimeRegex();

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    /// <summary>
    /// Calculates the standard deviation of a list of values.
    /// </summary>
    private static decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (values.Count == 0) return 0m;

        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        return (decimal)Math.Sqrt((double)(sumOfSquares / values.Count));
    }

    /// <summary>
    /// Calculates the mean deviation of a list of values.
    /// </summary>
    private static decimal CalculateMeanDeviation(List<decimal> values)
    {
        if (values.Count == 0) return 0m;

        var avg = values.Average();
        return values.Sum(v => Math.Abs(v - avg)) / values.Count;
    }

    /// <summary>
    /// Gets the highest and lowest values over a specified period.
    /// </summary>
    private static (decimal highest, decimal lowest) GetHighLowRange(List<Bar> bars, int period)
    {
        if (bars.Count < period) period = bars.Count;

        var recentBars = bars.TakeLast(period).ToList();
        return (recentBars.Max(b => b.High), recentBars.Min(b => b.Low));
    }

    /// <summary>
    /// Calculates Simple Moving Average.
    /// </summary>
    private static decimal CalculateSMA(List<decimal> values, int period)
    {
        if (values.Count < period) period = values.Count;
        return values.TakeLast(period).Average();
    }

    /// <summary>
    /// Calculates Exponential Moving Average.
    /// </summary>
    private static decimal CalculateEMA(List<decimal> values, int period)
    {
        if (values.Count == 0) return 0m;
        if (values.Count < period) period = values.Count;

        var multiplier = 2m / (period + 1);
        var ema = values.Take(period).Average(); // Start with SMA

        foreach (var value in values.Skip(period))
        {
            ema = (value - ema) * multiplier + ema;
        }

        return ema;
    }

    // ============================================================================
    // BOLLINGER BANDS
    // ============================================================================

    /// <summary>
    /// Calculates Bollinger Bands (upper, middle, lower).
    /// </summary>
    private (decimal upper, decimal middle, decimal lower) CalculateBollingerBands(
        List<Bar> bars, int period, decimal stdDevMultiplier, string source = "close")
    {
        if (bars.Count < period)
        {
            _logger.LogWarning("Insufficient bars for Bollinger Bands. Need {Required}, have {Actual}",
                period, bars.Count);
            var currentPrice = bars.Last().Close;
            return (currentPrice, currentPrice, currentPrice);
        }

        // Get source values
        var values = bars.TakeLast(period).Select(b => source.ToLowerInvariant() switch
        {
            "close" => b.Close,
            "open" => b.Open,
            "high" => b.High,
            "low" => b.Low,
            _ => b.Close
        }).ToList();

        var middle = values.Average(); // SMA
        var stdDev = CalculateStandardDeviation(values);
        var upper = middle + (stdDev * stdDevMultiplier);
        var lower = middle - (stdDev * stdDevMultiplier);

        return (upper, middle, lower);
    }

    // ============================================================================
    // MACD (Moving Average Convergence Divergence)
    // ============================================================================

    /// <summary>
    /// Calculates MACD (line, signal, histogram).
    /// </summary>
    private (decimal macd, decimal signal, decimal histogram) CalculateMACD(
        List<Bar> bars, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        if (bars.Count < slowPeriod)
        {
            _logger.LogWarning("Insufficient bars for MACD. Need {Required}, have {Actual}",
                slowPeriod, bars.Count);
            return (0m, 0m, 0m);
        }

        var closePrices = bars.Select(b => b.Close).ToList();

        var fastEMA = CalculateEMA(closePrices, fastPeriod);
        var slowEMA = CalculateEMA(closePrices, slowPeriod);
        var macdLine = fastEMA - slowEMA;

        // Calculate signal line (EMA of MACD line)
        // We need historical MACD values for this
        var macdValues = new List<decimal>();
        for (int i = slowPeriod; i <= bars.Count; i++)
        {
            var subset = closePrices.Take(i).ToList();
            var f = CalculateEMA(subset, fastPeriod);
            var s = CalculateEMA(subset, slowPeriod);
            macdValues.Add(f - s);
        }

        var signalLine = macdValues.Count >= signalPeriod
            ? CalculateEMA(macdValues, signalPeriod)
            : macdLine;
        var histogram = macdLine - signalLine;

        return (macdLine, signalLine, histogram);
    }

    // ============================================================================
    // STOCHASTIC OSCILLATOR
    // ============================================================================

    /// <summary>
    /// Calculates Stochastic Oscillator (%K, %D).
    /// </summary>
    private (decimal k, decimal d) CalculateStochastic(
        List<Bar> bars, int kPeriod = 14, int dPeriod = 3, int smoothK = 3)
    {
        if (bars.Count < kPeriod)
        {
            _logger.LogWarning("Insufficient bars for Stochastic. Need {Required}, have {Actual}",
                kPeriod, bars.Count);
            return (50m, 50m);
        }

        // Calculate raw %K values
        var kValues = new List<decimal>();
        for (int i = kPeriod - 1; i < bars.Count; i++)
        {
            var periodBars = bars.Skip(i - kPeriod + 1).Take(kPeriod).ToList();
            var highest = periodBars.Max(b => b.High);
            var lowest = periodBars.Min(b => b.Low);
            var currentClose = bars[i].Close;

            var k = (highest == lowest) ? 50m : ((currentClose - lowest) / (highest - lowest)) * 100m;
            kValues.Add(k);
        }

        // Smooth %K
        var smoothedK = kValues.Count >= smoothK
            ? CalculateSMA(kValues, smoothK)
            : kValues.Last();

        // Calculate %D (SMA of smoothed %K)
        var dValue = kValues.Count >= dPeriod
            ? CalculateSMA(kValues.TakeLast(dPeriod).ToList(), dPeriod)
            : smoothedK;

        return (smoothedK, dValue);
    }

    // ============================================================================
    // ADX (Average Directional Index)
    // ============================================================================

    /// <summary>
    /// Calculates ADX (Average Directional Index).
    /// </summary>
    private decimal CalculateADX(List<Bar> bars, int period = 14)
    {
        if (bars.Count < period + 1)
        {
            _logger.LogWarning("Insufficient bars for ADX. Need {Required}, have {Actual}",
                period + 1, bars.Count);
            return 0m;
        }

        var plusDM = new List<decimal>();
        var minusDM = new List<decimal>();
        var trueRanges = new List<decimal>();

        // Calculate +DM, -DM, and TR
        for (int i = 1; i < bars.Count; i++)
        {
            var current = bars[i];
            var previous = bars[i - 1];

            var highDiff = current.High - previous.High;
            var lowDiff = previous.Low - current.Low;

            plusDM.Add(highDiff > lowDiff && highDiff > 0 ? highDiff : 0);
            minusDM.Add(lowDiff > highDiff && lowDiff > 0 ? lowDiff : 0);

            var tr1 = current.High - current.Low;
            var tr2 = Math.Abs(current.High - previous.Close);
            var tr3 = Math.Abs(current.Low - previous.Close);
            trueRanges.Add(Math.Max(tr1, Math.Max(tr2, tr3)));
        }

        // Calculate smoothed +DM, -DM, and ATR
        var smoothedPlusDM = plusDM.TakeLast(period).Sum();
        var smoothedMinusDM = minusDM.TakeLast(period).Sum();
        var atr = trueRanges.TakeLast(period).Average();

        if (atr == 0) return 0m;

        // Calculate +DI and -DI
        var plusDI = (smoothedPlusDM / period / atr) * 100;
        var minusDI = (smoothedMinusDM / period / atr) * 100;

        // Calculate DX
        var diDiff = Math.Abs(plusDI - minusDI);
        var diSum = plusDI + minusDI;
        var dx = diSum == 0 ? 0m : (diDiff / diSum) * 100;

        // ADX is smoothed DX
        return dx; // Simplified - full implementation would smooth DX over period
    }

    // ============================================================================
    // CCI (Commodity Channel Index)
    // ============================================================================

    /// <summary>
    /// Calculates CCI (Commodity Channel Index).
    /// </summary>
    private decimal CalculateCCI(List<Bar> bars, int period = 20)
    {
        if (bars.Count < period)
        {
            _logger.LogWarning("Insufficient bars for CCI. Need {Required}, have {Actual}",
                period, bars.Count);
            return 0m;
        }

        // Calculate typical prices
        var typicalPrices = bars.TakeLast(period)
            .Select(b => (b.High + b.Low + b.Close) / 3)
            .ToList();

        var sma = typicalPrices.Average();
        var meanDeviation = CalculateMeanDeviation(typicalPrices);

        if (meanDeviation == 0) return 0m;

        var currentTypicalPrice = (bars.Last().High + bars.Last().Low + bars.Last().Close) / 3;
        var cci = (currentTypicalPrice - sma) / (0.015m * meanDeviation);

        return cci;
    }

    // ============================================================================
    // WILLIAMS %R
    // ============================================================================

    /// <summary>
    /// Calculates Williams %R.
    /// </summary>
    private decimal CalculateWilliamsR(List<Bar> bars, int period = 14)
    {
        if (bars.Count < period)
        {
            _logger.LogWarning("Insufficient bars for Williams %R. Need {Required}, have {Actual}",
                period, bars.Count);
            return -50m;
        }

        var recentBars = bars.TakeLast(period).ToList();
        var highest = recentBars.Max(b => b.High);
        var lowest = recentBars.Min(b => b.Low);
        var currentClose = bars.Last().Close;

        if (highest == lowest) return -50m;

        var williamsR = ((highest - currentClose) / (highest - lowest)) * -100m;
        return williamsR;
    }

    // ============================================================================
    // OBV (On-Balance Volume)
    // ============================================================================

    /// <summary>
    /// Calculates OBV (On-Balance Volume).
    /// </summary>
    private decimal CalculateOBV(List<Bar> bars)
    {
        if (bars.Count < 2)
        {
            _logger.LogWarning("Insufficient bars for OBV. Need at least 2 bars");
            return bars.Last().Volume;
        }

        decimal obv = 0;
        for (int i = 1; i < bars.Count; i++)
        {
            if (bars[i].Close > bars[i - 1].Close)
            {
                obv += bars[i].Volume;
            }
            else if (bars[i].Close < bars[i - 1].Close)
            {
                obv -= bars[i].Volume;
            }
            // If close unchanged, OBV unchanged
        }

        return obv;
    }

    // ============================================================================
    // ICHIMOKU CLOUD
    // ============================================================================

    /// <summary>
    /// Result structure for Ichimoku Cloud indicator.
    /// </summary>
    private record IchimokuResult(
        decimal Tenkan,      // Conversion Line
        decimal Kijun,       // Base Line
        decimal SenkouA,     // Leading Span A
        decimal SenkouB,     // Leading Span B
        decimal Chikou       // Lagging Span
    );

    /// <summary>
    /// Calculates Ichimoku Cloud.
    /// </summary>
    private IchimokuResult CalculateIchimoku(
        List<Bar> bars,
        int tenkanPeriod = 9,
        int kijunPeriod = 26,
        int senkouBPeriod = 52,
        int displacement = 26)
    {
        if (bars.Count < senkouBPeriod)
        {
            _logger.LogWarning("Insufficient bars for Ichimoku. Need {Required}, have {Actual}",
                senkouBPeriod, bars.Count);
            var currentPrice = bars.Last().Close;
            return new IchimokuResult(currentPrice, currentPrice, currentPrice, currentPrice, currentPrice);
        }

        // Tenkan-sen (Conversion Line): (9-period high + 9-period low) / 2
        var tenkanRange = GetHighLowRange(bars, tenkanPeriod);
        var tenkan = (tenkanRange.highest + tenkanRange.lowest) / 2;

        // Kijun-sen (Base Line): (26-period high + 26-period low) / 2
        var kijunRange = GetHighLowRange(bars, kijunPeriod);
        var kijun = (kijunRange.highest + kijunRange.lowest) / 2;

        // Senkou Span A (Leading Span A): (Tenkan + Kijun) / 2, displaced forward
        var senkouA = (tenkan + kijun) / 2;

        // Senkou Span B (Leading Span B): (52-period high + 52-period low) / 2, displaced forward
        var senkouBRange = GetHighLowRange(bars, senkouBPeriod);
        var senkouB = (senkouBRange.highest + senkouBRange.lowest) / 2;

        // Chikou Span (Lagging Span): Current close, displaced backward
        var chikou = bars.Last().Close;

        return new IchimokuResult(tenkan, kijun, senkouA, senkouB, chikou);
    }

    // ============================================================================
    // PARABOLIC SAR
    // ============================================================================

    /// <summary>
    /// Calculates Parabolic SAR.
    /// </summary>
    private List<decimal> CalculateParabolicSAR(
        List<Bar> bars,
        decimal accelerationStart = 0.02m,
        decimal accelerationMax = 0.2m)
    {
        if (bars.Count < 2)
        {
            _logger.LogWarning("Insufficient bars for Parabolic SAR");
            return new List<decimal> { bars.Last().Close };
        }

        var sarValues = new List<decimal>();
        var isUpTrend = bars[1].Close > bars[0].Close;
        var sar = isUpTrend ? bars[0].Low : bars[0].High;
        var extremePoint = isUpTrend ? bars[1].High : bars[1].Low;
        var acceleration = accelerationStart;

        sarValues.Add(sar);

        for (int i = 1; i < bars.Count; i++)
        {
            var current = bars[i];

            // Update SAR
            sar = sar + acceleration * (extremePoint - sar);

            // Check for trend reversal
            var reversal = false;
            if (isUpTrend)
            {
                if (current.Low < sar)
                {
                    reversal = true;
                    isUpTrend = false;
                    sar = extremePoint;
                    extremePoint = current.Low;
                    acceleration = accelerationStart;
                }
            }
            else
            {
                if (current.High > sar)
                {
                    reversal = true;
                    isUpTrend = true;
                    sar = extremePoint;
                    extremePoint = current.High;
                    acceleration = accelerationStart;
                }
            }

            if (!reversal)
            {
                // Update extreme point and acceleration
                if (isUpTrend)
                {
                    if (current.High > extremePoint)
                    {
                        extremePoint = current.High;
                        acceleration = Math.Min(acceleration + accelerationStart, accelerationMax);
                    }
                }
                else
                {
                    if (current.Low < extremePoint)
                    {
                        extremePoint = current.Low;
                        acceleration = Math.Min(acceleration + accelerationStart, accelerationMax);
                    }
                }
            }

            sarValues.Add(sar);
        }

        return sarValues;
    }

    // ============================================================================
    // VOLUME PROFILE
    // ============================================================================

    /// <summary>
    /// Calculates Volume Profile.
    /// </summary>
    private Dictionary<decimal, long> CalculateVolumeProfile(List<Bar> bars, int bins = 20)
    {
        if (bars.Count == 0)
        {
            _logger.LogWarning("No bars for Volume Profile");
            return new Dictionary<decimal, long>();
        }

        var highest = bars.Max(b => b.High);
        var lowest = bars.Min(b => b.Low);
        var priceRange = highest - lowest;

        if (priceRange == 0)
        {
            return new Dictionary<decimal, long> { [bars.Last().Close] = bars.Sum(b => b.Volume) };
        }

        var binSize = priceRange / bins;
        var volumeProfile = new Dictionary<decimal, long>();

        // Initialize bins
        for (int i = 0; i < bins; i++)
        {
            var priceLevel = lowest + (binSize * i);
            volumeProfile[priceLevel] = 0;
        }

        // Distribute volume across price levels
        foreach (var bar in bars)
        {
            var binIndex = (int)((bar.Close - lowest) / binSize);
            binIndex = Math.Clamp(binIndex, 0, bins - 1);
            var priceLevel = lowest + (binSize * binIndex);

            if (volumeProfile.ContainsKey(priceLevel))
            {
                volumeProfile[priceLevel] += bar.Volume;
            }
        }

        return volumeProfile;
    }
}
