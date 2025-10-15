using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents a saved comparison of multiple strategies for side-by-side analysis.
/// NOTE: Uses UserId = 1 (anonymous user) until Phase 1 (Authentication) is completed.
/// </summary>
[Table("strategy_comparisons")]
public class StrategyComparison
{
    /// <summary>
    /// Unique identifier for the comparison.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the user who created this comparison.
    /// Currently defaults to 1 (anonymous user) until authentication is implemented.
    /// </summary>
    [Required]
    [Column("user_id")]
    public int UserId { get; set; } = 1;

    /// <summary>
    /// Name of the comparison (e.g., "Scalping Strategies", "Q4 2024 Winners").
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Array of strategy IDs being compared (stored as JSONB).
    /// </summary>
    [Required]
    [Column("strategy_ids", TypeName = "jsonb")]
    public required int[] StrategyIds { get; set; }

    /// <summary>
    /// Timestamp when the comparison was created.
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the user who owns this comparison.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
