using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for evaluating trading strategy entry conditions and calculating technical indicators.
/// </summary>
public interface IStrategyEvaluator
{
    /// <summary>
    /// Evaluates whether all entry conditions for a strategy are met.
    /// </summary>
    /// <param name="strategy">The strategy containing entry conditions to evaluate</param>
    /// <param name="currentBar">The current bar being evaluated</param>
    /// <param name="historicalBars">Historical bars for indicator calculations (must include currentBar)</param>
    /// <returns>True if all entry conditions pass, false otherwise</returns>
    bool EvaluateEntry(Strategy strategy, Bar currentBar, List<Bar> historicalBars);

    /// <summary>
    /// Calculates a technical indicator value for the given bars.
    /// </summary>
    /// <param name="indicator">The indicator name (e.g., "rsi", "atr", "ema_20")</param>
    /// <param name="bars">Historical bars for calculation</param>
    /// <param name="period">The period to use for calculation</param>
    /// <returns>The calculated indicator value</returns>
    decimal CalculateIndicator(string indicator, List<Bar> bars, int period);
}
