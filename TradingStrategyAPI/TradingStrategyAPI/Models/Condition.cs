using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents a trading condition (entry or exit) for a strategy.
/// Conditions use indicators, operators, and values to define trading logic.
/// </summary>
[Table("conditions")]
public class Condition
{
    /// <summary>
    /// Unique identifier for the condition.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// The indicator to evaluate.
    /// Valid values: "price", "volume", "vwap", "rsi", "atr", "ema9", "ema20", "ema50", "macd", "bb_upper", "bb_lower"
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("indicator")]
    [RegularExpression(@"^(price|volume|vwap|rsi|atr|ema9|ema20|ema50|macd|bb_upper|bb_lower|adx|stoch)$",
        ErrorMessage = "Invalid indicator type")]
    public required string Indicator { get; set; }

    /// <summary>
    /// Comparison operator.
    /// Valid values: ">", "<", ">=", "<=", "=", "crosses_above", "crosses_below"
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("operator")]
    [RegularExpression(@"^(>|<|>=|<=|=|crosses_above|crosses_below)$",
        ErrorMessage = "Invalid operator")]
    public required string Operator { get; set; }

    /// <summary>
    /// The value to compare against.
    /// Can be a number (e.g., "100"), an indicator name (e.g., "ema20"),
    /// or an expression (e.g., "1.5x_average", "0.8x_vwap").
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("value")]
    public required string Value { get; set; }

    /// <summary>
    /// Optional description of the condition logic.
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
    /// Validates that the value format is correct based on its type.
    /// </summary>
    public bool IsValidValue()
    {
        // Check if it's a numeric value
        if (decimal.TryParse(Value, out _))
            return true;

        // Check if it's a valid indicator reference
        var validIndicators = new[] { "price", "volume", "vwap", "rsi", "atr", "ema9", "ema20", "ema50", "macd", "bb_upper", "bb_lower", "adx", "stoch" };
        if (validIndicators.Contains(Value))
            return true;

        // Check if it's a valid expression (e.g., "1.5x_average")
        if (Value.Contains("x_") && Value.Split('x')[0] is string multiplier && decimal.TryParse(multiplier, out _))
            return true;

        return false;
    }
}
