using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents a custom indicator created by a user.
/// Supports both formula-based and built-in indicator types.
/// NOTE: Uses userId = 1 (anonymous user) until Phase 1 (Authentication) is completed.
/// </summary>
[Table("custom_indicators")]
public class CustomIndicator
{
    /// <summary>
    /// Unique identifier for the custom indicator.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the user who created this indicator.
    /// Default: 1 (anonymous user) until Phase 1.
    /// </summary>
    [Required]
    [Column("user_id")]
    public int UserId { get; set; } = 1;

    /// <summary>
    /// Internal name for the indicator (used in formulas and references).
    /// Must be unique per user. No spaces, alphanumeric and underscore only.
    /// Example: "my_ema_21", "distance_from_vwap"
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Display name shown in UI.
    /// Example: "My EMA 21", "Distance from VWAP (%)"
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("display_name")]
    public required string DisplayName { get; set; }

    /// <summary>
    /// Type of indicator.
    /// Built-in types: "EMA", "SMA", "RSI", "BollingerBands", "MACD", "ATR", "ADX", "Stochastic"
    /// Custom type: "Custom"
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Parameters for the indicator as JSONB.
    /// For built-in: { "period": 21, "source": "close" }
    /// For custom: can include any custom parameters
    /// </summary>
    [Required]
    [Column("parameters", TypeName = "jsonb")]
    public required string Parameters { get; set; }

    /// <summary>
    /// Formula for custom indicators.
    /// Can reference: close, open, high, low, volume, vwap, ema9, ema20, ema50, etc.
    /// Example: "(close - vwap) / vwap * 100"
    /// Null for built-in indicators.
    /// </summary>
    [Column("formula")]
    public string? Formula { get; set; }

    /// <summary>
    /// Description of what this indicator calculates and how to use it.
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this indicator is shared publicly with other users.
    /// Default: false (private to user).
    /// </summary>
    [Column("is_public")]
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Timestamp when the indicator was created (UTC).
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the indicator was last updated (UTC).
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the user who created this indicator.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
