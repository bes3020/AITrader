using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TradingStrategyAPI.Database;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Tracks and analyzes strategy evaluation errors
/// </summary>
public class ErrorTracker : IErrorTracker
{
    private readonly TradingDbContext _context;
    private readonly ILogger<ErrorTracker> _logger;

    public ErrorTracker(TradingDbContext context, ILogger<ErrorTracker> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StrategyError> LogErrorAsync(
        string errorType,
        string message,
        Exception? exception = null,
        int? strategyId = null,
        string? failedExpression = null,
        Dictionary<string, object>? context = null)
    {
        var error = new StrategyError
        {
            ErrorType = errorType,
            Message = message,
            Details = exception?.Message,
            StackTrace = exception?.StackTrace,
            StrategyId = strategyId,
            FailedExpression = failedExpression,
            Context = context != null ? JsonSerializer.Serialize(context) : null,
            SuggestedFix = GenerateSuggestedFix(errorType, message, failedExpression),
            Severity = DetermineSeverity(errorType, exception),
            Timestamp = DateTime.UtcNow
        };

        _context.StrategyErrors.Add(error);
        await _context.SaveChangesAsync();

        _logger.LogError(exception,
            "[ErrorTracker] {ErrorType}: {Message} | Expression: {Expression} | Suggested: {Fix}",
            errorType, message, failedExpression ?? "N/A", error.SuggestedFix ?? "N/A");

        return error;
    }

    public async Task<List<StrategyError>> GetStrategyErrorsAsync(int strategyId)
    {
        return await _context.StrategyErrors
            .Where(e => e.StrategyId == strategyId)
            .OrderByDescending(e => e.Timestamp)
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<StrategyError>> GetRecentErrorsAsync(int count = 50)
    {
        return await _context.StrategyErrors
            .Include(e => e.Strategy)
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<ErrorStatistics> GetErrorStatisticsAsync()
    {
        var errors = await _context.StrategyErrors
            .Where(e => e.Timestamp >= DateTime.UtcNow.AddDays(-7))
            .ToListAsync();

        return new ErrorStatistics
        {
            TotalErrors = errors.Count,
            ErrorsByType = errors.GroupBy(e => e.ErrorType)
                .ToDictionary(g => g.Key, g => g.Count()),
            ErrorsBySeverity = errors.GroupBy(e => e.Severity)
                .ToDictionary(g => g.Key, g => g.Count()),
            TopFailedExpressions = errors
                .Where(e => !string.IsNullOrEmpty(e.FailedExpression))
                .GroupBy(e => e.FailedExpression!)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count()),
            UnresolvedErrors = errors.Count(e => !e.IsResolved)
        };
    }

    public async Task<List<ErrorPattern>> AnalyzeErrorPatternsAsync()
    {
        var errors = await _context.StrategyErrors
            .Where(e => e.Timestamp >= DateTime.UtcNow.AddDays(-30))
            .Where(e => !string.IsNullOrEmpty(e.FailedExpression))
            .ToListAsync();

        var patterns = new List<ErrorPattern>();

        // Pattern 1: "X * Y" format instead of "Xx_Y"
        var multiplyPatterns = errors
            .Where(e => e.FailedExpression != null && e.FailedExpression.Contains(" * "))
            .GroupBy(e => e.FailedExpression)
            .Select(g => new ErrorPattern
            {
                Pattern = "Using ' * ' instead of 'x_' format",
                Occurrences = g.Count(),
                SuggestedFix = $"Change '{g.Key}' to '{ConvertMultiplyToXFormat(g.Key!)}'",
                Examples = g.Select(e => e.FailedExpression!).Distinct().Take(3).ToList()
            })
            .ToList();

        patterns.AddRange(multiplyPatterns);

        // Pattern 2: Incorrect indicator names
        var indicatorPatterns = errors
            .Where(e => e.Message.Contains("Cannot resolve value"))
            .GroupBy(e => ExtractIndicatorName(e.FailedExpression))
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g => new ErrorPattern
            {
                Pattern = $"Unknown indicator: {g.Key}",
                Occurrences = g.Count(),
                SuggestedFix = $"Valid indicators: price, volume, vwap, ema9, ema20, ema50, avgVolume20",
                Examples = g.Select(e => e.FailedExpression!).Distinct().Take(3).ToList()
            })
            .ToList();

        patterns.AddRange(indicatorPatterns);

        return patterns.Where(p => p.Occurrences > 0).ToList();
    }

    private string DetermineSeverity(string errorType, Exception? exception)
    {
        if (errorType.Contains("Critical") || exception is NullReferenceException)
            return "Critical";
        if (errorType.Contains("Parsing") || errorType.Contains("Evaluation"))
            return "Warning";
        return "Error";
    }

    private string? GenerateSuggestedFix(string errorType, string message, string? failedExpression)
    {
        if (string.IsNullOrEmpty(failedExpression))
            return null;

        // Pattern: "X * Y" should be "Xx_Y"
        if (failedExpression.Contains(" * "))
        {
            var converted = ConvertMultiplyToXFormat(failedExpression);
            return $"Change '{failedExpression}' to '{converted}'";
        }

        // Pattern: "average_volume" should be "avgVolume20" or use multiplier format
        if (failedExpression.Contains("average_volume"))
        {
            return "Use 'avgVolume20' indicator or format as '1.5x_average'";
        }

        // Pattern: spaces in expressions
        if (failedExpression.Contains(" "))
        {
            return "Remove spaces from expressions. Use underscores instead.";
        }

        // Pattern: incorrect indicator name
        if (message.Contains("Cannot resolve value"))
        {
            return "Valid indicators: price, volume, vwap, ema9, ema20, ema50, avgVolume20. " +
                   "Valid formats: '1.5x_average', '0.8x_vwap'";
        }

        return null;
    }

    private string ConvertMultiplyToXFormat(string expression)
    {
        // Convert "1.5 * average_volume" to "1.5x_average"
        var parts = expression.Split(new[] { " * " }, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            var multiplier = parts[0].Trim();
            var indicator = parts[1].Trim().Replace("average_volume", "average").Replace("_", "");
            return $"{multiplier}x_{indicator}";
        }
        return expression;
    }

    private string? ExtractIndicatorName(string? expression)
    {
        if (string.IsNullOrEmpty(expression))
            return null;

        // Extract the indicator name from various formats
        var words = expression.Split(new[] { ' ', '*', 'x', '_' }, StringSplitOptions.RemoveEmptyEntries);
        return words.LastOrDefault(w => !double.TryParse(w, out _));
    }
}
