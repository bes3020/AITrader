using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents a tag for organizing and filtering strategies.
/// NOTE: Uses UserId = 1 (anonymous user) until Phase 1 (Authentication) is completed.
/// </summary>
[Table("strategy_tags")]
public class StrategyTag
{
    /// <summary>
    /// Unique identifier for the tag.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the user who created this tag.
    /// Currently defaults to 1 (anonymous user) until authentication is implemented.
    /// </summary>
    [Required]
    [Column("user_id")]
    public int UserId { get; set; } = 1;

    /// <summary>
    /// Name of the tag (e.g., "Scalping", "Swing Trade", "High Risk").
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Hex color code for the tag (e.g., "#FF5733").
    /// </summary>
    [Required]
    [MaxLength(7)]
    [Column("color")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$",
        ErrorMessage = "Color must be a valid hex code (e.g., #FF5733)")]
    public required string Color { get; set; }

    /// <summary>
    /// Timestamp when the tag was created.
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the user who owns this tag.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
