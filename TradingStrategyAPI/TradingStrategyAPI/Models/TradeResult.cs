using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents the result of a single trade execution.
/// Contains entry/exit details and performance metrics.
/// </summary>
[Table("trade_results")]
public class TradeResult
{
    /// <summary>
    /// Unique identifier for the trade result.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Timestamp when the trade was entered.
    /// </summary>
    [Required]
    [Column("entry_time")]
    public DateTime EntryTime { get; set; }

    /// <summary>
    /// Timestamp when the trade was exited (null if still open).
    /// </summary>
    [Column("exit_time")]
    public DateTime? ExitTime { get; set; }

    /// <summary>
    /// Price at which the trade was entered.
    /// </summary>
    [Required]
    [Column("entry_price", TypeName = "decimal(18,2)")]
    public decimal EntryPrice { get; set; }

    /// <summary>
    /// Price at which the trade was exited (null if still open).
    /// </summary>
    [Column("exit_price", TypeName = "decimal(18,2)")]
    public decimal? ExitPrice { get; set; }

    /// <summary>
    /// Profit or loss for this trade in points/currency.
    /// Negative values indicate a loss.
    /// </summary>
    [Column("pnl", TypeName = "decimal(18,2)")]
    public decimal Pnl { get; set; }

    /// <summary>
    /// Trade outcome classification.
    /// Valid values: "win", "loss", "timeout" (exited due to time/session end)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("result")]
    [RegularExpression(@"^(win|loss|timeout)$",
        ErrorMessage = "Result must be 'win', 'loss', or 'timeout'")]
    public required string Result { get; set; }

    /// <summary>
    /// Number of bars the trade was held.
    /// </summary>
    [Required]
    [Column("bars_held")]
    [Range(0, int.MaxValue)]
    public int BarsHeld { get; set; }

    /// <summary>
    /// Maximum Adverse Excursion - the worst unrealized loss during the trade.
    /// Always a non-positive value representing the peak drawdown.
    /// </summary>
    [Required]
    [Column("max_adverse_excursion", TypeName = "decimal(18,2)")]
    public decimal MaxAdverseExcursion { get; set; }

    /// <summary>
    /// Maximum Favorable Excursion - the best unrealized profit during the trade.
    /// Always a non-negative value representing the peak profit.
    /// </summary>
    [Required]
    [Column("max_favorable_excursion", TypeName = "decimal(18,2)")]
    public decimal MaxFavorableExcursion { get; set; }

    /// <summary>
    /// Foreign key to the parent strategy result.
    /// </summary>
    [Required]
    [Column("strategy_result_id")]
    public int StrategyResultId { get; set; }

    /// <summary>
    /// Navigation property to the parent strategy result.
    /// </summary>
    [ForeignKey(nameof(StrategyResultId))]
    [JsonIgnore] // Prevent circular reference in API responses
    public StrategyResult? StrategyResult { get; set; }

    /// <summary>
    /// Start of chart data window (typically entry time - setup duration).
    /// </summary>
    [Column("chart_data_start")]
    public DateTime? ChartDataStart { get; set; }

    /// <summary>
    /// End of chart data window (exit time or current time if still open).
    /// </summary>
    [Column("chart_data_end")]
    public DateTime? ChartDataEnd { get; set; }

    /// <summary>
    /// Index of the entry bar in the chart data array.
    /// </summary>
    [Column("entry_bar_index")]
    public int? EntryBarIndex { get; set; }

    /// <summary>
    /// Index of the exit bar in the chart data array.
    /// </summary>
    [Column("exit_bar_index")]
    public int? ExitBarIndex { get; set; }

    /// <summary>
    /// JSON array of bars showing setup context (before entry).
    /// Stores compact bar data to reduce storage.
    /// </summary>
    [Column("setup_bars", TypeName = "jsonb")]
    public string? SetupBars { get; set; }

    /// <summary>
    /// JSON array of bars during the trade.
    /// Stores compact bar data to reduce storage.
    /// </summary>
    [Column("trade_bars", TypeName = "jsonb")]
    public string? TradeBars { get; set; }

    /// <summary>
    /// JSON object containing indicator values at entry and exit.
    /// Example: {"entry": {"ema9": 4520.5, "vwap": 4518.2}, "exit": {"ema9": 4525.1, "vwap": 4523.8}}
    /// </summary>
    [Column("indicator_values", TypeName = "jsonb")]
    public string? IndicatorValues { get; set; }

    /// <summary>
    /// AI-generated notes about this specific trade.
    /// </summary>
    [Column("trade_notes")]
    public string? TradeNotes { get; set; }

    /// <summary>
    /// Entry quality score (0-100) based on setup conditions.
    /// </summary>
    [Column("entry_quality_score")]
    [Range(0, 100)]
    public int? EntryQualityScore { get; set; }

    /// <summary>
    /// Exit quality score (0-100) based on exit execution.
    /// </summary>
    [Column("exit_quality_score")]
    [Range(0, 100)]
    public int? ExitQualityScore { get; set; }

    /// <summary>
    /// Risk/reward ratio achieved (actual exit PnL / stop loss risk).
    /// </summary>
    [Column("risk_reward_ratio", TypeName = "decimal(10,2)")]
    public decimal? RiskRewardRatio { get; set; }

    /// <summary>
    /// Navigation property to trade analysis.
    /// </summary>
    [JsonIgnore]
    public TradeAnalysis? Analysis { get; set; }

    /// <summary>
    /// Calculates the trade duration in minutes.
    /// </summary>
    public int? GetDurationMinutes()
    {
        if (!ExitTime.HasValue)
            return null;

        return (int)(ExitTime.Value - EntryTime).TotalMinutes;
    }

    /// <summary>
    /// Calculates the efficiency of the trade (actual PnL vs maximum possible).
    /// Returns a value between 0 and 1, where 1 means the trade exited at the peak.
    /// </summary>
    public decimal? GetTradeEfficiency()
    {
        if (MaxFavorableExcursion <= 0)
            return null;

        return Pnl / MaxFavorableExcursion;
    }

    /// <summary>
    /// Determines if this trade gave back profit (had unrealized profit that was lost).
    /// </summary>
    public bool GaveBackProfit()
    {
        return MaxFavorableExcursion > 0 && Pnl < MaxFavorableExcursion;
    }
}
