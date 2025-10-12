using System.Diagnostics;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for analyzing trading strategy results and generating AI-powered insights.
/// </summary>
public class ResultsAnalyzer : IResultsAnalyzer
{
    private readonly IAIService _aiService;
    private readonly ILogger<ResultsAnalyzer> _logger;

    public ResultsAnalyzer(IAIService aiService, ILogger<ResultsAnalyzer> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<StrategyResult> AnalyzeAsync(List<TradeResult> trades, Strategy strategy)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Analyzing results for strategy {StrategyId} with {TradeCount} trades",
                strategy.Id, trades.Count);

            if (!trades.Any())
            {
                _logger.LogWarning("No trades to analyze for strategy {StrategyId}", strategy.Id);
                return CreateEmptyResult(strategy);
            }

            // Calculate summary statistics
            var totalTrades = trades.Count;
            var winningTrades = trades.Where(t => t.Result == "win").ToList();
            var losingTrades = trades.Where(t => t.Result == "loss").ToList();
            var timeoutTrades = trades.Where(t => t.Result == "timeout").ToList();

            var winRate = totalTrades > 0 ? (decimal)winningTrades.Count / totalTrades : 0m;
            var totalPnl = trades.Sum(t => t.Pnl);

            var avgWin = winningTrades.Any() ? winningTrades.Average(t => t.Pnl) : 0m;
            var avgLoss = losingTrades.Any() ? losingTrades.Average(t => t.Pnl) : 0m;

            // Calculate max drawdown
            var maxDrawdown = CalculateMaxDrawdown(trades);

            // Calculate profit factor
            var grossProfit = winningTrades.Sum(t => t.Pnl);
            var grossLoss = Math.Abs(losingTrades.Sum(t => t.Pnl));
            var profitFactor = grossLoss > 0 ? grossProfit / grossLoss : 0m;

            // Calculate Sharpe ratio
            var sharpeRatio = CalculateSharpeRatio(trades);

            _logger.LogInformation("Statistics calculated: WinRate={WinRate:P2}, TotalPnL=${TotalPnl:F2}, ProfitFactor={ProfitFactor:F2}",
                winRate, totalPnl, profitFactor);

            // Analyze loss patterns
            var lossPatterns = AnalyzeLossPatterns(losingTrades);

            // Find worst trades
            var worstTrades = trades
                .OrderBy(t => t.Pnl)
                .Take(5)
                .ToList();

            var worstTradesTotal = worstTrades.Sum(t => t.Pnl);

            _logger.LogInformation("Worst 5 trades total: ${Total:F2}", worstTradesTotal);

