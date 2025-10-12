namespace TradingStrategyAPI.DataLoader.Models;

/// <summary>
/// Result of a CSV import operation.
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Whether the import was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The symbol that was imported.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Number of bars successfully imported.
    /// </summary>
    public int BarsImported { get; set; }

    /// <summary>
    /// Number of bars skipped (duplicates).
    /// </summary>
    public int BarsSkipped { get; set; }

    /// <summary>
    /// Number of bars that failed validation.
    /// </summary>
    public int BarsInvalid { get; set; }

    /// <summary>
    /// Start date/time of the imported data.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date/time of the imported data.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Time taken for the import operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Error message if the import failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// List of warnings encountered during import.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// List of validation errors (row numbers and descriptions).
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();
}
