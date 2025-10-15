using Microsoft.AspNetCore.Mvc;
using TradingStrategyAPI.DTOs;
using TradingStrategyAPI.Services;

namespace TradingStrategyAPI.Controllers;

/// <summary>
/// Controller for managing custom indicators.
/// NOTE: Uses userId = 1 (anonymous user) until Phase 1 (Authentication) is completed.
/// When Phase 1 is complete:
/// - Add [Authorize] attribute to all endpoints
/// - Replace userId = 1 with User.GetUserId()
/// - Add ownership checks for update/delete operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IndicatorController : ControllerBase
{
    private readonly IIndicatorService _indicatorService;
    private readonly ILogger<IndicatorController> _logger;

    public IndicatorController(
        IIndicatorService indicatorService,
        ILogger<IndicatorController> logger)
    {
        _indicatorService = indicatorService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all built-in indicator definitions.
    /// GET /api/indicator/built-in
    /// </summary>
    [HttpGet("built-in")]
    public ActionResult<List<BuiltInIndicatorResponse>> GetBuiltInIndicators()
    {
        try
        {
            var indicators = _indicatorService.GetBuiltInIndicators();
            return Ok(indicators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting built-in indicators");
            return StatusCode(500, new { message = "Failed to retrieve built-in indicators" });
        }
    }

    /// <summary>
    /// Gets all custom indicators for the current user.
    /// GET /api/indicator/my
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<List<IndicatorResponse>>> GetMyIndicators()
    {
        try
        {
            const int userId = 1; // TODO: Replace with authenticated user ID in Phase 1

            var indicators = await _indicatorService.GetUserIndicatorsAsync(userId);

            var response = indicators.Select(i => new IndicatorResponse
            {
                Id = i.Id,
                UserId = i.UserId,
                Name = i.Name,
                DisplayName = i.DisplayName,
                Type = i.Type,
                Parameters = i.Parameters,
                Formula = i.Formula,
                Description = i.Description,
                IsPublic = i.IsPublic,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user indicators");
            return StatusCode(500, new { message = "Failed to retrieve indicators" });
        }
    }

    /// <summary>
    /// Gets all public indicators.
    /// GET /api/indicator/public
    /// </summary>
    [HttpGet("public")]
    public async Task<ActionResult<List<IndicatorResponse>>> GetPublicIndicators()
    {
        try
        {
            var indicators = await _indicatorService.GetPublicIndicatorsAsync();

            var response = indicators.Select(i => new IndicatorResponse
            {
                Id = i.Id,
                UserId = i.UserId,
                Name = i.Name,
                DisplayName = i.DisplayName,
                Type = i.Type,
                Parameters = i.Parameters,
                Formula = i.Formula,
                Description = i.Description,
                IsPublic = i.IsPublic,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public indicators");
            return StatusCode(500, new { message = "Failed to retrieve public indicators" });
        }
    }

    /// <summary>
    /// Creates a new custom indicator.
    /// POST /api/indicator
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<IndicatorResponse>> CreateIndicator([FromBody] CreateIndicatorRequest request)
    {
        try
        {
            const int userId = 1; // TODO: Replace with authenticated user ID in Phase 1

            var indicator = await _indicatorService.CreateIndicatorAsync(request, userId);

            var response = new IndicatorResponse
            {
                Id = indicator.Id,
                UserId = indicator.UserId,
                Name = indicator.Name,
                DisplayName = indicator.DisplayName,
                Type = indicator.Type,
                Parameters = indicator.Parameters,
                Formula = indicator.Formula,
                Description = indicator.Description,
                IsPublic = indicator.IsPublic,
                CreatedAt = indicator.CreatedAt,
                UpdatedAt = indicator.UpdatedAt
            };

            return CreatedAtAction(nameof(GetIndicator), new { id = indicator.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid indicator creation request");
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid indicator parameters");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating indicator");
            return StatusCode(500, new { message = "Failed to create indicator" });
        }
    }

    /// <summary>
    /// Gets a specific indicator by ID.
    /// GET /api/indicator/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<IndicatorResponse>> GetIndicator(int id)
    {
        try
        {
            const int userId = 1; // TODO: Replace with authenticated user ID in Phase 1

            var indicators = await _indicatorService.GetUserIndicatorsAsync(userId);
            var indicator = indicators.FirstOrDefault(i => i.Id == id);

            if (indicator == null)
            {
                // Check if it's a public indicator
                var publicIndicators = await _indicatorService.GetPublicIndicatorsAsync();
                indicator = publicIndicators.FirstOrDefault(i => i.Id == id);

                if (indicator == null)
                {
                    return NotFound(new { message = $"Indicator {id} not found" });
                }
            }

            var response = new IndicatorResponse
            {
                Id = indicator.Id,
                UserId = indicator.UserId,
                Name = indicator.Name,
                DisplayName = indicator.DisplayName,
                Type = indicator.Type,
                Parameters = indicator.Parameters,
                Formula = indicator.Formula,
                Description = indicator.Description,
                IsPublic = indicator.IsPublic,
                CreatedAt = indicator.CreatedAt,
                UpdatedAt = indicator.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indicator {Id}", id);
            return StatusCode(500, new { message = "Failed to retrieve indicator" });
        }
    }

    /// <summary>
    /// Updates an existing custom indicator.
    /// PUT /api/indicator/{id}
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<IndicatorResponse>> UpdateIndicator(int id, [FromBody] UpdateIndicatorRequest request)
    {
        try
        {
            const int userId = 1; // TODO: Replace with authenticated user ID in Phase 1

            var indicator = await _indicatorService.UpdateIndicatorAsync(id, request, userId);

            var response = new IndicatorResponse
            {
                Id = indicator.Id,
                UserId = indicator.UserId,
                Name = indicator.Name,
                DisplayName = indicator.DisplayName,
                Type = indicator.Type,
                Parameters = indicator.Parameters,
                Formula = indicator.Formula,
                Description = indicator.Description,
                IsPublic = indicator.IsPublic,
                CreatedAt = indicator.CreatedAt,
                UpdatedAt = indicator.UpdatedAt
            };

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Indicator {id} not found" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid update request for indicator {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating indicator {Id}", id);
            return StatusCode(500, new { message = "Failed to update indicator" });
        }
    }

    /// <summary>
    /// Deletes a custom indicator.
    /// DELETE /api/indicator/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteIndicator(int id)
    {
        try
        {
            const int userId = 1; // TODO: Replace with authenticated user ID in Phase 1

            await _indicatorService.DeleteIndicatorAsync(id, userId);

            _logger.LogInformation("Deleted indicator {Id}", id);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Indicator {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting indicator {Id}", id);
            return StatusCode(500, new { message = "Failed to delete indicator" });
        }
    }

    /// <summary>
    /// Calculates indicator values for a specific date range.
    /// GET /api/indicator/{id}/calculate?symbol=ES&startDate=2024-01-01&endDate=2024-12-31
    /// </summary>
    [HttpGet("{id}/calculate")]
    public async Task<ActionResult<CalculateIndicatorResponse>> CalculateIndicator(
        int id,
        [FromQuery] string symbol,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            const int userId = 1; // TODO: Replace with authenticated user ID in Phase 1

            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { message = "Symbol is required" });
            }

            if (endDate <= startDate)
            {
                return BadRequest(new { message = "End date must be after start date" });
            }

            var result = await _indicatorService.CalculateIndicatorAsync(id, symbol, startDate, endDate, userId);

            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Indicator {id} not found" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot calculate indicator {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating indicator {Id}", id);
            return StatusCode(500, new { message = "Failed to calculate indicator" });
        }
    }
}