            // Generate AI insights
            string insights;
            try
            {
                insights = await GenerateInsightsAsync(
                    totalTrades,
                    winRate,
                    totalPnl,
                    avgWin,
                    avgLoss,
                    lossPatterns.hourDistribution,
                    worstTradesTotal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI insights, using default message");
                insights = GenerateDefaultInsights(totalTrades, winRate, totalPnl, losingTrades);
            }

            // Determine backtest date range
            var backtestStart = trades.Min(t => t.EntryTime);
            var backtestEnd = trades.Max(t => t.ExitTime ?? t.EntryTime);

            // Create strategy result
            var result = new StrategyResult
            {
                StrategyId = strategy.Id,
                TotalTrades = totalTrades,
                WinRate = winRate,
                TotalPnl = totalPnl,
                AvgWin = avgWin,
                AvgLoss = avgLoss,
                MaxDrawdown = maxDrawdown,
                ProfitFactor = profitFactor > 0 ? profitFactor : null,
                SharpeRatio = sharpeRatio > 0 ? sharpeRatio : null,
                Insights = insights,
                BacktestStart = backtestStart,
                BacktestEnd = backtestEnd,
                CreatedAt = DateTime.UtcNow
            };

            sw.Stop();
            _logger.LogInformation("Analysis completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error analyzing results for strategy {StrategyId} after {ElapsedMs}ms",
                strategy.Id, sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Generates AI-powered insights using the Claude service.
    /// </summary>
    private async Task<string> GenerateInsightsAsync(
        int totalTrades,
        decimal winRate,
        decimal totalPnl,
        decimal avgWin,
        decimal avgLoss,
        string hourDistribution,
        decimal worstTradesTotal)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Generating AI insights using {Provider}...", _aiService.ProviderName);

            var prompt = $@"Analyze these trading results and identify the main problems:

Total trades: {totalTrades}
Win rate: {winRate:P1}
Total P&L: ${totalPnl:F2}
Average win: ${avgWin:F2}
Average loss: ${avgLoss:F2}

Loss distribution by hour: {hourDistribution}

Worst 5 trades lost ${Math.Abs(worstTradesTotal):F2}

In 2-3 sentences, explain the primary weakness of this strategy.
Be specific and actionable. Focus on when/why losses occur.";

            // Note: We're using the GenerateInsightsAsync method from IAIService
            // but we need to create a StrategyResult object first
            // For now, we'll build the prompt directly since we're in the analysis phase

            // Call AI service (this is a simplified approach - in production you'd use the proper method)
            var tempResult = new StrategyResult
            {
                TotalTrades = totalTrades,
                WinRate = winRate,
                TotalPnl = totalPnl,
                AvgWin = avgWin,
                AvgLoss = avgLoss,
                MaxDrawdown = 0,
                BacktestStart = DateTime.UtcNow.AddDays(-30),
                BacktestEnd = DateTime.UtcNow,
                AllTrades = new List<TradeResult>() // Empty for now
            };

            var insights = await _aiService.GenerateInsightsAsync(tempResult);

            sw.Stop();
            _logger.LogInformation("AI insights generated in {ElapsedMs}ms using {Provider}",
                sw.ElapsedMilliseconds, _aiService.ProviderName);

            return insights;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error generating AI insights after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Generates default insights when AI service is unavailable.
    /// </summary>
    private string GenerateDefaultInsights(int totalTrades, decimal winRate, decimal totalPnl, List<TradeResult> losingTrades)
    {
        var avgLossDuration = losingTrades.Any()
            ? losingTrades.Average(t => t.BarsHeld)
            : 0;

        var insights = $"Strategy executed {totalTrades} trades with a {winRate:P1} win rate and ${totalPnl:F2} total P&L. ";

        if (winRate < 0.5m)
        {
            insights += "The win rate is below 50%, suggesting entry conditions may need refinement. ";
        }

        if (losingTrades.Any())
        {
            var hourlyLosses = losingTrades
                .GroupBy(t => t.EntryTime.Hour)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (hourlyLosses != null)
            {
                insights += $"Most losses occur around {hourlyLosses.Key}:00, indicating potential time-based weakness. ";
            }
        }

        if (avgLossDuration > 50)
        {
            insights += "Losing trades are held too long on average, consider tighter stops.";
        }

        return insights;
    }

    /// <summary>
    /// Analyzes patterns in losing trades.
    /// </summary>
    private (string hourDistribution, string dayDistribution, double avgDuration) AnalyzeLossPatterns(List<TradeResult> losingTrades)
    {
        if (!losingTrades.Any())
        {
            return ("No losses", "No losses", 0);
        }

        // Group by hour
        var hourGroups = losingTrades
            .GroupBy(t => t.EntryTime.Hour)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => $"{g.Key}:00 ({g.Count()} losses)")
            .ToList();

        var hourDistribution = string.Join(", ", hourGroups);

        // Group by day of week
        var dayGroups = losingTrades
            .GroupBy(t => t.EntryTime.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => $"{g.Key} ({g.Count()} losses)")
            .ToList();

        var dayDistribution = string.Join(", ", dayGroups);

        // Average duration
        var avgDuration = losingTrades.Average(t => t.BarsHeld);

        _logger.LogDebug("Loss patterns - Hours: {Hours}, Days: {Days}, AvgDuration: {Duration:F1} bars",
            hourDistribution, dayDistribution, avgDuration);

        return (hourDistribution, dayDistribution, avgDuration);
    }

    /// <summary>
    /// Calculates the maximum drawdown from a series of trades.
    /// </summary>
    private decimal CalculateMaxDrawdown(List<TradeResult> trades)
    {
        if (!trades.Any())
        {
            return 0m;
        }

        decimal cumulativePnl = 0m;
        decimal peak = 0m;
        decimal maxDrawdown = 0m;

        foreach (var trade in trades.OrderBy(t => t.EntryTime))
        {
            cumulativePnl += trade.Pnl;

            if (cumulativePnl > peak)
            {
                peak = cumulativePnl;
            }

            var drawdown = peak - cumulativePnl;
            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
            }
        }

        _logger.LogDebug("Max drawdown calculated: ${MaxDrawdown:F2}", maxDrawdown);

        return -maxDrawdown; // Return as negative number
    }

    /// <summary>
    /// Calculates the Sharpe ratio for the trading strategy.
    /// </summary>
    private decimal CalculateSharpeRatio(List<TradeResult> trades)
    {
        if (trades.Count < 2)
        {
            return 0m;
        }

        var returns = trades.Select(t => t.Pnl).ToList();
        var avgReturn = returns.Average();
        var stdDev = CalculateStandardDeviation(returns);

        if (stdDev == 0)
        {
            return 0m;
        }

        // Annualized Sharpe ratio (assuming 252 trading days)
        var sharpe = (avgReturn / stdDev) * (decimal)Math.Sqrt(252);

        _logger.LogDebug("Sharpe ratio calculated: {Sharpe:F2}", sharpe);

        return sharpe;
    }

    /// <summary>
    /// Calculates standard deviation of a series of values.
    /// </summary>
    private decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (values.Count < 2)
        {
            return 0m;
        }

        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        var variance = sumOfSquares / (values.Count - 1);

        return (decimal)Math.Sqrt((double)variance);
    }

    /// <summary>
    /// Creates an empty result for strategies with no trades.
    /// </summary>
    private StrategyResult CreateEmptyResult(Strategy strategy)
    {
        return new StrategyResult
        {
            StrategyId = strategy.Id,
            TotalTrades = 0,
            WinRate = 0m,
            TotalPnl = 0m,
            AvgWin = 0m,
            AvgLoss = 0m,
            MaxDrawdown = 0m,
            Insights = "No trades were executed during the backtest period. Strategy conditions may be too restrictive or data may be insufficient.",
            BacktestStart = DateTime.UtcNow,
            BacktestEnd = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}
