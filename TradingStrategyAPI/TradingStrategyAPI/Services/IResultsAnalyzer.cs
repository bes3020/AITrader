using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for analyzing trading strategy results and generating insights.
/// </summary>
public interface IResultsAnalyzer
{
    /// <summary>
    /// Analyzes a list of trade results and generates comprehensive statistics and AI-powered insights.
    /// </summary>
    /// <param name="trades">List of trade results to analyze</param>
    /// <param name="strategy">The strategy that generated these trades</param>
    /// <returns>A StrategyResult containing all performance metrics and insights</returns>
    Task<StrategyResult> AnalyzeAsync(List<TradeResult> trades, Strategy strategy);
}
