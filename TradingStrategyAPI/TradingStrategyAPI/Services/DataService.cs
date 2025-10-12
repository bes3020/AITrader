using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;
using TradingStrategyAPI.Database;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for retrieving and caching market data from PostgreSQL and Redis.
/// Supports multiple futures symbols: ES, NQ, YM, BTC, CL.
/// </summary>
public class DataService : IDataService
{
    private readonly TradingDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DataService> _logger;
    private readonly IDatabase _redisDb;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(90);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the DataService.
    /// </summary>
    public DataService(
        TradingDbContext context,
        IConnectionMultiplexer redis,
        ILogger<DataService> logger)
    {
        _context = context;
        _redis = redis;
        _logger = logger;
        _redisDb = _redis.GetDatabase();
    }

    /// <inheritdoc/>
    public async Task<List<Bar>> GetBarsAsync(string symbol, DateTime start, DateTime end)
    {
        var sw = Stopwatch.StartNew();
        var cacheKey = $"bars:{symbol.ToUpperInvariant()}:{start:yyyyMMddHHmmss}:{end:yyyyMMddHHmmss}";

        // Validate symbol
        if (!FuturesContractSpecs.IsValidSymbol(symbol))
        {
            throw new ArgumentException($"Invalid symbol: {symbol}. Supported symbols: {string.Join(", ", FuturesContractSpecs.GetSupportedSymbols())}", nameof(symbol));
        }

        var symbolUpper = symbol.ToUpperInvariant();

        try
        {
            // Try to get from cache first
            var cachedData = await _redisDb.StringGetAsync(cacheKey);
            if (cachedData.HasValue)
            {
                sw.Stop();
                _logger.LogDebug(
                    "Cache hit for {Symbol} bars {Start} to {End}. Retrieved in {ElapsedMs}ms",
                    symbolUpper, start, end, sw.ElapsedMilliseconds);

                var cachedBars = JsonSerializer.Deserialize<List<Bar>>(cachedData!, JsonOptions);
                return cachedBars ?? new List<Bar>();
            }

            // Cache miss - query database
            _logger.LogDebug("Cache miss for {Symbol} bars {Start} to {End}. Querying database...", symbolUpper, start, end);

            var bars = await _context.Bars
                .AsNoTracking()
                .Where(b => b.Symbol == symbolUpper && b.Timestamp >= start && b.Timestamp <= end)
                .OrderBy(b => b.Timestamp)
                .ToListAsync();

            sw.Stop();
            _logger.LogInformation(
                "Retrieved {Count} {Symbol} bars from database in {ElapsedMs}ms",
                bars.Count, symbolUpper, sw.ElapsedMilliseconds);

            // Cache the results
            if (bars.Any())
            {
                var serialized = JsonSerializer.Serialize(bars, JsonOptions);
                await _redisDb.StringSetAsync(cacheKey, serialized, CacheTtl);
                _logger.LogDebug("Cached {Count} {Symbol} bars with key {CacheKey}", bars.Count, symbolUpper, cacheKey);
            }

            return bars;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error while fetching {Symbol} bars. Falling back to database only.", symbolUpper);

            // Fallback to database without caching
            return await _context.Bars
                .AsNoTracking()
                .Where(b => b.Symbol == symbolUpper && b.Timestamp >= start && b.Timestamp <= end)
                .OrderBy(b => b.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {Symbol} bars from {Start} to {End}", symbolUpper, start, end);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Bar?> GetBarAsync(string symbol, DateTime timestamp)
    {
        var sw = Stopwatch.StartNew();
        var cacheKey = $"bar:{symbol.ToUpperInvariant()}:{timestamp:yyyyMMddHHmmss}";

        // Validate symbol
        if (!FuturesContractSpecs.IsValidSymbol(symbol))
        {
            throw new ArgumentException($"Invalid symbol: {symbol}. Supported symbols: {string.Join(", ", FuturesContractSpecs.GetSupportedSymbols())}", nameof(symbol));
        }

        var symbolUpper = symbol.ToUpperInvariant();

        try
        {
            // Try to get from cache first
            var cachedData = await _redisDb.StringGetAsync(cacheKey);
            if (cachedData.HasValue)
            {
                sw.Stop();
                _logger.LogDebug(
                    "Cache hit for {Symbol} bar at {Timestamp}. Retrieved in {ElapsedMs}ms",
                    symbolUpper, timestamp, sw.ElapsedMilliseconds);

                return JsonSerializer.Deserialize<Bar>(cachedData!, JsonOptions);
            }

            // Cache miss - query database
            _logger.LogDebug("Cache miss for {Symbol} bar at {Timestamp}. Querying database...", symbolUpper, timestamp);

            var bar = await _context.Bars
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Symbol == symbolUpper && b.Timestamp == timestamp);

            sw.Stop();

            if (bar is not null)
            {
                _logger.LogDebug(
                    "Retrieved {Symbol} bar at {Timestamp} from database in {ElapsedMs}ms",
                    symbolUpper, timestamp, sw.ElapsedMilliseconds);

                // Cache the result
                var serialized = JsonSerializer.Serialize(bar, JsonOptions);
                await _redisDb.StringSetAsync(cacheKey, serialized, CacheTtl);
            }
            else
            {
                _logger.LogDebug(
                    "No {Symbol} bar found at {Timestamp} after {ElapsedMs}ms",
                    symbolUpper, timestamp, sw.ElapsedMilliseconds);
            }

            return bar;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error while fetching {Symbol} bar. Falling back to database only.", symbolUpper);

            // Fallback to database without caching
            return await _context.Bars
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Symbol == symbolUpper && b.Timestamp == timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {Symbol} bar at {Timestamp}", symbolUpper, timestamp);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<(decimal high, decimal low)> GetPreviousDayHighLowAsync(string symbol, DateTime date)
    {
        var sw = Stopwatch.StartNew();
        var dateOnly = date.Date;
        var cacheKey = $"prev_day_hl:{symbol.ToUpperInvariant()}:{dateOnly:yyyyMMdd}";

        // Validate symbol
        if (!FuturesContractSpecs.IsValidSymbol(symbol))
        {
            throw new ArgumentException($"Invalid symbol: {symbol}. Supported symbols: {string.Join(", ", FuturesContractSpecs.GetSupportedSymbols())}", nameof(symbol));
        }

        var symbolUpper = symbol.ToUpperInvariant();

        try
        {
            // Try to get from cache first
            var cachedData = await _redisDb.StringGetAsync(cacheKey);
            if (cachedData.HasValue)
            {
                sw.Stop();
                _logger.LogDebug(
                    "Cache hit for {Symbol} previous day high/low for {Date}. Retrieved in {ElapsedMs}ms",
                    symbolUpper, dateOnly, sw.ElapsedMilliseconds);

                var cached = JsonSerializer.Deserialize<(decimal high, decimal low)>(cachedData!, JsonOptions);
                return cached;
            }

            // Cache miss - query database
            _logger.LogDebug("Cache miss for {Symbol} previous day high/low for {Date}. Querying database...", symbolUpper, dateOnly);

            // Find the previous trading day (could be 1-3 days back due to weekends/holidays)
            var maxLookbackDays = 7; // Look back up to a week to handle holidays
            var startSearch = dateOnly.AddDays(-maxLookbackDays);

            var previousDayBars = await _context.Bars
                .AsNoTracking()
                .Where(b => b.Symbol == symbolUpper && b.Timestamp >= startSearch && b.Timestamp < dateOnly)
                .OrderByDescending(b => b.Timestamp)
                .ToListAsync();

            if (!previousDayBars.Any())
            {
                sw.Stop();
                _logger.LogWarning(
                    "No {Symbol} previous day data found for {Date} after {ElapsedMs}ms. Searched back to {StartSearch}",
                    symbolUpper, dateOnly, sw.ElapsedMilliseconds, startSearch);

                return (0m, 0m);
            }

            // Get the most recent previous trading day
            var previousDay = previousDayBars.First().Timestamp.Date;
            var previousDayData = previousDayBars
                .Where(b => b.Timestamp.Date == previousDay)
                .ToList();

            var high = previousDayData.Max(b => b.High);
            var low = previousDayData.Min(b => b.Low);

            sw.Stop();
            _logger.LogInformation(
                "{Symbol} previous day ({PreviousDay}) high/low for {Date}: High={High}, Low={Low}. Retrieved in {ElapsedMs}ms from {BarCount} bars",
                symbolUpper, previousDay, dateOnly, high, low, sw.ElapsedMilliseconds, previousDayData.Count);

            // Cache the result
            var result = (high, low);
            var serialized = JsonSerializer.Serialize(result, JsonOptions);
            await _redisDb.StringSetAsync(cacheKey, serialized, CacheTtl);

            return result;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error while fetching {Symbol} previous day high/low. Falling back to database only.", symbolUpper);

            // Fallback to database without caching
            var startSearch = dateOnly.AddDays(-7);
            var previousDayBars = await _context.Bars
                .AsNoTracking()
                .Where(b => b.Symbol == symbolUpper && b.Timestamp >= startSearch && b.Timestamp < dateOnly)
                .OrderByDescending(b => b.Timestamp)
                .ToListAsync();

            if (!previousDayBars.Any())
            {
                return (0m, 0m);
            }

            var previousDay = previousDayBars.First().Timestamp.Date;
            var previousDayData = previousDayBars
                .Where(b => b.Timestamp.Date == previousDay)
                .ToList();

            return (previousDayData.Max(b => b.High), previousDayData.Min(b => b.Low));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {Symbol} previous day high/low for {Date}", symbolUpper, dateOnly);
            throw;
        }
    }
}
