using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using TradingStrategyAPI.Database;
using TradingStrategyAPI.DTOs;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for managing custom indicators.
/// Handles CRUD operations and calculations with Redis caching.
/// NOTE: Uses userId = 1 (anonymous user) until Phase 1 (Authentication) is completed.
/// </summary>
public class IndicatorService : IIndicatorService
{
    private readonly TradingDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndicatorService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(1);

    public IndicatorService(
        TradingDbContext context,
        IConnectionMultiplexer redis,
        ILogger<IndicatorService> logger)
    {
        _context = context;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Gets all custom indicators for a user.
    /// </summary>
    public async Task<List<CustomIndicator>> GetUserIndicatorsAsync(int userId = 1)
    {
        _logger.LogInformation("Getting indicators for user {UserId}", userId);

        return await _context.CustomIndicators
            .Where(i => i.UserId == userId)
            .OrderBy(i => i.DisplayName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all public indicators.
    /// </summary>
    public async Task<List<CustomIndicator>> GetPublicIndicatorsAsync()
    {
        _logger.LogInformation("Getting public indicators");

        return await _context.CustomIndicators
            .Where(i => i.IsPublic)
            .OrderBy(i => i.DisplayName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all built-in indicator definitions.
    /// </summary>
    public List<BuiltInIndicatorResponse> GetBuiltInIndicators()
    {
        _logger.LogInformation("Getting built-in indicators");

        return BuiltInIndicator.GetAll().Select(def => new BuiltInIndicatorResponse
        {
            Type = def.Type,
            DisplayName = def.DisplayName,
            Description = def.Description,
            Category = def.Category,
            Parameters = def.Parameters,
            CommonPresets = def.CommonPresets
        }).ToList();
    }

    /// <summary>
    /// Creates a new custom indicator.
    /// </summary>
    public async Task<CustomIndicator> CreateIndicatorAsync(CreateIndicatorRequest request, int userId = 1)
    {
        _logger.LogInformation("Creating indicator {Name} for user {UserId}", request.Name, userId);

        // Validate name uniqueness
        var exists = await _context.CustomIndicators
            .AnyAsync(i => i.UserId == userId && i.Name == request.Name);

        if (exists)
        {
            throw new InvalidOperationException($"Indicator with name '{request.Name}' already exists");
        }

        // Validate parameters JSON
        try
        {
            JsonDocument.Parse(request.Parameters);
        }
        catch (JsonException)
        {
            throw new ArgumentException("Parameters must be valid JSON");
        }

        // Validate formula for custom indicators
        if (request.Type.Equals("Custom", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.Formula))
            {
                throw new ArgumentException("Formula is required for custom indicators");
            }
        }
        else
        {
            // Validate built-in type exists
            var builtIn = BuiltInIndicator.GetByType(request.Type);
            if (builtIn == null)
            {
                throw new ArgumentException($"Unknown indicator type: {request.Type}");
            }
        }

        var indicator = new CustomIndicator
        {
            UserId = userId,
            Name = request.Name,
            DisplayName = request.DisplayName,
            Type = request.Type,
            Parameters = request.Parameters,
            Formula = request.Formula,
            Description = request.Description,
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CustomIndicators.Add(indicator);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created indicator {Id}: {Name}", indicator.Id, indicator.Name);

        return indicator;
    }

    /// <summary>
    /// Updates an existing custom indicator.
    /// </summary>
    public async Task<CustomIndicator> UpdateIndicatorAsync(int id, UpdateIndicatorRequest request, int userId = 1)
    {
        _logger.LogInformation("Updating indicator {Id} for user {UserId}", id, userId);

        var indicator = await _context.CustomIndicators
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (indicator == null)
        {
            throw new KeyNotFoundException($"Indicator {id} not found");
        }

        // Update fields
        if (request.DisplayName != null)
            indicator.DisplayName = request.DisplayName;

        if (request.Parameters != null)
        {
            try
            {
                JsonDocument.Parse(request.Parameters);
                indicator.Parameters = request.Parameters;
            }
            catch (JsonException)
            {
                throw new ArgumentException("Parameters must be valid JSON");
            }
        }

        if (request.Formula != null)
            indicator.Formula = request.Formula;

        if (request.Description != null)
            indicator.Description = request.Description;

        if (request.IsPublic.HasValue)
            indicator.IsPublic = request.IsPublic.Value;

        indicator.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Clear cache for this indicator
        await ClearIndicatorCacheAsync(id);

        _logger.LogInformation("Updated indicator {Id}", id);

        return indicator;
    }

    /// <summary>
    /// Deletes a custom indicator.
    /// </summary>
    public async Task DeleteIndicatorAsync(int id, int userId = 1)
    {
        _logger.LogInformation("Deleting indicator {Id} for user {UserId}", id, userId);

        var indicator = await _context.CustomIndicators
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (indicator == null)
        {
            throw new KeyNotFoundException($"Indicator {id} not found");
        }

        _context.CustomIndicators.Remove(indicator);
        await _context.SaveChangesAsync();

        // Clear cache
        await ClearIndicatorCacheAsync(id);

        _logger.LogInformation("Deleted indicator {Id}", id);
    }

    /// <summary>
    /// Calculates indicator values for a specific date range.
    /// </summary>
    public async Task<CalculateIndicatorResponse> CalculateIndicatorAsync(
        int indicatorId,
        string symbol,
        DateTime startDate,
        DateTime endDate,
        int userId = 1)
    {
        _logger.LogInformation("Calculating indicator {Id} for {Symbol} from {Start} to {End}",
            indicatorId, symbol, startDate, endDate);

        // Check cache first
        var cacheKey = $"indicator:{indicatorId}:{symbol}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
        var cached = await _redis.GetDatabase().StringGetAsync(cacheKey);

        if (cached.HasValue)
        {
            _logger.LogInformation("Cache hit for indicator calculation");
            return JsonSerializer.Deserialize<CalculateIndicatorResponse>(cached!)!;
        }

        // Get indicator
        var indicator = await _context.CustomIndicators
            .FirstOrDefaultAsync(i => i.Id == indicatorId && (i.UserId == userId || i.IsPublic));

        if (indicator == null)
        {
            throw new KeyNotFoundException($"Indicator {indicatorId} not found");
        }

        // Get historical data
        var bars = await _context.Bars
            .Where(b => b.Symbol == symbol.ToUpperInvariant() &&
                       b.Timestamp >= startDate &&
                       b.Timestamp <= endDate)
            .OrderBy(b => b.Timestamp)
            .ToArrayAsync();

        if (bars.Length == 0)
        {
            throw new InvalidOperationException($"No data available for {symbol} in the specified date range");
        }

        // Parse parameters
        var parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(indicator.Parameters);
        if (parameters == null)
        {
            throw new InvalidOperationException("Invalid indicator parameters");
        }

        // Calculate based on type
        decimal[] values;
        Dictionary<string, decimal[]>? additionalSeries = null;

        if (indicator.Type.Equals("Custom", StringComparison.OrdinalIgnoreCase))
        {
            // Custom formula-based indicator
            values = CalculateCustomIndicator(indicator.Formula!, bars);
        }
        else
        {
            // Built-in indicator
            (values, additionalSeries) = CalculateBuiltInIndicator(indicator.Type, parameters, bars);
        }

        var response = new CalculateIndicatorResponse
        {
            IndicatorId = indicator.Id,
            IndicatorName = indicator.DisplayName,
            Type = indicator.Type,
            Values = values,
            Timestamps = bars.Select(b => b.Timestamp).ToArray(),
            AdditionalSeries = additionalSeries,
            Parameters = indicator.Parameters
        };

        // Cache result
        var serialized = JsonSerializer.Serialize(response);
        await _redis.GetDatabase().StringSetAsync(cacheKey, serialized, CacheTtl);

        _logger.LogInformation("Calculated {Count} values for indicator {Id}", values.Length, indicatorId);

        return response;
    }

    /// <summary>
    /// Calculates a built-in indicator.
    /// </summary>
    private (decimal[] values, Dictionary<string, decimal[]>? additionalSeries) CalculateBuiltInIndicator(
        string type,
        Dictionary<string, JsonElement> parameters,
        Bar[] bars)
    {
        switch (type.ToUpperInvariant())
        {
            case "EMA":
                {
                    var period = parameters["period"].GetInt32();
                    var source = parameters.GetValueOrDefault("source", JsonDocument.Parse("\"close\"").RootElement).GetString() ?? "close";
                    var sourceData = IndicatorCalculator.GetSource(bars, source);
                    var values = IndicatorCalculator.CalculateEMA(sourceData, period);
                    return (values, null);
                }

            case "SMA":
                {
                    var period = parameters["period"].GetInt32();
                    var source = parameters.GetValueOrDefault("source", JsonDocument.Parse("\"close\"").RootElement).GetString() ?? "close";
                    var sourceData = IndicatorCalculator.GetSource(bars, source);
                    var values = IndicatorCalculator.CalculateSMA(sourceData, period);
                    return (values, null);
                }

            case "RSI":
                {
                    var period = parameters["period"].GetInt32();
                    var closes = bars.Select(b => b.Close).ToArray();
                    var values = IndicatorCalculator.CalculateRSI(closes, period);
                    return (values, null);
                }

            case "BOLLINGERBANDS":
                {
                    var period = parameters["period"].GetInt32();
                    var stdDev = parameters["stdDev"].GetDecimal();
                    var closes = bars.Select(b => b.Close).ToArray();
                    var (upper, middle, lower) = IndicatorCalculator.CalculateBollingerBands(closes, period, stdDev);
                    var additionalSeries = new Dictionary<string, decimal[]>
                    {
                        ["upper"] = upper,
                        ["lower"] = lower
                    };
                    return (middle, additionalSeries);
                }

            case "MACD":
                {
                    var fastPeriod = parameters["fastPeriod"].GetInt32();
                    var slowPeriod = parameters["slowPeriod"].GetInt32();
                    var signalPeriod = parameters["signalPeriod"].GetInt32();
                    var closes = bars.Select(b => b.Close).ToArray();
                    var (macd, signal, histogram) = IndicatorCalculator.CalculateMACD(closes, fastPeriod, slowPeriod, signalPeriod);
                    var additionalSeries = new Dictionary<string, decimal[]>
                    {
                        ["signal"] = signal,
                        ["histogram"] = histogram
                    };
                    return (macd, additionalSeries);
                }

            case "ATR":
                {
                    var period = parameters["period"].GetInt32();
                    var values = IndicatorCalculator.CalculateATR(bars, period);
                    return (values, null);
                }

            case "STOCHASTIC":
                {
                    var kPeriod = parameters["kPeriod"].GetInt32();
                    var dPeriod = parameters["dPeriod"].GetInt32();
                    var (k, d) = IndicatorCalculator.CalculateStochastic(bars, kPeriod, dPeriod);
                    var additionalSeries = new Dictionary<string, decimal[]>
                    {
                        ["d"] = d
                    };
                    return (k, additionalSeries);
                }

            default:
                throw new NotSupportedException($"Indicator type '{type}' is not supported");
        }
    }

    /// <summary>
    /// Calculates a custom formula-based indicator.
    /// Simple implementation - can be enhanced with expression parsing library.
    /// </summary>
    private decimal[] CalculateCustomIndicator(string formula, Bar[] bars)
    {
        // This is a simplified implementation
        // In production, you'd use a proper expression evaluator like NCalc or DynamicExpresso

        var results = new decimal[bars.Length];

        for (int i = 0; i < bars.Length; i++)
        {
            var bar = bars[i];

            // Replace variables with values
            var expression = formula
                .Replace("close", bar.Close.ToString())
                .Replace("open", bar.Open.ToString())
                .Replace("high", bar.High.ToString())
                .Replace("low", bar.Low.ToString())
                .Replace("volume", bar.Volume.ToString())
                .Replace("vwap", bar.Vwap.ToString());

            // Simple evaluation for common patterns
            // Example: "(close - vwap) / vwap * 100"
            // This is a placeholder - use a proper expression evaluator in production
            try
            {
                // For now, just return a placeholder value
                // TODO: Integrate NCalc or similar library for proper formula evaluation
                results[i] = 0;
            }
            catch
            {
                results[i] = 0;
            }
        }

        return results;
    }

    /// <summary>
    /// Clears cached calculations for an indicator.
    /// </summary>
    private async Task ClearIndicatorCacheAsync(int indicatorId)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: $"indicator:{indicatorId}:*");

        foreach (var key in keys)
        {
            await _redis.GetDatabase().KeyDeleteAsync(key);
        }

        _logger.LogInformation("Cleared cache for indicator {Id}", indicatorId);
    }
}
