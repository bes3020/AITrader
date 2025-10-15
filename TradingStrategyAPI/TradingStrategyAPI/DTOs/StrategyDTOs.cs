using System.ComponentModel.DataAnnotations;

namespace TradingStrategyAPI.DTOs;

/// <summary>
/// Request to create a new strategy.
/// </summary>
public class CreateStrategyRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [RegularExpression(@"^(long|short|both)$")]
    public required string Direction { get; set; }

    [MaxLength(20)]
    public string? Symbol { get; set; }

    [MaxLength(10)]
    public string? Timeframe { get; set; }

    public int MaxPositions { get; set; } = 1;
    public int PositionSize { get; set; } = 1;

    public List<ConditionDto>? EntryConditions { get; set; }
    public StopLossDto? StopLoss { get; set; }
    public TakeProfitDto? TakeProfit { get; set; }

    public string[]? Tags { get; set; }
    public string? Notes { get; set; }
    public bool IsFavorite { get; set; } = false;
}

/// <summary>
/// Request to update an existing strategy.
/// </summary>
public class UpdateStrategyRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [RegularExpression(@"^(long|short|both)$")]
    public string? Direction { get; set; }

    [MaxLength(20)]
    public string? Symbol { get; set; }

    [MaxLength(10)]
    public string? Timeframe { get; set; }

    public int? MaxPositions { get; set; }
    public int? PositionSize { get; set; }
    public bool? IsActive { get; set; }

    public List<ConditionDto>? EntryConditions { get; set; }
    public StopLossDto? StopLoss { get; set; }
    public TakeProfitDto? TakeProfit { get; set; }

    public string[]? Tags { get; set; }
    public string? Notes { get; set; }
    public bool? IsFavorite { get; set; }
    public bool? IsArchived { get; set; }
}

/// <summary>
/// Request to create a new version of a strategy.
/// </summary>
public class CreateVersionRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public string? Notes { get; set; }

    // Optional: override specific parts of the parent strategy
    public List<ConditionDto>? EntryConditions { get; set; }
    public StopLossDto? StopLoss { get; set; }
    public TakeProfitDto? TakeProfit { get; set; }
}

/// <summary>
/// Condition DTO for entry/exit conditions.
/// </summary>
public class ConditionDto
{
    [Required]
    public required string Indicator { get; set; }

    [Required]
    public required string Operator { get; set; }

    [Required]
    public required string Value { get; set; }
}

/// <summary>
/// Stop loss configuration DTO.
/// </summary>
public class StopLossDto
{
    [Required]
    [RegularExpression(@"^(points|percent|atr)$")]
    public required string Type { get; set; }

    [Required]
    public required decimal Value { get; set; }
}

/// <summary>
/// Take profit configuration DTO.
/// </summary>
public class TakeProfitDto
{
    [Required]
    [RegularExpression(@"^(points|percent|atr)$")]
    public required string Type { get; set; }

    [Required]
    public required decimal Value { get; set; }
}

/// <summary>
/// Strategy list item for overview displays.
/// </summary>
public class StrategyListItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Direction { get; set; }
    public string? Symbol { get; set; }
    public string? Timeframe { get; set; }
    public bool IsActive { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public int VersionNumber { get; set; }
    public int? ParentStrategyId { get; set; }
    public string[]? Tags { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastBacktestedAt { get; set; }

    // Summary stats from latest result
    public LatestResultSummary? LatestResult { get; set; }
}

/// <summary>
/// Summary of latest backtest result.
/// </summary>
public class LatestResultSummary
{
    public int ResultId { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal TotalPnl { get; set; }
    public decimal MaxDrawdown { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed strategy response with all data.
/// </summary>
public class StrategyDetailResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Direction { get; set; }
    public string? Symbol { get; set; }
    public string? Timeframe { get; set; }
    public bool IsActive { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public int VersionNumber { get; set; }
    public int? ParentStrategyId { get; set; }
    public int MaxPositions { get; set; }
    public int PositionSize { get; set; }
    public string[]? Tags { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastBacktestedAt { get; set; }

    public List<ConditionDto> EntryConditions { get; set; } = new();
    public StopLossDto? StopLoss { get; set; }
    public TakeProfitDto? TakeProfit { get; set; }

    public List<StrategyVersionSummary> Versions { get; set; } = new();
    public List<ResultSummary> Results { get; set; } = new();
}

/// <summary>
/// Summary of a strategy version.
/// </summary>
public class StrategyVersionSummary
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public LatestResultSummary? LatestResult { get; set; }
}

/// <summary>
/// Summary of a backtest result.
/// </summary>
public class ResultSummary
{
    public int Id { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal TotalPnl { get; set; }
    public decimal AvgWin { get; set; }
    public decimal AvgLoss { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal ProfitFactor { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Export format for strategies (JSON).
/// </summary>
public class StrategyExportFormat
{
    public string Version { get; set; } = "1.0";
    public required StrategyExportData Strategy { get; set; }
}

/// <summary>
/// Strategy data for export.
/// </summary>
public class StrategyExportData
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Direction { get; set; }
    public string? Symbol { get; set; }
    public string? Timeframe { get; set; }
    public int MaxPositions { get; set; }
    public int PositionSize { get; set; }
    public string[]? Tags { get; set; }
    public string? Notes { get; set; }
    public List<ConditionDto> EntryConditions { get; set; } = new();
    public StopLossDto? StopLoss { get; set; }
    public TakeProfitDto? TakeProfit { get; set; }
}

/// <summary>
/// Request to import a strategy from JSON.
/// </summary>
public class ImportStrategyRequest
{
    [Required]
    public required string JsonData { get; set; }

    public bool SetAsFavorite { get; set; } = false;
}

/// <summary>
/// Request to compare multiple strategies.
/// </summary>
public class CompareStrategiesRequest
{
    [Required]
    [MinLength(2)]
    public required int[] StrategyIds { get; set; }

    public string? ComparisonName { get; set; }
    public bool SaveComparison { get; set; } = false;
}

/// <summary>
/// Response for strategy comparison.
/// </summary>
public class StrategyComparisonResponse
{
    public string? ComparisonName { get; set; }
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
    public List<StrategyComparisonItem> Strategies { get; set; } = new();
}

/// <summary>
/// Individual strategy in comparison.
/// </summary>
public class StrategyComparisonItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Direction { get; set; }
    public string? Symbol { get; set; }
    public int EntryConditionsCount { get; set; }
    public LatestResultSummary? LatestResult { get; set; }

    // Comparison metrics
    public decimal? WinRateRank { get; set; }
    public decimal? PnlRank { get; set; }
    public decimal? DrawdownRank { get; set; }
}

/// <summary>
/// Request to search strategies.
/// </summary>
public class SearchStrategiesRequest
{
    public string? Query { get; set; }
    public string[]? Tags { get; set; }
    public string? Symbol { get; set; }
    public string? Direction { get; set; }
    public bool? IsFavorite { get; set; }
    public bool? IsArchived { get; set; }
    public decimal? MinWinRate { get; set; }
    public decimal? MinPnl { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Paginated search results.
/// </summary>
public class SearchStrategiesResponse
{
    public List<StrategyListItem> Strategies { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
