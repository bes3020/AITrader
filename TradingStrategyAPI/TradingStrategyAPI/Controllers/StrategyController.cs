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
    private readonly ITradeAnalyzer _tradeAnalyzer;
    private readonly IStrategyManager _strategyManager;
    private readonly TradingDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<StrategyController> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(30);

    public StrategyController(
        IAIService aiService,
        IStrategyScanner scanner,
        IResultsAnalyzer analyzer,
        ITradeAnalyzer tradeAnalyzer,
        IStrategyManager strategyManager,
        TradingDbContext context,
        IConnectionMultiplexer redis,
        ILogger<StrategyController> logger)
    {
        _aiService = aiService;
        _scanner = scanner;
        _analyzer = analyzer;
        _tradeAnalyzer = tradeAnalyzer;
        _strategyManager = strategyManager;
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
    // [Authorize] // Temporarily disabled for development
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
                .Include(s => s.Results)
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
    /// Gets paginated list of trades for a strategy result with optional filters.
    /// </summary>
    /// <param name="strategyId">Strategy ID</param>
    /// <param name="resultId">Result ID</param>
    /// <param name="result">Filter by result type: win/loss/timeout</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="sortBy">Sort field: pnl, entryTime, duration (default: entryTime)</param>
    /// <response code="200">Trades retrieved successfully</response>
    /// <response code="404">Strategy or result not found</response>
    [HttpGet("{strategyId}/results/{resultId}/trades")]
    [ProducesResponseType(typeof(TradeListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TradeListResponse>> GetTrades(
        int strategyId,
        int resultId,
        [FromQuery] string? result = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "entryTime")
    {
        try
        {
            // Validate parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Check if result exists
            var strategyResult = await _context.StrategyResults
                .FirstOrDefaultAsync(r => r.Id == resultId && r.StrategyId == strategyId);

            if (strategyResult == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Result not found",
                    Detail = $"Strategy result with ID {resultId} not found for strategy {strategyId}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Build query
            var query = _context.TradeResults
                .Where(t => t.StrategyResultId == resultId)
                .AsQueryable();

            // Apply result filter
            if (!string.IsNullOrEmpty(result))
            {
                var resultLower = result.ToLowerInvariant();
                query = query.Where(t => t.Result == resultLower);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "pnl" => query.OrderByDescending(t => t.Pnl),
                "duration" => query.OrderByDescending(t => t.BarsHeld),
                _ => query.OrderBy(t => t.EntryTime)
            };

            // Apply pagination
            var trades = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate summary
            var allTrades = await _context.TradeResults
                .Where(t => t.StrategyResultId == resultId)
                .ToListAsync();

            var summary = CalculateTradeListSummary(allTrades);

            var response = new TradeListResponse
            {
                Trades = trades,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                Summary = summary
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trades for result {ResultId}", resultId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error retrieving trades",
                Detail = "An error occurred while retrieving trades",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets detailed information about a single trade including chart data and analysis.
    /// </summary>
    /// <param name="strategyId">Strategy ID</param>
    /// <param name="resultId">Result ID</param>
    /// <param name="tradeId">Trade ID</param>
    /// <response code="200">Trade detail retrieved successfully</response>
    /// <response code="404">Trade not found</response>
    [HttpGet("{strategyId}/results/{resultId}/trades/{tradeId}")]
    [ProducesResponseType(typeof(TradeDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TradeDetailResponse>> GetTradeDetail(
        int strategyId,
        int resultId,
        int tradeId)
    {
        try
        {
            // Get trade with analysis
            var trade = await _context.TradeResults
                .Include(t => t.Analysis)
                .FirstOrDefaultAsync(t => t.Id == tradeId && t.StrategyResultId == resultId);

            if (trade == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Trade not found",
                    Detail = $"Trade with ID {tradeId} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Get strategy for analysis if needed
            var strategy = await _context.Strategies
                .Include(s => s.EntryConditions)
                .FirstOrDefaultAsync(s => s.Id == strategyId);

            if (strategy == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Strategy not found",
                    Detail = $"Strategy with ID {strategyId} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Generate analysis if not exists
            if (trade.Analysis == null)
            {
                var analysis = await _tradeAnalyzer.AnalyzeTradeAsync(trade, strategy);
                analysis.TradeResultId = trade.Id;
                _context.TradeAnalyses.Add(analysis);
                await _context.SaveChangesAsync();
                trade.Analysis = analysis;
            }

            // Parse chart data from stored JSON
            List<BarData>? chartData = null;
            if (!string.IsNullOrEmpty(trade.SetupBars) && !string.IsNullOrEmpty(trade.TradeBars))
            {
                chartData = ParseChartData(trade.SetupBars, trade.TradeBars);
            }

            var response = new TradeDetailResponse
            {
                Trade = trade,
                Analysis = trade.Analysis,
                ChartData = chartData,
                IndicatorSeries = null // Could be populated from indicator values JSON
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trade detail for trade {TradeId}", tradeId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error retrieving trade detail",
                Detail = "An error occurred while retrieving trade details",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets identified patterns across all trades in a result.
    /// </summary>
    /// <param name="strategyId">Strategy ID</param>
    /// <param name="resultId">Result ID</param>
    /// <response code="200">Patterns retrieved successfully</response>
    /// <response code="404">Result not found</response>
    [HttpGet("{strategyId}/results/{resultId}/patterns")]
    [ProducesResponseType(typeof(List<TradePattern>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TradePattern>>> GetPatterns(int strategyId, int resultId)
    {
        try
        {
            // Verify result exists
            var exists = await _context.StrategyResults
                .AnyAsync(r => r.Id == resultId && r.StrategyId == strategyId);

            if (!exists)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Result not found",
                    Detail = $"Strategy result with ID {resultId} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Get all trades
            var trades = await _context.TradeResults
                .Where(t => t.StrategyResultId == resultId)
                .ToListAsync();

            // Find patterns
            var patterns = await _tradeAnalyzer.FindPatternsAsync(trades);

            return Ok(patterns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patterns for result {ResultId}", resultId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error retrieving patterns",
                Detail = "An error occurred while analyzing trade patterns",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets performance heatmap data for visualization.
    /// </summary>
    /// <param name="strategyId">Strategy ID</param>
    /// <param name="resultId">Result ID</param>
    /// <param name="dimension">Dimension to analyze: hour, day, or condition</param>
    /// <response code="200">Heatmap data retrieved successfully</response>
    /// <response code="400">Invalid dimension parameter</response>
    /// <response code="404">Result not found</response>
    [HttpGet("{strategyId}/results/{resultId}/heatmap")]
    [ProducesResponseType(typeof(HeatmapData), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HeatmapData>> GetHeatmap(
        int strategyId,
        int resultId,
        [FromQuery] string dimension = "hour")
    {
        try
        {
            // Validate dimension
            var validDimensions = new[] { "hour", "day", "condition" };
            if (!validDimensions.Contains(dimension.ToLowerInvariant()))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid dimension",
                    Detail = $"Dimension must be one of: {string.Join(", ", validDimensions)}",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Verify result exists
            var exists = await _context.StrategyResults
                .AnyAsync(r => r.Id == resultId && r.StrategyId == strategyId);

            if (!exists)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Result not found",
                    Detail = $"Strategy result with ID {resultId} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Get all trades
            var trades = await _context.TradeResults
                .Where(t => t.StrategyResultId == resultId)
                .ToListAsync();

            // Generate heatmap
            var heatmap = await _tradeAnalyzer.GenerateHeatmapAsync(trades, dimension);

            return Ok(heatmap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating heatmap for result {ResultId}", resultId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error generating heatmap",
                Detail = "An error occurred while generating heatmap data",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    // Helper methods

    private TradeListSummary CalculateTradeListSummary(List<TradeResult> trades)
    {
        var wins = trades.Where(t => t.Result == "win").ToList();
        var losses = trades.Where(t => t.Result == "loss").ToList();
        var timeouts = trades.Where(t => t.Result == "timeout").ToList();

        return new TradeListSummary
        {
            TotalTrades = trades.Count,
            Wins = wins.Count,
            Losses = losses.Count,
            Timeouts = timeouts.Count,
            TotalPnl = trades.Sum(t => t.Pnl),
            AvgPnl = trades.Any() ? trades.Average(t => t.Pnl) : 0,
            WinRate = trades.Any() ? (decimal)wins.Count / trades.Count * 100 : 0,
            AvgWin = wins.Any() ? wins.Average(t => t.Pnl) : 0,
            AvgLoss = losses.Any() ? losses.Average(t => t.Pnl) : 0,
            LargestWin = wins.Any() ? wins.Max(t => t.Pnl) : 0,
            LargestLoss = losses.Any() ? losses.Min(t => t.Pnl) : 0
        };
    }

    private List<BarData> ParseChartData(string setupBarsJson, string tradeBarsJson)
    {
        try
        {
            var setupBars = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(setupBarsJson) ?? new List<Dictionary<string, JsonElement>>();
            var tradeBars = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(tradeBarsJson) ?? new List<Dictionary<string, JsonElement>>();

            var allBars = setupBars.Concat(tradeBars).ToList();

            return allBars.Select(b => new BarData
            {
                Timestamp = b["t"].GetDateTime(),
                Open = b["o"].GetDecimal(),
                High = b["h"].GetDecimal(),
                Low = b["l"].GetDecimal(),
                Close = b["c"].GetDecimal(),
                Volume = b["v"].GetInt64()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing chart data");
            return new List<BarData>();
        }
    }

    // ==================== CRUD OPERATIONS ====================

    /// <summary>
    /// Creates a new strategy.
    /// POST /api/strategy
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<StrategyDetailResponse>> CreateStrategy([FromBody] CreateStrategyRequest request)
    {
        try
        {
            var result = await _strategyManager.CreateStrategyAsync(request);
            return CreatedAtAction(nameof(GetStrategyDetail), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating strategy");
            return StatusCode(500, new { message = "Failed to create strategy" });
        }
    }

    /// <summary>
    /// Gets detailed strategy information.
    /// GET /api/strategy/{id}/detail
    /// </summary>
    [HttpGet("{id}/detail")]
    public async Task<ActionResult<StrategyDetailResponse>> GetStrategyDetail(int id)
    {
        try
        {
            var result = await _strategyManager.GetStrategyAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Strategy {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving strategy {Id}", id);
            return StatusCode(500, new { message = "Failed to retrieve strategy" });
        }
    }

    /// <summary>
    /// Updates an existing strategy.
    /// PUT /api/strategy/{id}
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<StrategyDetailResponse>> UpdateStrategy(int id, [FromBody] UpdateStrategyRequest request)
    {
        try
        {
            var result = await _strategyManager.UpdateStrategyAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Strategy {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating strategy {Id}", id);
            return StatusCode(500, new { message = "Failed to update strategy" });
        }
    }

    /// <summary>
    /// Deletes (archives) a strategy.
    /// DELETE /api/strategy/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteStrategy(int id)
    {
        try
        {
            await _strategyManager.DeleteStrategyAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Strategy {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting strategy {Id}", id);
            return StatusCode(500, new { message = "Failed to delete strategy" });
        }
    }

    /// <summary>
    /// Duplicates a strategy.
    /// POST /api/strategy/{id}/duplicate
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public async Task<ActionResult<StrategyDetailResponse>> DuplicateStrategy(int id, [FromBody] DuplicateRequest request)
    {
        try
        {
            var result = await _strategyManager.DuplicateStrategyAsync(id, request.NewName);
            return CreatedAtAction(nameof(GetStrategyDetail), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Strategy {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating strategy {Id}", id);
            return StatusCode(500, new { message = "Failed to duplicate strategy" });
        }
    }

    /// <summary>
    /// Creates a new version of a strategy.
    /// POST /api/strategy/{id}/version
    /// </summary>
    [HttpPost("{id}/version")]
    public async Task<ActionResult<StrategyDetailResponse>> CreateVersion(int id, [FromBody] CreateVersionRequest request)
    {
        try
        {
            var result = await _strategyManager.CreateVersionAsync(id, request);
            return CreatedAtAction(nameof(GetStrategyDetail), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Strategy {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version for strategy {Id}", id);
            return StatusCode(500, new { message = "Failed to create version" });
        }
    }

    /// <summary>
    /// Gets all versions of a strategy.
    /// GET /api/strategy/{id}/versions
    /// </summary>
    [HttpGet("{id}/versions")]
    public async Task<ActionResult<List<StrategyVersionSummary>>> GetVersions(int id)
    {
        try
        {
            var result = await _strategyManager.GetVersionsAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Strategy {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving versions for strategy {Id}", id);
            return StatusCode(500, new { message = "Failed to retrieve versions" });
        }
    }

    /// <summary>
    /// Toggles favorite status.
    /// POST /api/strategy/{id}/favorite
    /// </summary>
    [HttpPost("{id}/favorite")]
    public async Task<ActionResult<object>> ToggleFavorite(int id)
    {
        try
        {
            var isFavorite = await _strategyManager.ToggleFavoriteAsync(id);
            return Ok(new { isFavorite });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Strategy {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite for strategy {Id}", id);
            return StatusCode(500, new { message = "Failed to toggle favorite" });
        }
    }

    /// <summary>
    /// Archives or unarchives a strategy.
    /// POST /api/strategy/{id}/archive
    /// </summary>
    [HttpPost("{id}/archive")]
    public async Task<ActionResult> ArchiveStrategy(int id, [FromBody] ArchiveRequest request)
    {
        try
        {
            await _strategyManager.ArchiveStrategyAsync(id, request.Archive);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Strategy {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving strategy {Id}", id);
            return StatusCode(500, new { message = "Failed to archive strategy" });
        }
    }

    /// <summary>
    /// Exports a strategy to JSON.
    /// POST /api/strategy/{id}/export
    /// </summary>
    [HttpPost("{id}/export")]
    public async Task<ActionResult<StrategyExportFormat>> ExportStrategy(int id)
    {
        try
        {
            var result = await _strategyManager.ExportStrategyAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Strategy {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting strategy {Id}", id);
            return StatusCode(500, new { message = "Failed to export strategy" });
        }
    }

    /// <summary>
    /// Imports a strategy from JSON.
    /// POST /api/strategy/import
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<StrategyDetailResponse>> ImportStrategy([FromBody] ImportStrategyRequest request)
    {
        try
        {
            var result = await _strategyManager.ImportStrategyAsync(request);
            return CreatedAtAction(nameof(GetStrategyDetail), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing strategy");
            return StatusCode(500, new { message = "Failed to import strategy" });
        }
    }

    /// <summary>
    /// Searches strategies with filters.
    /// POST /api/strategy/search
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<SearchStrategiesResponse>> SearchStrategies([FromBody] SearchStrategiesRequest request)
    {
        try
        {
            var result = await _strategyManager.SearchStrategiesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching strategies");
            return StatusCode(500, new { message = "Failed to search strategies" });
        }
    }

    /// <summary>
    /// Compares multiple strategies.
    /// POST /api/strategy/compare
    /// </summary>
    [HttpPost("compare")]
    public async Task<ActionResult<StrategyComparisonResponse>> CompareStrategies([FromBody] CompareStrategiesRequest request)
    {
        try
        {
            var result = await _strategyManager.CompareStrategiesAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing strategies");
            return StatusCode(500, new { message = "Failed to compare strategies" });
        }
    }

    // Helper request DTOs for endpoints that need simple parameters
    public class DuplicateRequest { public required string NewName { get; set; } }
    public class ArchiveRequest { public bool Archive { get; set; } }

    /// <summary>
    /// Computes SHA256 hash of a string for cache keys.
    /// </summary>
    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
