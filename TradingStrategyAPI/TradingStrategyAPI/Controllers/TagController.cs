using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingStrategyAPI.Database;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Controllers;

/// <summary>
/// Controller for managing strategy tags.
/// NOTE: Uses userId = 1 (anonymous user) until Phase 1 (Authentication) is completed.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TagController : ControllerBase
{
    private readonly TradingDbContext _context;
    private readonly ILogger<TagController> _logger;

    public TagController(TradingDbContext context, ILogger<TagController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all tags for the current user.
    /// GET /api/tag
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<StrategyTag>>> GetTags()
    {
        const int userId = 1; // TODO: Replace with authenticated user ID in Phase 1

        var tags = await _context.StrategyTags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return Ok(tags);
    }

    /// <summary>
    /// Gets a specific tag by ID.
    /// GET /api/tag/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<StrategyTag>> GetTag(int id)
    {
        const int userId = 1; // TODO: Replace with authenticated user ID in Phase 1

        var tag = await _context.StrategyTags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (tag == null)
        {
            return NotFound(new { message = $"Tag {id} not found" });
        }

        return Ok(tag);
    }

    /// <summary>
    /// Creates a new tag.
    /// POST /api/tag
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<StrategyTag>> CreateTag([FromBody] CreateTagRequest request)
    {
        const int userId = 1; // TODO: Replace with authenticated user ID in Phase 1

        // Check for duplicate tag name
        var existingTag = await _context.StrategyTags
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Name == request.Name);

        if (existingTag != null)
        {
            return Conflict(new { message = $"Tag '{request.Name}' already exists" });
        }

        var tag = new StrategyTag
        {
            UserId = userId,
            Name = request.Name,
            Color = request.Color,
            CreatedAt = DateTime.UtcNow
        };

        _context.StrategyTags.Add(tag);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tag created: {TagName} (ID: {TagId})", tag.Name, tag.Id);

        return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag);
    }

    /// <summary>
    /// Updates an existing tag.
    /// PUT /api/tag/{id}
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<StrategyTag>> UpdateTag(int id, [FromBody] UpdateTagRequest request)
    {
        const int userId = 1; // TODO: Replace with authenticated user ID in Phase 1

        var tag = await _context.StrategyTags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (tag == null)
        {
            return NotFound(new { message = $"Tag {id} not found" });
        }

        // Check for duplicate tag name if name is being changed
        if (request.Name != null && request.Name != tag.Name)
        {
            var existingTag = await _context.StrategyTags
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Name == request.Name);

            if (existingTag != null)
            {
                return Conflict(new { message = $"Tag '{request.Name}' already exists" });
            }

            tag.Name = request.Name;
        }

        if (request.Color != null)
        {
            tag.Color = request.Color;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tag updated: {TagName} (ID: {TagId})", tag.Name, tag.Id);

        return Ok(tag);
    }

    /// <summary>
    /// Deletes a tag.
    /// DELETE /api/tag/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag(int id)
    {
        const int userId = 1; // TODO: Replace with authenticated user ID in Phase 1

        var tag = await _context.StrategyTags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (tag == null)
        {
            return NotFound(new { message = $"Tag {id} not found" });
        }

        _context.StrategyTags.Remove(tag);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tag deleted: {TagName} (ID: {TagId})", tag.Name, tag.Id);

        return NoContent();
    }
}

/// <summary>
/// Request to create a new tag.
/// </summary>
public class CreateTagRequest
{
    public required string Name { get; set; }
    public required string Color { get; set; }
}

/// <summary>
/// Request to update an existing tag.
/// </summary>
public class UpdateTagRequest
{
    public string? Name { get; set; }
    public string? Color { get; set; }
}
