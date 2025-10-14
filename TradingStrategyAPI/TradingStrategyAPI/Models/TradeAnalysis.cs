using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents detailed analysis of a single trade execution.
/// Contains context, reasoning, and AI-generated insights.
/// </summary>
[Table("trade_analyses")]
public class TradeAnalysis
{
    /// <summary>
    /// Unique identifier for the trade analysis.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the trade result.
    /// </summary>
    [Required]
    [Column("trade_result_id")]
    public int TradeResultId { get; set; }

    /// <summary>
    /// Explanation of why the trade was entered.
    /// Example: "Price crossed above VWAP with volume 2x average"
    /// </summary>
    [Required]
    [Column("entry_reason")]
    public string EntryReason { get; set; } = string.Empty;

    /// <summary>
    /// Explanation of why the trade was exited.
    /// Example: "Stop loss hit", "Take profit reached", "End of session"
    /// </summary>
    [Required]
    [Column("exit_reason")]
    public string ExitReason { get; set; } = string.Empty;

    /// <summary>
    /// Market condition classification at trade time.
    /// Valid values: "trending", "ranging", "volatile", "quiet"
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("market_condition")]
    public string MarketCondition { get; set; } = string.Empty;

    /// <summary>
    /// Time of day classification.
    /// Valid values: "morning" (9:30-12:00), "midday" (12:00-14:00),
    /// "afternoon" (14:00-16:00), "close" (16:00-16:15)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("time_of_day")]
    public string TimeOfDay { get; set; } = string.Empty;

    /// <summary>
    /// Day of week when trade was executed.
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("day_of_week")]
    public string DayOfWeek { get; set; } = string.Empty;

    /// <summary>
    /// VIX (volatility index) level at trade entry time.
    /// Null if VIX data not available.
    /// </summary>
    [Column("vix_level", TypeName = "decimal(10,2)")]
    public decimal? VixLevel { get; set; }

    /// <summary>
    /// Average Directional Index (ADX) at entry.
    /// Measures trend strength (0-100).
    /// </summary>
    [Column("adx_value", TypeName = "decimal(10,2)")]
    public decimal? AdxValue { get; set; }

    /// <summary>
    /// Average True Range (ATR) at entry.
    /// Measures volatility in points.
    /// </summary>
    [Column("atr_value", TypeName = "decimal(10,2)")]
    public decimal? AtrValue { get; set; }

    /// <summary>
    /// AI-generated analysis of what went wrong for losing trades.
    /// Null for winning trades.
    /// </summary>
    [Column("what_went_wrong")]
    public string? WhatWentWrong { get; set; }

    /// <summary>
    /// AI-generated analysis of what went right for winning trades.
    /// Null for losing trades.
    /// </summary>
    [Column("what_went_right")]
    public string? WhatWentRight { get; set; }

    /// <summary>
    /// Full AI-generated narrative of the trade.
    /// Plain English explanation of setup, entry, trade progression, and exit.
    /// </summary>
    [Column("narrative")]
    public string? Narrative { get; set; }

    /// <summary>
    /// Key lessons learned from this trade.
    /// Example: "Avoid entries near significant resistance levels in choppy markets"
    /// </summary>
    [Column("lessons_learned")]
    public string? LessonsLearned { get; set; }

    /// <summary>
    /// When this analysis was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the trade result.
    /// </summary>
    [ForeignKey(nameof(TradeResultId))]
    [JsonIgnore]
    public TradeResult? TradeResult { get; set; }
}
