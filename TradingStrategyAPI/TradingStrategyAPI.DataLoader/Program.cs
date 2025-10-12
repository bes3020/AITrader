using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingStrategyAPI.Database;
using TradingStrategyAPI.DataLoader.Services;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.DataLoader;

public class Program
{
    private static IServiceProvider _serviceProvider = null!;
    private static ILogger<Program> _logger = null!;
    private static TradingDbContext _dbContext = null!;
    private static ContractMapper _contractMapper = null!;
    private static CsvParser _csvParser = null!;
    private static IndicatorCalculator _indicatorCalculator = null!;
    private static DataValidator _dataValidator = null!;
    private static IConfiguration _configuration = null!;

    public static async Task Main(string[] args)
    {
        // Build the host with dependency injection
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Get configuration
                var configuration = context.Configuration;

                // Add DbContext
                var connectionString = configuration.GetConnectionString("PostgreSQL");
                services.AddDbContext<TradingDbContext>(options =>
                    options.UseNpgsql(connectionString));

                // Add services
                services.AddSingleton<ContractMapper>();
                services.AddSingleton<CsvParser>();
                services.AddSingleton<IndicatorCalculator>();
                services.AddSingleton<DataValidator>();

                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            })
            .Build();

        // Get services
        _serviceProvider = host.Services;
        _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
        _dbContext = _serviceProvider.GetRequiredService<TradingDbContext>();
        _contractMapper = _serviceProvider.GetRequiredService<ContractMapper>();
        _csvParser = _serviceProvider.GetRequiredService<CsvParser>();
        _indicatorCalculator = _serviceProvider.GetRequiredService<IndicatorCalculator>();
        _dataValidator = _serviceProvider.GetRequiredService<DataValidator>();
        _configuration = _serviceProvider.GetRequiredService<IConfiguration>();

        // Verify database connection
        try
        {
            await _dbContext.Database.CanConnectAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Database connection successful");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Database connection failed: {ex.Message}");
            Console.ResetColor();
            return;
        }

        // Main menu loop
        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║          Trading Strategy Data Loader v1.0                 ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            // Show database status
            await ShowDatabaseStatusAsync();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("┌────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Menu Options                                               │");
            Console.WriteLine("└────────────────────────────────────────────────────────────┘");
            Console.ResetColor();
            Console.WriteLine("  1. Import single CSV file");
            Console.WriteLine("  2. Import directory (batch)");
            Console.WriteLine("  3. Validate existing data");
            Console.WriteLine("  4. Delete data for symbol/date range");
            Console.WriteLine("  5. Show detailed statistics");
            Console.WriteLine("  6. Recalculate indicators");
            Console.WriteLine("  0. Exit");
            Console.WriteLine();
            Console.Write("Select option: ");

            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await ImportSingleFileAsync();
                        break;
                    case "2":
                        await ImportDirectoryAsync();
                        break;
                    case "3":
                        await ValidateDataAsync();
                        break;
                    case "4":
                        await DeleteDataAsync();
                        break;
                    case "5":
                        await ShowDetailedStatisticsAsync();
                        break;
                    case "6":
                        await RecalculateIndicatorsAsync();
                        break;
                    case "0":
                        Console.WriteLine("Exiting...");
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid option. Press any key to continue...");
                        Console.ResetColor();
                        Console.ReadKey();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ Error: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "Error processing menu option {Choice}", choice);
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }

    /// <summary>
    /// Shows current database status with bar counts per symbol.
    /// </summary>
    private static async Task ShowDatabaseStatusAsync()
    {
        var stats = await _dbContext.Bars
            .GroupBy(b => b.Symbol)
            .Select(g => new
            {
                Symbol = g.Key,
                Count = g.Count(),
                MinDate = g.Min(b => b.Timestamp),
                MaxDate = g.Max(b => b.Timestamp)
            })
            .OrderBy(s => s.Symbol)
            .ToListAsync();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("┌────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ Database Status                                            │");
        Console.WriteLine("└────────────────────────────────────────────────────────────┘");
        Console.ResetColor();

        if (stats.Any())
        {
            Console.WriteLine($"{"Symbol",-10} {"Bars",12} {"From Date",20} {"To Date",20}");
            Console.WriteLine(new string('-', 62));
            foreach (var stat in stats)
            {
                Console.WriteLine($"{stat.Symbol,-10} {stat.Count,12:N0} {stat.MinDate,20:yyyy-MM-dd HH:mm} {stat.MaxDate,20:yyyy-MM-dd HH:mm}");
            }
            Console.WriteLine(new string('-', 62));
            Console.WriteLine($"{"TOTAL",-10} {stats.Sum(s => s.Count),12:N0}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No data loaded yet.");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Imports a single CSV file.
    /// </summary>
    private static async Task ImportSingleFileAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Import Single CSV File");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        // Get default directory from config
        var defaultDir = _configuration["DataLoader:DefaultImportDirectory"] ?? "C:/TradingData/";
        Console.WriteLine($"Default directory: {defaultDir}");
        Console.WriteLine();
        Console.Write("Enter CSV file path (or press Enter for file browser): ");
        var filePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.Write($"Enter filename in {defaultDir}: ");
            var filename = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(filename))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No file specified.");
                Console.ResetColor();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            filePath = Path.Combine(defaultDir, filename);
        }

        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ File not found: {filePath}");
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Processing file...");
        Console.ResetColor();

        // Parse filename to get contract info
        var contractInfo = _contractMapper.ParseContractFilename(filePath);
        Console.WriteLine($"Contract: {contractInfo.ContractCode} → {contractInfo.BaseSymbol}");
        Console.WriteLine($"Timeframe: {contractInfo.Timeframe}");
        Console.WriteLine($"Session: {contractInfo.Session}");
        Console.WriteLine();

        // Parse CSV file
        var csvBars = await _csvParser.ParseCsvFileAsync(filePath);
        Console.WriteLine($"Parsed {csvBars.Count:N0} bars from CSV");

        if (csvBars.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No valid bars found in file.");
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        // Validate data
        var enableValidation = _configuration.GetValue<bool>("DataLoader:EnableValidation", true);
        if (enableValidation)
        {
            Console.WriteLine("\nValidating data...");
            var validationResult = _dataValidator.ValidateCsvBars(csvBars);

            if (!validationResult.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Validation failed with {validationResult.Errors.Count} errors:");
                foreach (var error in validationResult.Errors.Take(10))
                {
                    Console.WriteLine($"  - {error}");
                }
                if (validationResult.Errors.Count > 10)
                {
                    Console.WriteLine($"  ... and {validationResult.Errors.Count - 10} more errors");
                }
                Console.ResetColor();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return;
            }

            if (validationResult.Warnings.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ {validationResult.Warnings.Count} warnings:");
                foreach (var warning in validationResult.Warnings.Take(5))
                {
                    Console.WriteLine($"  - {warning}");
                }
                if (validationResult.Warnings.Count > 5)
                {
                    Console.WriteLine($"  ... and {validationResult.Warnings.Count - 5} more warnings");
                }
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Validation passed");
            Console.ResetColor();
        }

        // Convert to Bar entities
        Console.WriteLine("\nConverting to Bar entities...");
        var bars = csvBars
            .Select(cb => cb.ToBar(contractInfo.BaseSymbol))
            .OrderBy(b => b.Timestamp)
            .ToList();

        // Calculate indicators
        Console.WriteLine("Calculating indicators...");
        _indicatorCalculator.CalculateAllIndicators(bars);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ Indicators calculated");
        Console.ResetColor();

        // Import to database
        Console.WriteLine("\nImporting to database...");
        var batchSize = _configuration.GetValue<int>("DataLoader:BatchSize", 10000);
        var skipDuplicates = _configuration.GetValue<bool>("DataLoader:SkipDuplicates", true);

        var imported = 0;
        var skipped = 0;

        for (int i = 0; i < bars.Count; i += batchSize)
        {
            var batch = bars.Skip(i).Take(batchSize).ToList();

            if (skipDuplicates)
            {
                // Check for existing bars
                var timestamps = batch.Select(b => b.Timestamp).ToList();
                var existing = await _dbContext.Bars
                    .Where(b => b.Symbol == contractInfo.BaseSymbol && timestamps.Contains(b.Timestamp))
                    .Select(b => b.Timestamp)
                    .ToListAsync();

                batch = batch.Where(b => !existing.Contains(b.Timestamp)).ToList();
                skipped += timestamps.Count - batch.Count;
            }

            if (batch.Any())
            {
                await _dbContext.Bars.AddRangeAsync(batch);
                await _dbContext.SaveChangesAsync();
                imported += batch.Count;
            }

            // Show progress
            var progress = Math.Min(i + batchSize, bars.Count);
            var percent = (progress * 100) / bars.Count;
            Console.Write($"\rProgress: {progress:N0} / {bars.Count:N0} ({percent}%) - Imported: {imported:N0}, Skipped: {skipped:N0}");
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Import Complete");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine($"File: {Path.GetFileName(filePath)}");
        Console.WriteLine($"Symbol: {contractInfo.BaseSymbol}");
        Console.WriteLine($"Bars imported: {imported:N0}");
        Console.WriteLine($"Bars skipped (duplicates): {skipped:N0}");
        Console.WriteLine($"Date range: {bars.Min(b => b.Timestamp):yyyy-MM-dd HH:mm} to {bars.Max(b => b.Timestamp):yyyy-MM-dd HH:mm}");
        Console.WriteLine("═══════════════════════════════════════════════════════════");

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    /// <summary>
    /// Imports all CSV files from a directory.
    /// </summary>
    private static async Task ImportDirectoryAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Import Directory (Batch)");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        var defaultDir = _configuration["DataLoader:DefaultImportDirectory"] ?? "C:/TradingData/";
        Console.Write($"Enter directory path (or press Enter for {defaultDir}): ");
        var dirPath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(dirPath))
        {
            dirPath = defaultDir;
        }

        if (!Directory.Exists(dirPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Directory not found: {dirPath}");
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var csvFiles = Directory.GetFiles(dirPath, "*.csv");
        if (csvFiles.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No CSV files found in directory.");
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"\nFound {csvFiles.Length} CSV files");
        Console.WriteLine("\nFiles to import:");
        foreach (var file in csvFiles)
        {
            Console.WriteLine($"  - {Path.GetFileName(file)}");
        }

        Console.WriteLine();
        Console.Write("Proceed with import? (y/n): ");
        if (Console.ReadLine()?.ToLower() != "y")
        {
            return;
        }

        var totalImported = 0;
        var totalSkipped = 0;
        var successCount = 0;
        var failCount = 0;

        foreach (var file in csvFiles)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Processing: {Path.GetFileName(file)}");
            Console.ResetColor();

            try
            {
                var contractInfo = _contractMapper.ParseContractFilename(file);
                var csvBars = await _csvParser.ParseCsvFileAsync(file);

                if (csvBars.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  ⚠ No valid bars found, skipping");
                    Console.ResetColor();
                    continue;
                }

                var bars = csvBars
                    .Select(cb => cb.ToBar(contractInfo.BaseSymbol))
                    .OrderBy(b => b.Timestamp)
                    .ToList();

                _indicatorCalculator.CalculateAllIndicators(bars);

                var batchSize = _configuration.GetValue<int>("DataLoader:BatchSize", 10000);
                var skipDuplicates = _configuration.GetValue<bool>("DataLoader:SkipDuplicates", true);
                var imported = 0;

                for (int i = 0; i < bars.Count; i += batchSize)
                {
                    var batch = bars.Skip(i).Take(batchSize).ToList();

                    if (skipDuplicates)
                    {
                        var timestamps = batch.Select(b => b.Timestamp).ToList();
                        var existing = await _dbContext.Bars
                            .Where(b => b.Symbol == contractInfo.BaseSymbol && timestamps.Contains(b.Timestamp))
                            .Select(b => b.Timestamp)
                            .ToListAsync();

                        batch = batch.Where(b => !existing.Contains(b.Timestamp)).ToList();
                        totalSkipped += timestamps.Count - batch.Count;
                    }

                    if (batch.Any())
                    {
                        await _dbContext.Bars.AddRangeAsync(batch);
                        await _dbContext.SaveChangesAsync();
                        imported += batch.Count;
                    }
                }

                totalImported += imported;
                successCount++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Imported {imported:N0} bars");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                failCount++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Error: {ex.Message}");
                Console.ResetColor();
                _logger.LogError(ex, "Failed to import {File}", file);
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Batch Import Complete");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine($"Files processed: {csvFiles.Length}");
        Console.WriteLine($"Successful: {successCount}");
        Console.WriteLine($"Failed: {failCount}");
        Console.WriteLine($"Total bars imported: {totalImported:N0}");
        Console.WriteLine($"Total bars skipped: {totalSkipped:N0}");
        Console.WriteLine("═══════════════════════════════════════════════════════════");

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    /// <summary>
    /// Validates existing data in the database.
    /// </summary>
    private static async Task ValidateDataAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Validate Existing Data");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        Console.Write("Enter symbol (or press Enter for all): ");
        var symbol = Console.ReadLine()?.ToUpperInvariant();

        var query = _dbContext.Bars.AsQueryable();
        if (!string.IsNullOrWhiteSpace(symbol))
        {
            query = query.Where(b => b.Symbol == symbol);
        }

        var bars = await query.OrderBy(b => b.Symbol).ThenBy(b => b.Timestamp).ToListAsync();

        if (bars.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No data found.");
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"\nValidating {bars.Count:N0} bars...");

        var symbolGroups = bars.GroupBy(b => b.Symbol);

        foreach (var group in symbolGroups)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Symbol: {group.Key}");
            Console.ResetColor();

            var symbolBars = group.OrderBy(b => b.Timestamp).ToList();

            // Detect time gaps (assuming 1 minute timeframe)
            var expectedInterval = TimeSpan.FromMinutes(1);
            var gaps = _dataValidator.FindTimeGaps(symbolBars, expectedInterval);

            if (gaps.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ⚠ Found {gaps.Count} time gaps:");
                foreach (var gap in gaps.Take(10))
                {
                    Console.WriteLine($"    {gap}");
                }
                if (gaps.Count > 10)
                {
                    Console.WriteLine($"    ... and {gaps.Count - 10} more gaps");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ No time gaps detected");
                Console.ResetColor();
            }

            // Detect anomalies
            var anomalies = _dataValidator.FindAnomalies(symbolBars);
            if (anomalies.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ⚠ Found {anomalies.Count} anomalies:");
                foreach (var anomaly in anomalies.Take(10))
                {
                    Console.WriteLine($"    {anomaly}");
                }
                if (anomalies.Count > 10)
                {
                    Console.WriteLine($"    ... and {anomalies.Count - 10} more anomalies");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ No anomalies detected");
                Console.ResetColor();
            }
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    /// <summary>
    /// Deletes data for a symbol and/or date range.
    /// </summary>
    private static async Task DeleteDataAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Delete Data");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        Console.Write("Enter symbol (required): ");
        var symbol = Console.ReadLine()?.ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(symbol))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Symbol is required.");
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var query = _dbContext.Bars.Where(b => b.Symbol == symbol);

        Console.Write("Enter start date (yyyy-MM-dd) or press Enter for all: ");
        var startDateStr = Console.ReadLine();
        DateTime? startDate = null;
        if (!string.IsNullOrWhiteSpace(startDateStr) && DateTime.TryParse(startDateStr, out var sd))
        {
            startDate = sd;
            query = query.Where(b => b.Timestamp >= startDate);
        }

        Console.Write("Enter end date (yyyy-MM-dd) or press Enter for all: ");
        var endDateStr = Console.ReadLine();
        DateTime? endDate = null;
        if (!string.IsNullOrWhiteSpace(endDateStr) && DateTime.TryParse(endDateStr, out var ed))
        {
            endDate = ed.AddDays(1); // Include entire day
            query = query.Where(b => b.Timestamp < endDate);
        }

        var count = await query.CountAsync();

        if (count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No data found matching criteria.");
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ WARNING: This will delete {count:N0} bars for {symbol}");
        if (startDate.HasValue || endDate.HasValue)
        {
            Console.WriteLine($"   Date range: {(startDate?.ToString("yyyy-MM-dd") ?? "any")} to {(endDate?.ToString("yyyy-MM-dd") ?? "any")}");
        }
        Console.ResetColor();
        Console.WriteLine();
        Console.Write("Type 'DELETE' to confirm: ");

        if (Console.ReadLine() != "DELETE")
        {
            Console.WriteLine("Delete cancelled.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("\nDeleting...");
        _dbContext.Bars.RemoveRange(query);
        await _dbContext.SaveChangesAsync();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Deleted {count:N0} bars");
        Console.ResetColor();

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    /// <summary>
    /// Shows detailed statistics per symbol.
    /// </summary>
    private static async Task ShowDetailedStatisticsAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Detailed Statistics");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        var symbols = await _dbContext.Bars
            .Select(b => b.Symbol)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        if (!symbols.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No data found.");
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        foreach (var symbol in symbols)
        {
            var bars = await _dbContext.Bars
                .Where(b => b.Symbol == symbol)
                .OrderBy(b => b.Timestamp)
                .ToListAsync();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n{symbol}");
            Console.WriteLine(new string('─', 60));
            Console.ResetColor();
            Console.WriteLine($"Total bars: {bars.Count:N0}");
            Console.WriteLine($"Date range: {bars.Min(b => b.Timestamp):yyyy-MM-dd HH:mm} to {bars.Max(b => b.Timestamp):yyyy-MM-dd HH:mm}");
            Console.WriteLine($"Price range: ${bars.Min(b => b.Low):N2} - ${bars.Max(b => b.High):N2}");
            Console.WriteLine($"Average volume: {bars.Average(b => b.Volume):N0}");
            Console.WriteLine($"Total volume: {bars.Sum(b => b.Volume):N0}");

            // Trading days
            var tradingDays = bars.Select(b => b.Timestamp.Date).Distinct().Count();
            Console.WriteLine($"Trading days: {tradingDays}");
            Console.WriteLine($"Avg bars/day: {bars.Count / Math.Max(tradingDays, 1):N0}");

            // Indicator coverage
            var barsWithEma9 = bars.Count(b => b.Ema9 > 0);
            var barsWithVwap = bars.Count(b => b.Vwap > 0);
            Console.WriteLine($"Bars with EMA9: {barsWithEma9:N0} ({(barsWithEma9 * 100.0 / bars.Count):F1}%)");
            Console.WriteLine($"Bars with VWAP: {barsWithVwap:N0} ({(barsWithVwap * 100.0 / bars.Count):F1}%)");
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    /// <summary>
    /// Recalculates all indicators for existing data.
    /// </summary>
    private static async Task RecalculateIndicatorsAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Recalculate Indicators");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        Console.Write("Enter symbol (or press Enter for all): ");
        var symbol = Console.ReadLine()?.ToUpperInvariant();

        var query = _dbContext.Bars.AsQueryable();
        if (!string.IsNullOrWhiteSpace(symbol))
        {
            query = query.Where(b => b.Symbol == symbol);
        }

        var symbolGroups = await query
            .GroupBy(b => b.Symbol)
            .Select(g => new { Symbol = g.Key, Count = g.Count() })
            .ToListAsync();

        if (!symbolGroups.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No data found.");
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"\nWill recalculate indicators for:");
        foreach (var group in symbolGroups)
        {
            Console.WriteLine($"  - {group.Symbol}: {group.Count:N0} bars");
        }

        Console.WriteLine();
        Console.Write("Proceed? (y/n): ");
        if (Console.ReadLine()?.ToLower() != "y")
        {
            return;
        }

        foreach (var group in symbolGroups)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Processing {group.Symbol}...");
            Console.ResetColor();

            var bars = await _dbContext.Bars
                .Where(b => b.Symbol == group.Symbol)
                .OrderBy(b => b.Timestamp)
                .ToListAsync();

            Console.WriteLine("Calculating indicators...");
            _indicatorCalculator.CalculateAllIndicators(bars);

            Console.WriteLine("Saving to database...");
            _dbContext.Bars.UpdateRange(bars);
            await _dbContext.SaveChangesAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Updated {bars.Count:N0} bars");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ Indicator recalculation complete");
        Console.ResetColor();

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
}
