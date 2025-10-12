using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.DTOs;

/// <summary>
/// Response containing strategy analysis results.
/// </summary>
public class AnalyzeStrategyResponse
{
    /// <summary>
    /// The parsed strategy with entry conditions, stop loss, and take profit.
    /// </summary>
    public required Strategy Strategy { get; set; }

    /// <summary>
    /// Comprehensive analysis results including performance metrics and AI insights.
    /// </summary>
    public required StrategyResult Result { get; set; }

    /// <summary>
    /// Total time taken to complete the analysis in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// AI provider used for strategy parsing (Claude or Gemini).
    /// </summary>
    public required string AiProvider { get; set; }
}
