using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for retrieving and caching market data (bars) for multiple futures symbols.
/// Supports: ES, NQ, YM, BTC, CL.
/// </summary>
public interface IDataService
{
    /// <summary>
    /// Retrieves market data bars within the specified date range for a given symbol.
    /// Results are cached in Redis for performance.
    /// </summary>
    /// <param name="symbol">The futures symbol (ES, NQ, YM, BTC, CL)</param>
    /// <param name="start">Start date/time (inclusive)</param>
    /// <param name="end">End date/time (inclusive)</param>
    /// <returns>List of bars ordered by timestamp</returns>
    Task<List<Bar>> GetBarsAsync(string symbol, DateTime start, DateTime end);

    /// <summary>
    /// Retrieves a single bar at the specified timestamp for a given symbol.
    /// Result is cached in Redis for performance.
    /// </summary>
    /// <param name="symbol">The futures symbol (ES, NQ, YM, BTC, CL)</param>
    /// <param name="timestamp">The exact timestamp of the bar</param>
    /// <returns>The bar at the specified timestamp, or null if not found</returns>
    Task<Bar?> GetBarAsync(string symbol, DateTime timestamp);

    /// <summary>
    /// Retrieves the high and low prices from the previous trading day for a given symbol.
    /// Useful for identifying key support/resistance levels.
    /// </summary>
    /// <param name="symbol">The futures symbol (ES, NQ, YM, BTC, CL)</param>
    /// <param name="date">The reference date (will find previous day's data)</param>
    /// <returns>A tuple containing the previous day's high and low prices</returns>
    Task<(decimal high, decimal low)> GetPreviousDayHighLowAsync(string symbol, DateTime date);
}
