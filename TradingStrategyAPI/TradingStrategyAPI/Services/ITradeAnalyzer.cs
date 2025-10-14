using TradingStrategyAPI.DTOs;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for analyzing individual trades and identifying patterns.
/// </summary>
public interface ITradeAnalyzer
{
    /// <summary>
    /// Analyzes a single trade and generates insights.
    /// </summary>
    /// <param name="trade">The trade to analyze</param>
    /// <param name="strategy">The strategy that generated this trade</param>
    /// <returns>Complete trade analysis with context and insights</returns>
    Task<TradeAnalysis> AnalyzeTradeAsync(TradeResult trade, Strategy strategy);

    /// <summary>
    /// Generates a plain-English narrative describing the trade from entry to exit.
    /// </summary>
    /// <param name="trade">The trade to narrate</param>
    /// <param name="strategy">The strategy that generated this trade</param>
    /// <returns>Human-readable trade story</returns>
    Task<string> GenerateTradeNarrativeAsync(TradeResult trade, Strategy strategy);

    /// <summary>
    /// Identifies common patterns across multiple trades.
    /// </summary>
    /// <param name="trades">List of trades to analyze</param>
    /// <returns>List of identified patterns (both positive and negative)</returns>
    Task<List<TradePattern>> FindPatternsAsync(List<TradeResult> trades);

    /// <summary>
    /// Calculates trade statistics grouped by various dimensions.
    /// </summary>
    /// <param name="trades">List of trades to analyze</param>
    /// <returns>Dictionary of dimension stats (by hour, day, condition, etc.)</returns>
    Task<Dictionary<string, DimensionStats>> CalculateTradeStatsByDimensionAsync(List<TradeResult> trades);

    /// <summary>
    /// Generates heatmap data for visualizing performance patterns.
    /// </summary>
    /// <param name="trades">List of trades to analyze</param>
    /// <param name="dimension">Dimension to analyze: "hour", "day", "condition"</param>
    /// <returns>Heatmap data ready for visualization</returns>
    Task<HeatmapData> GenerateHeatmapAsync(List<TradeResult> trades, string dimension);

    /// <summary>
    /// Classifies market condition based on price action and volatility.
    /// </summary>
    /// <param name="bars">Recent price bars</param>
    /// <returns>"trending", "ranging", "volatile", or "quiet"</returns>
    string ClassifyMarketCondition(List<Bar> bars);

    /// <summary>
    /// Classifies time of day for a given timestamp.
    /// </summary>
    /// <param name="timestamp">The time to classify</param>
    /// <returns>"morning", "midday", "afternoon", or "close"</returns>
    string ClassifyTimeOfDay(DateTime timestamp);

    /// <summary>
    /// Calculates entry quality score (0-100) based on setup conditions.
    /// </summary>
    /// <param name="trade">The trade to score</param>
    /// <param name="strategy">The strategy that generated this trade</param>
    /// <returns>Score from 0 (poor) to 100 (excellent)</returns>
    int CalculateEntryQualityScore(TradeResult trade, Strategy strategy);

    /// <summary>
    /// Calculates exit quality score (0-100) based on how well the exit was executed.
    /// </summary>
    /// <param name="trade">The trade to score</param>
    /// <returns>Score from 0 (poor) to 100 (excellent)</returns>
    int CalculateExitQualityScore(TradeResult trade);
}
