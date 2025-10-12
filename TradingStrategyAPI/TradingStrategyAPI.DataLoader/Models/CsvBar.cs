using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.DataLoader.Models;

/// <summary>
/// Represents a single bar from CSV file before conversion to Bar entity.
/// </summary>
public class CsvBar
{
    /// <summary>
    /// DateTime in format: yyyyMMdd HHmmss
    /// Example: "20250616 094000"
    /// </summary>
    public string DateTime { get; set; } = string.Empty;

    /// <summary>
    /// Opening price of the bar.
    /// </summary>
    public decimal Open { get; set; }

    /// <summary>
    /// Highest price during the bar period.
    /// </summary>
    public decimal High { get; set; }

    /// <summary>
    /// Lowest price during the bar period.
    /// </summary>
    public decimal Low { get; set; }

    /// <summary>
    /// Closing price of the bar.
    /// </summary>
    public decimal Close { get; set; }

    /// <summary>
    /// Total volume traded during the bar period.
    /// </summary>
    public long Volume { get; set; }

    /// <summary>
    /// Converts this CsvBar to a Bar entity with the specified symbol.
    /// </summary>
    /// <param name="symbol">The futures symbol (ES, NQ, YM, BTC, CL)</param>
    /// <returns>Bar entity ready for database insertion</returns>
    public Bar ToBar(string symbol)
    {
        // Parse "yyyyMMdd HHmmss" format
        var dt = System.DateTime.ParseExact(DateTime.Trim(), "yyyyMMdd HHmmss", null);

        // Ensure UTC DateTimeKind for PostgreSQL compatibility
        var utcDt = System.DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        return new Bar
        {
            Symbol = symbol.ToUpperInvariant(),
            Timestamp = utcDt,
            Open = Open,
            High = High,
            Low = Low,
            Close = Close,
            Volume = Volume,
            Vwap = 0, // Will be calculated by IndicatorCalculator
            Ema9 = 0,
            Ema20 = 0,
            Ema50 = 0,
            AvgVolume20 = 0
        };
    }

    /// <summary>
    /// Validates that the bar has valid OHLC relationships.
    /// </summary>
    public bool IsValid()
    {
        return High >= Open && High >= Close && High >= Low &&
               Low <= Open && Low <= Close && Low <= High &&
               Open > 0 && High > 0 && Low > 0 && Close > 0 &&
               Volume > 0;
    }
}
