using Microsoft.Extensions.Logging;
using TradingStrategyAPI.DataLoader.Models;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.DataLoader.Services;

/// <summary>
/// Validates market data for quality and completeness.
/// </summary>
public class DataValidator
{
    private readonly ILogger<DataValidator> _logger;

    public DataValidator(ILogger<DataValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a list of CSV bars before conversion.
    /// </summary>
    public ValidationResult ValidateCsvBars(List<CsvBar> bars)
    {
        _logger.LogInformation("Validating {Count} CSV bars", bars.Count);

        var result = new ValidationResult();
        var duplicateTimestamps = new HashSet<string>();

        for (int i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];

            // Check OHLC relationships
            if (!bar.IsValid())
            {
                result.Errors.Add($"Row {i + 1}: Invalid OHLC relationship - High={bar.High}, Low={bar.Low}, Open={bar.Open}, Close={bar.Close}");
            }

            // Check for duplicates
            if (duplicateTimestamps.Contains(bar.DateTime))
            {
                result.Warnings.Add($"Row {i + 1}: Duplicate timestamp - {bar.DateTime}");
                result.DuplicateCount++;
            }
            else
            {
                duplicateTimestamps.Add(bar.DateTime);
            }

            // Check for zero/negative values
            if (bar.Open <= 0 || bar.High <= 0 || bar.Low <= 0 || bar.Close <= 0)
            {
                result.Errors.Add($"Row {i + 1}: Zero or negative price detected");
            }

            if (bar.Volume <= 0)
            {
                result.Warnings.Add($"Row {i + 1}: Zero or negative volume - {bar.Volume}");
            }
        }

        result.IsValid = result.Errors.Count == 0;
        _logger.LogInformation("Validation complete: {ErrorCount} errors, {WarningCount} warnings, {Duplicates} duplicates",
            result.Errors.Count, result.Warnings.Count, result.DuplicateCount);

        return result;
    }

    /// <summary>
    /// Finds time gaps in the data (missing bars).
    /// </summary>
    public List<TimeGap> FindTimeGaps(List<Bar> bars, TimeSpan expectedInterval)
    {
        var gaps = new List<TimeGap>();

        if (bars.Count < 2)
            return gaps;

        for (int i = 1; i < bars.Count; i++)
        {
            var timeDiff = bars[i].Timestamp - bars[i - 1].Timestamp;

            // If gap is more than 2x expected interval (allowing for some flexibility)
            if (timeDiff > expectedInterval * 2)
            {
                gaps.Add(new TimeGap
                {
                    StartTime = bars[i - 1].Timestamp,
                    EndTime = bars[i].Timestamp,
                    Duration = timeDiff,
                    MissingBars = (int)(timeDiff.TotalMinutes / expectedInterval.TotalMinutes) - 1
                });
            }
        }

        _logger.LogInformation("Found {Count} time gaps in data", gaps.Count);
        return gaps;
    }

    /// <summary>
    /// Finds potential anomalies in the data (unusual price movements or volume).
    /// </summary>
    public List<string> FindAnomalies(List<Bar> bars)
    {
        var anomalies = new List<string>();

        if (bars.Count < 20)
            return anomalies;

        for (int i = 20; i < bars.Count; i++)
        {
            var bar = bars[i];
            var prevBar = bars[i - 1];

            // Check for unusual price jumps (>5% in one bar)
            var priceChange = Math.Abs((bar.Close - prevBar.Close) / prevBar.Close);
            if (priceChange > 0.05m)
            {
                anomalies.Add($"{bar.Timestamp:yyyy-MM-dd HH:mm}: Large price move {priceChange:P2} (${prevBar.Close} -> ${bar.Close})");
            }

            // Check for unusual volume spikes (>10x average)
            if (bar.AvgVolume20 > 0 && bar.Volume > bar.AvgVolume20 * 10)
            {
                anomalies.Add($"{bar.Timestamp:yyyy-MM-dd HH:mm}: Volume spike {bar.Volume} (avg: {bar.AvgVolume20})");
            }
        }

        _logger.LogInformation("Found {Count} potential anomalies", anomalies.Count);
        return anomalies;
    }
}

/// <summary>
/// Result of data validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int DuplicateCount { get; set; }
}

/// <summary>
/// Represents a gap in time series data.
/// </summary>
public class TimeGap
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int MissingBars { get; set; }

    public override string ToString()
    {
        return $"{StartTime:yyyy-MM-dd HH:mm} to {EndTime:yyyy-MM-dd HH:mm} ({MissingBars} missing bars)";
    }
}
