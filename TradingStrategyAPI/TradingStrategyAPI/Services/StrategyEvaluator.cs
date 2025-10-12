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
        return indicator.ToLowerInvariant() switch
        {
            "price" or "close" => currentBar.Close,
            "open" => currentBar.Open,
            "high" => currentBar.High,
            "low" => currentBar.Low,
            "volume" => currentBar.Volume,
            "vwap" => currentBar.Vwap,
            "ema_9" or "ema9" => currentBar.Ema9,
            "ema_20" or "ema20" => currentBar.Ema20,
            "ema_50" or "ema50" => currentBar.Ema50,
            "rsi" => CalculateRSI(historicalBars, 14),
            "atr" => CalculateATR(historicalBars, 14),
            "prev_day_high" => GetPreviousDayHigh(currentBar, historicalBars),
            "prev_day_low" => GetPreviousDayLow(currentBar, historicalBars),
            "time" => GetTimeInMinutes(currentBar.Timestamp),
            _ => throw new ArgumentException($"Unsupported indicator: {indicator}")
        };
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
                "average" or "avg_volume" => multiplier * currentBar.AvgVolume20,
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
}
