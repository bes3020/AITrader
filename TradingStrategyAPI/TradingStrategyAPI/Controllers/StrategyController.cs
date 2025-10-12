using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TradingStrategyAPI.Database;
using TradingStrategyAPI.DTOs;
using TradingStrategyAPI.Models;
using TradingStrategyAPI.Services;

namespace TradingStrategyAPI.Controllers;

/// <summary>
/// Controller for managing and analyzing trading strategies.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StrategyController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly IStrategyScanner _scanner;
    private readonly IResultsAnalyzer _analyzer;
    private readonly TradingDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<StrategyController> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(30);

    public StrategyController(
        IAIService aiService,
        IStrategyScanner scanner,
        IResultsAnalyzer analyzer,
        TradingDbContext context,
        IConnectionMultiplexer redis,
        ILogger<StrategyController> logger)
    {
        _aiService = aiService;
        _scanner = scanner;
        _analyzer = analyzer;
        _context = context;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a trading strategy from natural language description.
    /// </summary>
    /// <param name="request">Strategy description and backtest parameters</param>
    /// <returns>Complete strategy analysis with performance metrics</returns>
    /// <response code="200">Strategy analyzed successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error during analysis</response>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(AnalyzeStrategyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AnalyzeStrategyResponse>> AnalyzeStrategy([FromBody] AnalyzeStrategyRequest request)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Validate symbol
            if (!FuturesContractSpecs.IsValidSymbol(request.Symbol))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid symbol",
                    Detail = $"Symbol '{request.Symbol}' is not supported. Supported symbols: {string.Join(", ", FuturesContractSpecs.GetSupportedSymbols())}",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Validate date range
            if (request.EndDate <= request.StartDate)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid date range",
                    Detail = "End date must be after start date",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (request.EndDate > DateTime.UtcNow)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid end date",
                    Detail = "End date cannot be in the future",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation("Analyzing strategy on {Symbol}: {Description}",
                request.Symbol,
                request.Description.Length > 100 ? request.Description[..100] + "..." : request.Description);

            // Check cache (include symbol in cache key)
            var cacheKey = $"result:{request.Symbol.ToUpperInvariant()}:{ComputeHash(request.Description)}:{request.StartDate:yyyyMMdd}:{request.EndDate:yyyyMMdd}";
            var cachedResult = await _redis.GetDatabase().StringGetAsync(cacheKey);

            if (cachedResult.HasValue)
            {
                sw.Stop();
                _logger.LogInformation("Cache hit for strategy analysis. Retrieved in {ElapsedMs}ms", sw.ElapsedMilliseconds);

                var cached = JsonSerializer.Deserialize<AnalyzeStrategyResponse>(cachedResult!);
                return Ok(cached);
            }

            // Step 1: Parse strategy using AI
            _logger.LogInformation("Parsing strategy with {Provider}...", _aiService.ProviderName);
            var strategy = await _aiService.ParseStrategyAsync(request.Description);
            strategy.Description = request.Description;
            strategy.Symbol = request.Symbol.ToUpperInvariant();
            strategy.UserId = 1; // Default anonymous user
            strategy.CreatedAt = DateTime.UtcNow;
            strategy.UpdatedAt = DateTime.UtcNow;

            // Save strategy to database to get an ID
            _context.Strategies.Add(strategy);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved strategy with ID {StrategyId} for anonymous user", strategy.Id);

            // Step 2: Scan historical data with symbol
            _logger.LogInformation("Scanning {Symbol} historical data from {Start} to {End}...",
                request.Symbol, request.StartDate, request.EndDate);
            var trades = await _scanner.ScanAsync(strategy, request.Symbol, request.StartDate, request.EndDate);

            // Step 3: Analyze results
            _logger.LogInformation("Analyzing {TradeCount} trades...", trades.Count);
            var result = await _analyzer.AnalyzeAsync(trades, strategy);

            // Link result to strategy
            result.StrategyId = strategy.Id;

            // Add trades to result
            foreach (var trade in trades)
            {
                trade.StrategyResultId = result.Id;
                result.AllTrades.Add(trade);
            }

            // Save result and trades to database
            _context.StrategyResults.Add(result);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved strategy result with ID {ResultId}", result.Id);

            sw.Stop();

            var response = new AnalyzeStrategyResponse
            {
                Strategy = strategy,
                Result = result,
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                AiProvider = _aiService.ProviderName
            };

            // Cache result
            var serialized = JsonSerializer.Serialize(response);
            await _redis.GetDatabase().StringSetAsync(cacheKey, serialized, CacheTtl);

            _logger.LogInformation("Strategy analysis completed in {ElapsedMs}ms. Total trades: {Count}, Win rate: {WinRate:P1}",
                sw.ElapsedMilliseconds, result.TotalTrades, result.WinRate);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error parsing or analyzing strategy after {ElapsedMs}ms", sw.ElapsedMilliseconds);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Strategy analysis failed",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Unexpected error during strategy analysis after {ElapsedMs}ms", sw.ElapsedMilliseconds);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An unexpected error occurred during strategy analysis",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Retrieves a strategy by ID.
    /// </summary>
    /// <param name="id">Strategy ID</param>
    /// <returns>Strategy details</returns>
    /// <response code="200">Strategy found</response>
    /// <response code="404">Strategy not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Strategy), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Strategy>> GetStrategy(int id)
    {
        try
        {
            var strategy = await _context.Strategies
                .Include(s => s.EntryConditions)
                .Include(s => s.StopLoss)
                .Include(s => s.TakeProfit)
                .Include(s => s.Results)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (strategy == null)
            {
                _logger.LogWarning("Strategy {Id} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Strategy not found",
                    Detail = $"Strategy with ID {id} does not exist",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Retrieved strategy {Id}: {Name}", id, strategy.Name);
            return Ok(strategy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving strategy {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error retrieving strategy",
                Detail = "An error occurred while retrieving the strategy",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Saves a strategy to the database.
    /// </summary>
    /// <param name="strategy">Strategy to save</param>
    /// <returns>Saved strategy with generated ID</returns>
    /// <response code="201">Strategy saved successfully</response>
    /// <response code="400">Invalid strategy data</response>
    [HttpPost("save")]
    [ProducesResponseType(typeof(Strategy), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Strategy>> SaveStrategy([FromBody] Strategy strategy)
    {
        try
        {
            // Validate strategy
            if (!strategy.IsValidStrategy())
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid strategy",
                    Detail = "Strategy must have a name, direction, entry conditions, stop loss, and take profit",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Set timestamps
            strategy.CreatedAt = DateTime.UtcNow;
            strategy.UpdatedAt = DateTime.UtcNow;

            // Add to database
            _context.Strategies.Add(strategy);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved strategy {Id}: {Name}", strategy.Id, strategy.Name);

            return CreatedAtAction(nameof(GetStrategy), new { id = strategy.Id }, strategy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving strategy");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error saving strategy",
                Detail = "An error occurred while saving the strategy",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Lists all strategies for the authenticated user.
    /// </summary>
    /// <param name="symbol">Optional: Filter strategies by symbol (ES, NQ, YM, BTC, CL)</param>
    /// <returns>List of user's strategies</returns>
    /// <response code="200">Strategies retrieved successfully</response>
    /// <response code="400">Invalid symbol filter</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("list")]
    [Authorize]
    [ProducesResponseType(typeof(List<Strategy>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<Strategy>>> ListStrategies([FromQuery] string? symbol = null)
    {
        try
        {
            // Validate symbol if provided
            if (!string.IsNullOrEmpty(symbol) && !FuturesContractSpecs.IsValidSymbol(symbol))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid symbol",
                    Detail = $"Symbol '{symbol}' is not supported. Supported symbols: {string.Join(", ", FuturesContractSpecs.GetSupportedSymbols())}",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // TODO: Get user ID from claims
            // var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Build query
            var query = _context.Strategies
                .Include(s => s.EntryConditions)
                .Include(s => s.StopLoss)
                .Include(s => s.TakeProfit)
                .AsQueryable();

            // Apply symbol filter if provided
            if (!string.IsNullOrEmpty(symbol))
            {
                var symbolUpper = symbol.ToUpperInvariant();
                query = query.Where(s => s.Symbol == symbolUpper);
                _logger.LogInformation("Filtering strategies by symbol: {Symbol}", symbolUpper);
            }

            // For now, return all strategies (remove user filter in production)
            var strategies = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} strategies{Filter}",
                strategies.Count,
                string.IsNullOrEmpty(symbol) ? "" : $" (filtered by {symbol})");

            return Ok(strategies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing strategies");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error listing strategies",
                Detail = "An error occurred while retrieving strategies",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Refines an existing strategy by adding new conditions.
    /// </summary>
    /// <param name="request">Original strategy ID and additional condition</param>
    /// <returns>Refined strategy analysis with comparison to original</returns>
    /// <response code="200">Strategy refined successfully</response>
    /// <response code="404">Original strategy not found</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpPost("refine")]
    [ProducesResponseType(typeof(AnalyzeStrategyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnalyzeStrategyResponse>> RefineStrategy([FromBody] RefineStrategyRequest request)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Validate date range
            if (request.EndDate <= request.StartDate)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid date range",
                    Detail = "End date must be after start date",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Load original strategy
            var originalStrategy = await _context.Strategies
                .Include(s => s.EntryConditions)
                .Include(s => s.StopLoss)
                .Include(s => s.TakeProfit)
                .FirstOrDefaultAsync(s => s.Id == request.StrategyId);

            if (originalStrategy == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Strategy not found",
                    Detail = $"Strategy with ID {request.StrategyId} does not exist",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Refining strategy {Id} with condition: {Condition}",
                request.StrategyId, request.AdditionalCondition);

            // Build refined description
            var refinedDescription = $"{originalStrategy.Description} {request.AdditionalCondition}";

            // Parse refined strategy
            var refinedStrategy = await _aiService.ParseStrategyAsync(refinedDescription);
            refinedStrategy.Name = $"{originalStrategy.Name} (Refined)";
            refinedStrategy.Description = refinedDescription;
            refinedStrategy.Version = originalStrategy.Version + 1;

            // Scan and analyze with symbol
            var trades = await _scanner.ScanAsync(refinedStrategy, request.Symbol, request.StartDate, request.EndDate);
            var result = await _analyzer.AnalyzeAsync(trades, refinedStrategy);

            // Add trades to result
            foreach (var trade in trades)
            {
                result.AllTrades.Add(trade);
            }

            sw.Stop();

            var response = new AnalyzeStrategyResponse
            {
                Strategy = refinedStrategy,
                Result = result,
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                AiProvider = _aiService.ProviderName
            };

            _logger.LogInformation("Strategy refinement completed in {ElapsedMs}ms. " +
                "Original win rate: N/A, Refined win rate: {RefinedWinRate:P1}",
                sw.ElapsedMilliseconds, result.WinRate);

            return Ok(response);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error refining strategy after {ElapsedMs}ms", sw.ElapsedMilliseconds);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Strategy refinement failed",
                Detail = "An error occurred during strategy refinement",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets a list of all supported futures symbols with their specifications and available data ranges.
    /// </summary>
    /// <returns>List of supported symbols with metadata</returns>
    /// <response code="200">Symbols retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("symbols")]
    [ProducesResponseType(typeof(List<SymbolInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<SymbolInfo>>> GetSymbols()
    {
        try
        {
            _logger.LogInformation("Retrieving supported symbols information");

            var symbols = new List<SymbolInfo>();
            var supportedSymbols = FuturesContractSpecs.GetSupportedSymbols();

            foreach (var symbol in supportedSymbols)
            {
                // Get date range and bar count for this symbol from database
                var symbolData = await _context.Bars
                    .AsNoTracking()
                    .Where(b => b.Symbol == symbol)
                    .GroupBy(b => b.Symbol)
                    .Select(g => new
                    {
                        MinDate = g.Min(b => b.Timestamp),
                        MaxDate = g.Max(b => b.Timestamp),
                        Count = g.Count()
                    })
                    .FirstOrDefaultAsync();

                var symbolInfo = new SymbolInfo
                {
                    Symbol = symbol,
                    Name = FuturesContractSpecs.GetDisplayName(symbol),
                    PointValue = FuturesContractSpecs.GetPointValue(symbol),
                    TickSize = FuturesContractSpecs.GetTickSize(symbol),
                    TickValue = FuturesContractSpecs.GetTickValue(symbol),
                    MinDate = symbolData?.MinDate,
                    MaxDate = symbolData?.MaxDate,
                    BarCount = symbolData?.Count ?? 0
                };

                symbols.Add(symbolInfo);
            }

            _logger.LogInformation("Retrieved information for {Count} symbols", symbols.Count);
            return Ok(symbols);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving symbol information");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error retrieving symbols",
                Detail = "An error occurred while retrieving symbol information",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets recent strategy evaluation errors for debugging.
    /// </summary>
    /// <param name="count">Number of recent errors to retrieve (default: 50, max: 200)</param>
    /// <returns>List of recent errors with suggested fixes</returns>
    /// <response code="200">Errors retrieved successfully</response>
    [HttpGet("errors")]
    [ProducesResponseType(typeof(List<StrategyError>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StrategyError>>> GetRecentErrors([FromQuery] int count = 50)
    {
        try
        {
            count = Math.Min(count, 200); // Cap at 200

            var errors = await _context.StrategyErrors
                .Include(e => e.Strategy)
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} recent errors", errors.Count);
            return Ok(errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving errors");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error retrieving errors",
                Detail = "An error occurred while retrieving error logs",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets error statistics and patterns for analysis.
    /// </summary>
    /// <returns>Error statistics including common patterns and suggested fixes</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    [HttpGet("errors/statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetErrorStatistics()
    {
        try
        {
            var errors = await _context.StrategyErrors
                .Where(e => e.Timestamp >= DateTime.UtcNow.AddDays(-7))
                .ToListAsync();

            var statistics = new
            {
                TotalErrors = errors.Count,
                UnresolvedErrors = errors.Count(e => !e.IsResolved),
                ErrorsByType = errors.GroupBy(e => e.ErrorType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),
                ErrorsBySeverity = errors.GroupBy(e => e.Severity)
                    .Select(g => new { Severity = g.Key, Count = g.Count() })
                    .ToList(),
                TopFailedExpressions = errors
                    .Where(e => !string.IsNullOrEmpty(e.FailedExpression))
                    .GroupBy(e => new { e.FailedExpression, e.SuggestedFix })
                    .Select(g => new
                    {
                        Expression = g.Key.FailedExpression,
                        Count = g.Count(),
                        SuggestedFix = g.Key.SuggestedFix
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToList()
            };

            _logger.LogInformation("Retrieved error statistics: {Total} total, {Unresolved} unresolved",
                statistics.TotalErrors, statistics.UnresolvedErrors);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving error statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error retrieving statistics",
                Detail = "An error occurred while calculating error statistics",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Computes SHA256 hash of a string for cache keys.
    /// </summary>
    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
