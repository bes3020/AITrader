using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using TradingStrategyAPI.DataLoader.Models;

namespace TradingStrategyAPI.DataLoader.Services;

/// <summary>
/// Parses CSV files containing market data bars.
/// </summary>
public class CsvParser
{
    private readonly ILogger<CsvParser> _logger;

    public CsvParser(ILogger<CsvParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses a CSV file and returns a list of CsvBar objects.
    /// </summary>
    public async Task<List<CsvBar>> ParseCsvFileAsync(string filePath)
    {
        _logger.LogInformation("Parsing CSV file: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV file not found: {filePath}");
        }

        var bars = new List<CsvBar>();
        var rowNumber = 0;
        var errorCount = 0;

        try
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false, // No header in our CSV files
                Delimiter = ",",
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null // Handle bad data manually
            });

            await foreach (var record in csv.GetRecordsAsync<CsvBar>())
            {
                rowNumber++;

                // Validate the bar
                if (!record.IsValid())
                {
                    _logger.LogWarning("Invalid bar at row {RowNumber}: OHLC={Open},{High},{Low},{Close} Volume={Volume}",
                        rowNumber, record.Open, record.High, record.Low, record.Close, record.Volume);
                    errorCount++;
                    continue;
                }

                bars.Add(record);

                // Log progress every 10,000 bars
                if (rowNumber % 10000 == 0)
                {
                    _logger.LogInformation("Parsed {Count} bars...", rowNumber);
                }
            }

            _logger.LogInformation("Parsing complete. Total bars: {Total}, Valid: {Valid}, Invalid: {Invalid}",
                rowNumber, bars.Count, errorCount);

            return bars;
        }
        catch (CsvHelper.HeaderValidationException ex)
        {
            _logger.LogError(ex, "CSV header validation failed");
            throw new FormatException($"CSV format error: {ex.Message}");
        }
        catch (CsvHelper.ReaderException ex)
        {
            _logger.LogError(ex, "CSV parsing error at row {RowNumber}", rowNumber);
            throw new FormatException($"CSV parsing error at row {rowNumber}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing CSV file");
            throw;
        }
    }

    /// <summary>
    /// Gets basic file information without full parsing.
    /// </summary>
    public async Task<(int lineCount, long fileSize)> GetFileInfoAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);
        var lineCount = 0;

        using var reader = new StreamReader(filePath);
        while (await reader.ReadLineAsync() != null)
        {
            lineCount++;
        }

        return (lineCount, fileInfo.Length);
    }
}
