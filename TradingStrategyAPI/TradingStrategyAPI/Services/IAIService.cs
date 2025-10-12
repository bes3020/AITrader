using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for AI-powered strategy parsing and analysis.
/// Supports multiple AI providers (Claude, Gemini) with identical interface.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Parses a natural language trading strategy description into a structured Strategy object.
    /// Results are cached in Redis to minimize API calls.
    /// </summary>
    /// <param name="description">Natural language description of the trading strategy</param>
    /// <returns>A structured Strategy object with parsed conditions, stop loss, and take profit</returns>
    /// <exception cref="InvalidOperationException">Thrown when AI response cannot be parsed</exception>
    Task<Strategy> ParseStrategyAsync(string description);

    /// <summary>
    /// Generates AI-powered insights and analysis from strategy backtest results.
    /// Identifies key weaknesses and provides actionable recommendations.
    /// </summary>
    /// <param name="result">The strategy result containing performance metrics and trade data</param>
    /// <returns>A concise analysis (2-3 sentences) identifying the primary weakness</returns>
    /// <exception cref="InvalidOperationException">Thrown when AI response cannot be generated</exception>
    Task<string> GenerateInsightsAsync(StrategyResult result);

    /// <summary>
    /// Gets the name of the AI provider being used (e.g., "Claude", "Gemini").
    /// </summary>
    string ProviderName { get; }
}
