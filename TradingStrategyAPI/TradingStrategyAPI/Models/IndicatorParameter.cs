using System.ComponentModel.DataAnnotations;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents a parameter for a custom indicator.
/// Used to define configurable inputs for indicator calculations.
/// </summary>
public class IndicatorParameter
{
    /// <summary>
    /// Parameter name (e.g., "period", "source", "stdDev").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string Name { get; set; }

    /// <summary>
    /// Data type of the parameter.
    /// Valid values: "int", "decimal", "string", "bool"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public required string Type { get; set; }

    /// <summary>
    /// Default value for the parameter (as string, converted based on Type).
    /// </summary>
    [Required]
    public required string DefaultValue { get; set; }

    /// <summary>
    /// Minimum allowed value (for numeric types).
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// Maximum allowed value (for numeric types).
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Whether this parameter is required.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Human-readable description of what this parameter controls.
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }
}
