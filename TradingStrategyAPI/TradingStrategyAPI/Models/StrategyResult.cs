using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents the aggregate results of a strategy backtest or live run.
/// Contains performance metrics and AI-generated insights.
/// </summary>
[Table("strategy_results")]
public class StrategyResult
{
    /// <summary>
    /// Unique identifier for the strategy result.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the strategy that was tested.
    /// </summary>
    [Required]
    [Column("strategy_id")]
    public int StrategyId { get; set; }

    /// <summary>
    /// Total number of trades executed.
    /// </summary>
    [Required]
    [Column("total_trades")]
    [Range(0, int.MaxValue)]
    public int TotalTrades { get; set; }

    /// <summary>
    /// Win rate as a decimal (e.g., 0.65 for 65% win rate).
    /// </summary>
    [Required]
    [Column("win_rate", TypeName = "decimal(5,4)")]
    [Range(0, 1)]
    public decimal WinRate { get; set; }

    /// <summary>
    /// Total profit/loss across all trades.
    /// </summary>
    [Required]
    [Column("total_pnl", TypeName = "decimal(18,2)")]
    public decimal TotalPnl { get; set; }

    /// <summary>
    /// Average profit for winning trades.
    /// </summary>
    [Required]
    [Column("avg_win", TypeName = "decimal(18,2)")]
    public decimal AvgWin { get; set; }

    /// <summary>
    /// Average loss for losing trades (always negative or zero).
    /// </summary>
    [Required]
    [Column("avg_loss", TypeName = "decimal(18,2)")]
    public decimal AvgLoss { get; set; }

    /// <summary>
    /// Maximum drawdown experienced during the test period.
    /// Represents the largest peak-to-trough decline.
    /// </summary>
    [Required]
    [Column("max_drawdown", TypeName = "decimal(18,2)")]
    public decimal MaxDrawdown { get; set; }

    /// <summary>
    /// Profit factor (gross profit / gross loss).
    /// Values > 1.0 indicate profitability.
    /// </summary>
    [Column("profit_factor", TypeName = "decimal(10,4)")]
    public decimal? ProfitFactor { get; set; }

    /// <summary>
    /// Sharpe ratio - risk-adjusted return metric.
    /// Higher values indicate better risk-adjusted performance.
    /// </summary>
    [Column("sharpe_ratio", TypeName = "decimal(10,4)")]
    public decimal? SharpeRatio { get; set; }

    /// <summary>
    /// AI-generated insights and analysis of the strategy performance.
    /// Includes observations about patterns, weaknesses, and suggestions.
    /// </summary>
    [Column("insights", TypeName = "text")]
    public string? Insights { get; set; }

    /// <summary>
    /// Timestamp when this result was generated.
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Start date of the backtest period.
    /// </summary>
    [Required]
    [Column("backtest_start")]
    public DateTime BacktestStart { get; set; }

    /// <summary>
    /// End date of the backtest period.
    /// </summary>
    [Required]
    [Column("backtest_end")]
    public DateTime BacktestEnd { get; set; }

    /// <summary>
    /// Navigation property to the parent strategy.
    /// </summary>
    [ForeignKey(nameof(StrategyId))]
    [JsonIgnore] // Prevent circular reference in API responses
    public Strategy? Strategy { get; set; }

    /// <summary>
    /// Navigation property to all individual trade results.
    /// </summary>
    public ICollection<TradeResult> AllTrades { get; set; } = new List<TradeResult>();

    /// <summary>
    /// Gets the worst performing trades (highest losses).
    /// </summary>
    [NotMapped]
    public List<TradeResult> WorstTrades => AllTrades
        .Where(t => t.Result == "loss")
        .OrderBy(t => t.Pnl)
        .Take(10)
        .ToList();

    /// <summary>
    /// Gets the best performing trades (highest profits).
    /// </summary>
    [NotMapped]
    public List<TradeResult> BestTrades => AllTrades
        .Where(t => t.Result == "win")
        .OrderByDescending(t => t.Pnl)
        .Take(10)
        .ToList();

    /// <summary>
    /// Calculates the expectancy per trade (average expected profit per trade).
    /// </summary>
    public decimal GetExpectancy()
    {
        if (TotalTrades == 0)
            return 0;

        return (WinRate * AvgWin) + ((1 - WinRate) * AvgLoss);
    }

    /// <summary>
    /// Calculates the number of winning trades.
    /// </summary>
    public int GetWinningTrades()
    {
        return (int)Math.Round(TotalTrades * WinRate);
    }

    /// <summary>
    /// Calculates the number of losing trades.
    /// </summary>
    public int GetLosingTrades()
    {
        return TotalTrades - GetWinningTrades();
    }

    /// <summary>
    /// Gets the duration of the backtest in days.
    /// </summary>
    public int GetBacktestDurationDays()
    {
        return (int)(BacktestEnd - BacktestStart).TotalDays;
    }

    /// <summary>
    /// Validates the strategy result data consistency.
    /// </summary>
    public bool IsValid()
    {
        return TotalTrades >= 0 &&
               WinRate >= 0 && WinRate <= 1 &&
               BacktestEnd > BacktestStart &&
               MaxDrawdown <= 0;
    }
}
