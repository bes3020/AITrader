using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents a 1-minute market data bar for futures trading.
/// Contains OHLCV data and pre-calculated technical indicators.
/// Supports multiple symbols: ES, NQ, YM, BTC, CL.
/// Uses composite primary key: (Symbol, Timestamp).
/// </summary>
[Table("futures_bars")]
public class Bar
{
    /// <summary>
    /// The futures symbol (ES, NQ, YM, BTC, CL).
    /// Part of composite primary key with Timestamp.
    /// </summary>
    [Required(ErrorMessage = "Symbol is required")]
    [RegularExpression("^(ES|NQ|YM|BTC|CL)$", ErrorMessage = "Symbol must be one of: ES, NQ, YM, BTC, CL")]
    [MaxLength(10)]
    [Column("symbol")]
    public required string Symbol { get; set; }

    /// <summary>
    /// The timestamp of the bar (UTC).
    /// Part of composite primary key with Symbol.
    /// </summary>
    [Required]
    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Opening price of the bar.
    /// </summary>
    [Required]
    [Column("open", TypeName = "decimal(18,2)")]
    public decimal Open { get; set; }

    /// <summary>
    /// Highest price during the bar period.
    /// </summary>
    [Required]
    [Column("high", TypeName = "decimal(18,2)")]
    public decimal High { get; set; }

    /// <summary>
    /// Lowest price during the bar period.
    /// </summary>
    [Required]
    [Column("low", TypeName = "decimal(18,2)")]
    public decimal Low { get; set; }

    /// <summary>
    /// Closing price of the bar.
    /// </summary>
    [Required]
    [Column("close", TypeName = "decimal(18,2)")]
    public decimal Close { get; set; }

    /// <summary>
    /// Total volume traded during the bar period.
    /// </summary>
    [Required]
    [Column("volume")]
    public long Volume { get; set; }

    /// <summary>
    /// Volume-Weighted Average Price for the bar.
    /// </summary>
    [Column("vwap", TypeName = "decimal(18,2)")]
    public decimal Vwap { get; set; }

    /// <summary>
    /// 9-period Exponential Moving Average.
    /// </summary>
    [Column("ema9", TypeName = "decimal(18,2)")]
    public decimal Ema9 { get; set; }

    /// <summary>
    /// 20-period Exponential Moving Average.
    /// </summary>
    [Column("ema20", TypeName = "decimal(18,2)")]
    public decimal Ema20 { get; set; }

    /// <summary>
    /// 50-period Exponential Moving Average.
    /// </summary>
    [Column("ema50", TypeName = "decimal(18,2)")]
    public decimal Ema50 { get; set; }

    /// <summary>
    /// 20-period average volume.
    /// </summary>
    [Column("avg_volume_20")]
    public long AvgVolume20 { get; set; }

    /// <summary>
    /// Validates that High is the highest price in the bar.
    /// </summary>
    public bool IsValid()
    {
        return High >= Open && High >= Close && High >= Low &&
               Low <= Open && Low <= Close && Low <= High;
    }
}
