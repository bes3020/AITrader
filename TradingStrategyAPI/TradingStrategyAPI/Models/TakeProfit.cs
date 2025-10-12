using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents a take profit configuration for a trading strategy.
/// Defines how to exit winning trades to lock in profits.
/// </summary>
[Table("take_profits")]
public class TakeProfit
{
    /// <summary>
    /// Unique identifier for the take profit.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Type of take profit calculation.
    /// Valid values: "points", "percentage", "atr" (Average True Range multiplier)
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("type")]
    [RegularExpression(@"^(points|percentage|atr)$",
        ErrorMessage = "Take profit type must be 'points', 'percentage', or 'atr'")]
    public required string Type { get; set; }

    /// <summary>
    /// The take profit value.
    /// - For "points": the number of points/ticks from entry
    /// - For "percentage": the percentage (e.g., 5.0 for 5%)
    /// - For "atr": the ATR multiplier (e.g., 2.0 for 2x ATR)
    /// </summary>
    [Required]
    [Range(0.01, 1000.0, ErrorMessage = "Value must be between 0.01 and 1000")]
    [Column("value", TypeName = "decimal(18,4)")]
    public decimal Value { get; set; }

    /// <summary>
    /// Optional description of the take profit logic.
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
    /// Calculates the actual take profit price based on entry price and current ATR.
    /// </summary>
    /// <param name="entryPrice">The entry price of the trade</param>
    /// <param name="currentAtr">Current Average True Range value (only needed for ATR type)</param>
    /// <param name="isLong">True if this is a long trade, false for short</param>
    /// <returns>The take profit price</returns>
    public decimal CalculateTakeProfitPrice(decimal entryPrice, decimal currentAtr, bool isLong)
    {
        decimal profitDistance = Type switch
        {
            "points" => Value,
            "percentage" => entryPrice * (Value / 100m),
            "atr" => currentAtr * Value,
            _ => throw new InvalidOperationException($"Unknown take profit type: {Type}")
        };

        return isLong ? entryPrice + profitDistance : entryPrice - profitDistance;
    }
}
