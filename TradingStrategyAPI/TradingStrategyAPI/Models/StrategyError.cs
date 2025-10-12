using System.Text.Json.Serialization;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Represents an error that occurred during strategy evaluation
/// </summary>
public class StrategyError
{
    /// <summary>
    /// Unique identifier for the error
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the strategy that caused the error
    /// </summary>
    public int? StrategyId { get; set; }

    /// <summary>
    /// Type of error (Parsing, Evaluation, Execution, etc.)
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Error severity (Info, Warning, Error, Critical)
    /// </summary>
    public string Severity { get; set; } = "Error";

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error information
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Stack trace if available
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// The condition or expression that caused the error
    /// </summary>
    public string? FailedExpression { get; set; }

    /// <summary>
    /// Suggested fix or correction
    /// </summary>
    public string? SuggestedFix { get; set; }

    /// <summary>
    /// When the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional context as JSON
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Whether this error has been resolved
    /// </summary>
    public bool IsResolved { get; set; } = false;

    /// <summary>
    /// Navigation property to the strategy
    /// </summary>
    [JsonIgnore] // Prevent circular reference in API responses
    public Strategy? Strategy { get; set; }
}
