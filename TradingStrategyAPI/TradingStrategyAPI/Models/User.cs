using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents a user account in the trading strategy system.
/// </summary>
[Table("users")]
public class User
{
    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// User's email address (used for authentication).
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    [Column("email")]
    public required string Email { get; set; }

    /// <summary>
    /// Hashed password for authentication.
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("password_hash")]
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Timestamp when the user account was created.
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for user's trading strategies.
    /// </summary>
    public ICollection<Strategy> Strategies { get; set; } = new List<Strategy>();
}
