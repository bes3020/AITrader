using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for scanning historical data and simulating strategy trades.
/// Supports multiple futures symbols with appropriate contract specifications.
/// </summary>
public interface IStrategyScanner
{
    /// <summary>
    /// Scans historical data for the specified date range and simulates trades based on strategy conditions.
    /// </summary>
    /// <param name="strategy">The strategy to test</param>
    /// <param name="symbol">The futures symbol to scan (ES, NQ, YM, BTC, CL)</param>
    /// <param name="startDate">Start date for the scan (inclusive)</param>
    /// <param name="endDate">End date for the scan (inclusive)</param>
    /// <returns>List of all trades executed during the scan period</returns>
    Task<List<TradeResult>> ScanAsync(Strategy strategy, string symbol, DateTime startDate, DateTime endDate);
}
