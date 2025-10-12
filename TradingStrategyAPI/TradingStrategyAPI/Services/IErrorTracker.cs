using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for tracking and analyzing strategy evaluation errors
/// </summary>
public interface IErrorTracker
{
    /// <summary>
    /// Logs an error with context and suggested fixes
    /// </summary>
    Task<StrategyError> LogErrorAsync(
        string errorType,
        string message,
        Exception? exception = null,
        int? strategyId = null,
        string? failedExpression = null,
        Dictionary<string, object>? context = null);

    /// <summary>
    /// Gets errors for a specific strategy
    /// </summary>
    Task<List<StrategyError>> GetStrategyErrorsAsync(int strategyId);

    /// <summary>
    /// Gets recent errors across all strategies
    /// </summary>
    Task<List<StrategyError>> GetRecentErrorsAsync(int count = 50);

    /// <summary>
    /// Gets error statistics
    /// </summary>
    Task<ErrorStatistics> GetErrorStatisticsAsync();

    /// <summary>
    /// Analyzes error patterns and suggests fixes
    /// </summary>
    Task<List<ErrorPattern>> AnalyzeErrorPatternsAsync();
}

/// <summary>
/// Error statistics
/// </summary>
public class ErrorStatistics
{
    public int TotalErrors { get; set; }
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
    public Dictionary<string, int> ErrorsBySeverity { get; set; } = new();
    public Dictionary<string, int> TopFailedExpressions { get; set; } = new();
    public int UnresolvedErrors { get; set; }
}

/// <summary>
/// Pattern detected in errors
/// </summary>
public class ErrorPattern
{
    public string Pattern { get; set; } = string.Empty;
    public int Occurrences { get; set; }
    public string SuggestedFix { get; set; } = string.Empty;
    public List<string> Examples { get; set; } = new();
}
