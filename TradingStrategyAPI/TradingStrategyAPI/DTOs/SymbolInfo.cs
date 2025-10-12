namespace TradingStrategyAPI.DTOs;

/// <summary>
/// Information about a supported futures symbol.
/// </summary>
public class SymbolInfo
{
    /// <summary>
    /// Symbol code (ES, NQ, YM, BTC, CL).
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Full display name of the symbol.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Dollar value per point move.
    /// </summary>
    public decimal PointValue { get; set; }

    /// <summary>
    /// Minimum price increment (tick size).
    /// </summary>
    public decimal TickSize { get; set; }

    /// <summary>
    /// Dollar value of a single tick.
    /// </summary>
    public decimal TickValue { get; set; }

    /// <summary>
    /// Earliest available data date for this symbol (null if no data).
    /// </summary>
    public DateTime? MinDate { get; set; }

    /// <summary>
    /// Latest available data date for this symbol (null if no data).
    /// </summary>
    public DateTime? MaxDate { get; set; }

    /// <summary>
    /// Total number of bars available for this symbol.
    /// </summary>
    public long BarCount { get; set; }
}
