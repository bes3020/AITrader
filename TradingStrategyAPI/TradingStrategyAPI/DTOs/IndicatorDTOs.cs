using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.DTOs;

/// <summary>
/// Request to create a custom indicator.
/// </summary>
public class CreateIndicatorRequest
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Type { get; set; }
    public required string Parameters { get; set; } // JSON string
    public string? Formula { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
}

/// <summary>
/// Request to update a custom indicator.
/// </summary>
public class UpdateIndicatorRequest
{
    public string? DisplayName { get; set; }
    public string? Parameters { get; set; }
    public string? Formula { get; set; }
    public string? Description { get; set; }
    public bool? IsPublic { get; set; }
}

/// <summary>
/// Response for a custom indicator.
/// </summary>
public class IndicatorResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Type { get; set; }
    public required string Parameters { get; set; }
    public string? Formula { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Response for a built-in indicator definition.
/// </summary>
public class BuiltInIndicatorResponse
{
    public required string Type { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }
    public required List<IndicatorParameter> Parameters { get; set; }
    public List<IndicatorPreset>? CommonPresets { get; set; }
}

/// <summary>
/// Request to calculate an indicator on historical data.
/// </summary>
public class CalculateIndicatorRequest
{
    public required string Symbol { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
}

/// <summary>
/// Response containing calculated indicator values.
/// </summary>
public class CalculateIndicatorResponse
{
    public int IndicatorId { get; set; }
    public required string IndicatorName { get; set; }
    public required string Type { get; set; }
    public required decimal[] Values { get; set; }
    public DateTime[] Timestamps { get; set; } = Array.Empty<DateTime>();
    public Dictionary<string, decimal[]>? AdditionalSeries { get; set; } // For multi-line indicators like Bollinger Bands
    public required string Parameters { get; set; }
}
