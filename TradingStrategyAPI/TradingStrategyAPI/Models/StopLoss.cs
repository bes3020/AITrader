using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents a stop loss configuration for a trading strategy.
/// Defines how to exit losing trades to limit risk.
/// </summary>
[Table("stop_losses")]
public class StopLoss
{
    /// <summary>
    /// Unique identifier for the stop loss.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Type of stop loss calculation.
    /// Valid values: "points", "percentage", "atr" (Average True Range multiplier)
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("type")]
    [RegularExpression(@"^(points|percentage|atr)$",
        ErrorMessage = "Stop loss type must be 'points', 'percentage', or 'atr'")]
    public required string Type { get; set; }

    /// <summary>
    /// The stop loss value.
    /// - For "points": the number of points/ticks from entry
    /// - For "percentage": the percentage (e.g., 2.0 for 2%)
    /// - For "atr": the ATR multiplier (e.g., 1.5 for 1.5x ATR)
    /// </summary>
    [Required]
    [Range(0.01, 1000.0, ErrorMessage = "Value must be between 0.01 and 1000")]
    [Column("value", TypeName = "decimal(18,4)")]
    public decimal Value { get; set; }

    /// <summary>
    /// Optional description of the stop loss logic.
    /// </summary>
    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Foreign key to the parent strategy.
    /// </summary>
    [Required]
    [Column("strategy_id")]
    public int StrategyId { get; set; }

    /// <summary>
    /// Navigation property to the parent strategy.
    /// </summary>
    [ForeignKey(nameof(StrategyId))]
    [JsonIgnore] // Prevent circular reference in API responses
    public Strategy? Strategy { get; set; }

    /// <summary>
    /// Calculates the actual stop loss price based on entry price and current ATR.
    /// </summary>
    /// <param name="entryPrice">The entry price of the trade</param>
    /// <param name="currentAtr">Current Average True Range value (only needed for ATR type)</param>
    /// <param name="isLong">True if this is a long trade, false for short</param>
    /// <returns>The stop loss price</returns>
    public decimal CalculateStopPrice(decimal entryPrice, decimal currentAtr, bool isLong)
    {
        decimal stopDistance = Type switch
        {
            "points" => Value,
            "percentage" => entryPrice * (Value / 100m),
            "atr" => currentAtr * Value,
            _ => throw new InvalidOperationException($"Unknown stop loss type: {Type}")
        };

        return isLong ? entryPrice - stopDistance : entryPrice + stopDistance;
    }
}
