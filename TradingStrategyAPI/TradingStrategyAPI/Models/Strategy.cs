using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents a complete trading strategy with entry conditions, exit rules, and metadata.
/// </summary>
[Table("strategies")]
public class Strategy
{
    /// <summary>
    /// Unique identifier for the strategy.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the user who created this strategy.
    /// </summary>
    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// Name of the strategy.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Detailed description of the strategy logic and goals.
    /// </summary>
    [MaxLength(2000)]
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Trading direction for this strategy.
    /// Valid values: "long" (buy), "short" (sell), "both"
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("direction")]
    [RegularExpression(@"^(long|short|both)$",
        ErrorMessage = "Direction must be 'long', 'short', or 'both'")]
    public required string Direction { get; set; }

    /// <summary>
    /// Symbol or ticker to trade (e.g., "ES", "NQ", "AAPL").
    /// </summary>
    [MaxLength(20)]
    [Column("symbol")]
    public string? Symbol { get; set; }

    /// <summary>
    /// Timeframe for the strategy (e.g., "1m", "5m", "1h", "1d").
    /// </summary>
    [MaxLength(10)]
    [Column("timeframe")]
    public string? Timeframe { get; set; }

    /// <summary>
    /// Timestamp when the strategy was created.
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the strategy was last updated.
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if the strategy is currently active.
    /// </summary>
    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Version number for tracking strategy iterations.
    /// </summary>
    [Column("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Foreign key to parent strategy for versioning.
    /// Null if this is the original strategy.
    /// </summary>
    [Column("parent_strategy_id")]
    public int? ParentStrategyId { get; set; }

    /// <summary>
    /// Version number within the version chain.
    /// </summary>
    [Column("version_number")]
    public int VersionNumber { get; set; } = 1;

    /// <summary>
    /// Tags for organizing and filtering strategies (stored as JSONB).
    /// </summary>
    [Column("tags", TypeName = "jsonb")]
    public string[]? Tags { get; set; }

    /// <summary>
    /// Additional notes about the strategy.
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Indicates if this strategy is marked as favorite.
    /// </summary>
    [Column("is_favorite")]
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// Timestamp when the strategy was last backtested.
    /// </summary>
    [Column("last_backtested_at")]
    public DateTime? LastBacktestedAt { get; set; }

    /// <summary>
    /// Indicates if this strategy is archived.
    /// </summary>
    [Column("is_archived")]
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Maximum number of concurrent positions allowed.
    /// </summary>
    [Column("max_positions")]
    [Range(1, 100)]
    public int MaxPositions { get; set; } = 1;

    /// <summary>
    /// Position size (number of contracts or shares).
    /// </summary>
    [Column("position_size")]
    [Range(1, 10000)]
    public int PositionSize { get; set; } = 1;

    /// <summary>
    /// Navigation property to the user who owns this strategy.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Navigation property to the parent strategy (for versions).
    /// </summary>
    [ForeignKey(nameof(ParentStrategyId))]
    public Strategy? ParentStrategy { get; set; }

    /// <summary>
    /// Navigation property to child strategy versions.
    /// </summary>
    [InverseProperty(nameof(ParentStrategy))]
    public ICollection<Strategy> Versions { get; set; } = new List<Strategy>();

    /// <summary>
    /// List of entry conditions that must be met to enter a trade.
    /// All conditions are typically combined with AND logic.
    /// </summary>
    public ICollection<Condition> EntryConditions { get; set; } = new List<Condition>();

    /// <summary>
    /// Stop loss configuration for risk management.
    /// </summary>
    public StopLoss? StopLoss { get; set; }

    /// <summary>
    /// Take profit configuration for profit taking.
    /// </summary>
    public TakeProfit? TakeProfit { get; set; }

    /// <summary>
    /// Historical results from backtests of this strategy.
    /// </summary>
    public ICollection<StrategyResult> Results { get; set; } = new List<StrategyResult>();

    /// <summary>
    /// Validates that the strategy has required components.
    /// </summary>
    public bool IsValidStrategy()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(Direction) &&
               EntryConditions.Any() &&
               StopLoss != null &&
               TakeProfit != null;
    }

    /// <summary>
    /// Gets the latest backtest result for this strategy.
    /// </summary>
    [NotMapped]
    public StrategyResult? LatestResult => Results
        .OrderByDescending(r => r.CreatedAt)
        .FirstOrDefault();

    /// <summary>
    /// Calculates the average win rate across all backtests.
    /// </summary>
    public decimal GetAverageWinRate()
    {
        if (!Results.Any())
            return 0;

        return Results.Average(r => r.WinRate);
    }

    /// <summary>
    /// Gets a summary string of the strategy for display purposes.
    /// </summary>
    public string GetSummary()
    {
        var conditionsCount = EntryConditions.Count;
        var latestWinRate = LatestResult?.WinRate ?? 0;

        return $"{Name} - {Direction.ToUpper()} - {conditionsCount} conditions - {latestWinRate:P1} win rate";
    }
}
