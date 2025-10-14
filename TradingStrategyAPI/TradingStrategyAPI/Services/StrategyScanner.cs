using System.Diagnostics;
using System.Text.Json;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for scanning historical market data and simulating strategy trades.
/// Supports multiple futures symbols with appropriate contract specifications.
/// </summary>
public class StrategyScanner : IStrategyScanner
{
    private readonly IStrategyEvaluator _evaluator;
    private readonly IDataService _dataService;
    private readonly ILogger<StrategyScanner> _logger;

    // Trading constants (symbol-independent)
    private const decimal SlippageTicks = 2m; // 2 ticks slippage per trade
    private const decimal Commission = 4m; // $4 per round trip
    private const int MaxBarsInTrade = 100; // Maximum bars to hold a trade
    private const int MinHistoricalBars = 50; // Minimum bars needed for indicators

    public StrategyScanner(
        IStrategyEvaluator evaluator,
        IDataService dataService,
        ILogger<StrategyScanner> logger)
    {
        _evaluator = evaluator;
        _dataService = dataService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<TradeResult>> ScanAsync(Strategy strategy, string symbol, DateTime startDate, DateTime endDate)
    {
        var sw = Stopwatch.StartNew();
        var trades = new List<TradeResult>();

        // Validate symbol
        if (!FuturesContractSpecs.IsValidSymbol(symbol))
        {
            throw new ArgumentException($"Invalid symbol: {symbol}. Supported symbols: {string.Join(", ", FuturesContractSpecs.GetSupportedSymbols())}", nameof(symbol));
        }

        var symbolUpper = symbol.ToUpperInvariant();
        var pointMultiplier = FuturesContractSpecs.GetPointValue(symbolUpper);
        var tickValue = FuturesContractSpecs.GetTickValue(symbolUpper);
        var slippageCost = FuturesContractSpecs.GetSlippageCost(symbolUpper);

        try
        {
            _logger.LogInformation("Starting strategy scan for {StrategyName} on {Symbol} from {StartDate} to {EndDate}. " +
                "Point multiplier: ${PointMultiplier}, Slippage: ${SlippageCost}",
                strategy.Name, symbolUpper, startDate, endDate, pointMultiplier, slippageCost);

            // Load all bars for the date range (add extra days at start for historical data)
            var dataStartDate = startDate.AddDays(-10); // Extra days for indicators
            var bars = await _dataService.GetBarsAsync(symbolUpper, dataStartDate, endDate);

            if (!bars.Any())
            {
                _logger.LogWarning("No bars found for date range {Start} to {End}", dataStartDate, endDate);
                return trades;
            }

            _logger.LogInformation("Loaded {Count} bars for analysis", bars.Count);

            // Find the index where actual scanning should start
            var scanStartIndex = bars.FindIndex(b => b.Timestamp >= startDate);
            if (scanStartIndex < 0)
            {
                _logger.LogWarning("Start date {StartDate} not found in bar data", startDate);
                return trades;
            }

            // Ensure we have enough historical data
            var actualStartIndex = Math.Max(scanStartIndex, MinHistoricalBars);

            _logger.LogInformation("Scanning {Count} bars starting from index {Index}",
                bars.Count - actualStartIndex, actualStartIndex);

            // Scan through bars looking for entry signals
            for (int i = actualStartIndex; i < bars.Count; i++)
            {
                var currentBar = bars[i];

                // Skip if not enough future bars for trade simulation
                if (i + MaxBarsInTrade >= bars.Count)
                {
                    _logger.LogDebug("Skipping bar at {Timestamp} - insufficient future bars", currentBar.Timestamp);
                    break;
                }

                // Get historical bars up to and including current bar
                var historicalBars = bars.Take(i + 1).ToList();

                // Check if entry conditions are met
                var entrySignal = _evaluator.EvaluateEntry(strategy, currentBar, historicalBars);

                if (entrySignal)
                {
                    _logger.LogInformation("Entry signal detected at {Timestamp} (Price: {Price})",
                        currentBar.Timestamp, currentBar.Close);

                    // Get setup bars (20 bars before entry for context)
                    var setupStartIndex = Math.Max(0, i - 20);
                    var setupBars = bars.Skip(setupStartIndex).Take(i - setupStartIndex).ToList();

                    // Get future bars for trade simulation
                    var futureBars = bars.Skip(i + 1).Take(MaxBarsInTrade).ToList();

                    // Simulate the trade with symbol-specific multipliers and capture context
                    var trade = SimulateTrade(strategy, currentBar, historicalBars, setupBars, futureBars, pointMultiplier, slippageCost);

                    if (trade != null)
                    {
                        trades.Add(trade);
                        _logger.LogInformation("Trade completed: Entry={EntryPrice}, Exit={ExitPrice}, P&L={Pnl}, Result={Result}",
                            trade.EntryPrice, trade.ExitPrice, trade.Pnl, trade.Result);

                        // Skip ahead to avoid overlapping trades
                        i += trade.BarsHeld;
                    }
                }
            }

            sw.Stop();
            _logger.LogInformation("Scan completed in {ElapsedMs}ms. Total trades: {Count}",
                sw.ElapsedMilliseconds, trades.Count);

            return trades;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error during strategy scan after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Simulates a single trade from entry to exit using symbol-specific contract specifications.
    /// </summary>
    /// <param name="strategy">The trading strategy</param>
    /// <param name="entryBar">The bar at which entry occurs</param>
    /// <param name="historicalBars">All historical bars up to entry (for indicator calculation)</param>
    /// <param name="setupBars">Setup context bars (typically 20 bars before entry)</param>
    /// <param name="futureBars">Future bars for trade simulation</param>
    /// <param name="pointMultiplier">Dollar value per point move for this symbol</param>
    /// <param name="slippageCost">Total slippage cost (2 ticks) for this symbol</param>
    private TradeResult? SimulateTrade(Strategy strategy, Bar entryBar, List<Bar> historicalBars, List<Bar> setupBars, List<Bar> futureBars, decimal pointMultiplier, decimal slippageCost)
    {
        try
        {
            if (!futureBars.Any())
            {
                _logger.LogWarning("No future bars available for trade simulation at {Timestamp}", entryBar.Timestamp);
                return null;
            }

            var isLong = strategy.Direction.ToLowerInvariant() == "long";
            var entryPrice = entryBar.Close;

            // Apply slippage (convert dollar slippage to price points)
            var slippageAmount = slippageCost / pointMultiplier;
            entryPrice += isLong ? slippageAmount : -slippageAmount;

            // Calculate stop loss and take profit prices
            var stopLossDistance = CalculateDistance(strategy.StopLoss!, entryBar);
            var takeProfitDistance = CalculateDistance(strategy.TakeProfit!, entryBar);

            decimal stopPrice, targetPrice;

            if (isLong)
            {
                stopPrice = entryPrice - stopLossDistance;
                targetPrice = entryPrice + takeProfitDistance;
            }
            else
            {
                stopPrice = entryPrice + stopLossDistance;
                targetPrice = entryPrice - takeProfitDistance;
            }

            _logger.LogDebug("Trade setup: Entry={Entry}, Stop={Stop}, Target={Target}, Direction={Direction}",
                entryPrice, stopPrice, targetPrice, strategy.Direction);

            // Track max adverse/favorable excursion
            decimal mae = 0m;
            decimal mfe = 0m;
            decimal exitPrice = 0m;
            string exitReason = "timeout";
            int barsHeld = 0;

            // Simulate trade through future bars
            for (int i = 0; i < futureBars.Count; i++)
            {
                var bar = futureBars[i];
                barsHeld = i + 1;

                if (isLong)
                {
                    // Check for stop loss hit
                    if (bar.Low <= stopPrice)
                    {
                        exitPrice = stopPrice;
                        exitReason = "loss";
                        _logger.LogDebug("Stop loss hit at bar {Index}, Price={Price}", i, stopPrice);
                        break;
                    }

                    // Check for take profit hit
                    if (bar.High >= targetPrice)
                    {
                        exitPrice = targetPrice;
                        exitReason = "win";
                        _logger.LogDebug("Take profit hit at bar {Index}, Price={Price}", i, targetPrice);
                        break;
                    }

                    // Update MAE and MFE
                    var unrealizedPnl = (bar.Close - entryPrice) * pointMultiplier;
                    mae = Math.Min(mae, unrealizedPnl);
                    mfe = Math.Max(mfe, unrealizedPnl);
                }
                else // Short
                {
                    // Check for stop loss hit
                    if (bar.High >= stopPrice)
                    {
                        exitPrice = stopPrice;
                        exitReason = "loss";
                        _logger.LogDebug("Stop loss hit at bar {Index}, Price={Price}", i, stopPrice);
                        break;
                    }

                    // Check for take profit hit
                    if (bar.Low <= targetPrice)
                    {
                        exitPrice = targetPrice;
                        exitReason = "win";
                        _logger.LogDebug("Take profit hit at bar {Index}, Price={Price}", i, targetPrice);
                        break;
                    }

                    // Update MAE and MFE
                    var unrealizedPnl = (entryPrice - bar.Close) * pointMultiplier;
                    mae = Math.Min(mae, unrealizedPnl);
                    mfe = Math.Max(mfe, unrealizedPnl);
                }
            }

            // If no stop or target hit, exit at market close
            if (exitPrice == 0m)
            {
                var lastBar = futureBars.Last();
                exitPrice = lastBar.Close;
                exitReason = "timeout";
                barsHeld = futureBars.Count;

                // Apply exit slippage
                exitPrice += isLong ? -slippageAmount : slippageAmount;

                _logger.LogDebug("Trade timed out after {Bars} bars, Exit={Exit}", barsHeld, exitPrice);
            }

            // Calculate P&L using symbol-specific point multiplier
            var pnl = isLong
                ? (exitPrice - entryPrice) * pointMultiplier
                : (entryPrice - exitPrice) * pointMultiplier;

            // Subtract commission
            pnl -= Commission;

            // Capture trade bars (bars during the trade)
            var tradeBarsData = futureBars.Take(barsHeld).ToList();

            // Calculate risk/reward ratio
            var stopDistance = Math.Abs(entryPrice - stopPrice);
            var riskRewardRatio = stopDistance > 0 ? Math.Abs(pnl) / (stopDistance * pointMultiplier) : 0;

            // Calculate indicator values at entry and exit
            var indicatorValues = CaptureIndicatorValues(historicalBars, entryBar, tradeBarsData.LastOrDefault());

            // Create trade result
            var trade = new TradeResult
            {
                EntryTime = entryBar.Timestamp,
                ExitTime = barsHeld > 0 && barsHeld <= futureBars.Count
                    ? futureBars[barsHeld - 1].Timestamp
                    : futureBars.Last().Timestamp,
                EntryPrice = entryPrice,
                ExitPrice = exitPrice,
                Pnl = pnl,
                Result = exitReason,
                BarsHeld = barsHeld,
                MaxAdverseExcursion = mae,
                MaxFavorableExcursion = mfe,
                ChartDataStart = setupBars.FirstOrDefault()?.Timestamp,
                ChartDataEnd = tradeBarsData.LastOrDefault()?.Timestamp ?? entryBar.Timestamp,
                EntryBarIndex = setupBars.Count, // Entry is right after setup bars
                ExitBarIndex = setupBars.Count + barsHeld,
                SetupBars = SerializeBars(setupBars),
                TradeBars = SerializeBars(tradeBarsData),
                IndicatorValues = indicatorValues,
                RiskRewardRatio = riskRewardRatio
            };

            return trade;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating trade at {Timestamp}", entryBar.Timestamp);
            return null;
        }
    }

    /// <summary>
    /// Calculates the distance (in price points) for stop loss or take profit.
    /// </summary>
    private decimal CalculateDistance(StopLoss stopLoss, Bar entryBar)
    {
        return stopLoss.Type.ToLowerInvariant() switch
        {
            "points" => stopLoss.Value,
            "percentage" => entryBar.Close * (stopLoss.Value / 100m),
            "atr" => entryBar.Close * stopLoss.Value * 0.01m, // Assume ATR is percentage-based
            _ => stopLoss.Value // Default to points
        };
    }

    /// <summary>
    /// Calculates the distance (in price points) for stop loss or take profit.
    /// </summary>
    private decimal CalculateDistance(TakeProfit takeProfit, Bar entryBar)
    {
        return takeProfit.Type.ToLowerInvariant() switch
        {
            "points" => takeProfit.Value,
            "percentage" => entryBar.Close * (takeProfit.Value / 100m),
            "atr" => entryBar.Close * takeProfit.Value * 0.01m, // Assume ATR is percentage-based
            _ => takeProfit.Value // Default to points
        };
    }

    /// <summary>
    /// Serializes bars to compact JSON format for storage.
    /// </summary>
    private string? SerializeBars(List<Bar> bars)
    {
        if (bars == null || bars.Count == 0)
            return null;

        try
        {
            // Create simplified bar objects to reduce storage
            var simplifiedBars = bars.Select(b => new
            {
                t = b.Timestamp,
                o = b.Open,
                h = b.High,
                l = b.Low,
                c = b.Close,
                v = b.Volume
            }).ToList();

            return JsonSerializer.Serialize(simplifiedBars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing bars");
            return null;
        }
    }

    /// <summary>
    /// Captures indicator values at entry and exit for analysis.
    /// </summary>
    private string? CaptureIndicatorValues(List<Bar> historicalBars, Bar entryBar, Bar? exitBar)
    {
        try
        {
            var indicators = new Dictionary<string, Dictionary<string, decimal>>();

            // Calculate indicators at entry
            var entryIndicators = new Dictionary<string, decimal>
            {
                ["price"] = entryBar.Close,
                ["volume"] = entryBar.Volume
            };

            // Calculate common indicators if we have enough historical data
            if (historicalBars.Count >= 50)
            {
                entryIndicators["ema9"] = CalculateEMA(historicalBars, 9);
                entryIndicators["ema20"] = CalculateEMA(historicalBars, 20);
                entryIndicators["ema50"] = CalculateEMA(historicalBars, 50);
                entryIndicators["vwap"] = CalculateVWAP(historicalBars);
                entryIndicators["avgVolume20"] = (decimal)historicalBars.TakeLast(20).Average(b => b.Volume);
            }

            indicators["entry"] = entryIndicators;

            // Calculate indicators at exit if available
            if (exitBar != null)
            {
                var exitIndicators = new Dictionary<string, decimal>
                {
                    ["price"] = exitBar.Close,
                    ["volume"] = exitBar.Volume
                };

                indicators["exit"] = exitIndicators;
            }

            return JsonSerializer.Serialize(indicators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing indicator values");
            return null;
        }
    }

    /// <summary>
    /// Calculates Exponential Moving Average.
    /// </summary>
    private decimal CalculateEMA(List<Bar> bars, int period)
    {
        if (bars.Count < period)
            return bars.Last().Close;

        var multiplier = 2m / (period + 1);
        var recentBars = bars.TakeLast(period * 2).ToList(); // Take extra bars for accuracy

        // Start with SMA
        var ema = recentBars.Take(period).Average(b => b.Close);

        // Calculate EMA
        for (int i = period; i < recentBars.Count; i++)
        {
            ema = (recentBars[i].Close - ema) * multiplier + ema;
        }

        return ema;
    }

    /// <summary>
    /// Calculates Volume Weighted Average Price for the trading day.
    /// </summary>
    private decimal CalculateVWAP(List<Bar> bars)
    {
        // VWAP resets each day, so calculate for current day only
        var currentDay = bars.Last().Timestamp.Date;
        var todayBars = bars.Where(b => b.Timestamp.Date == currentDay).ToList();

        if (todayBars.Count == 0)
            return bars.Last().Close;

        var totalVolume = todayBars.Sum(b => b.Volume);
        if (totalVolume == 0)
            return bars.Last().Close;

        var vwap = todayBars.Sum(b => ((b.High + b.Low + b.Close) / 3m) * b.Volume) / totalVolume;
        return vwap;
    }
}
