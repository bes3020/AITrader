using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TradingStrategyAPI.Database;
using TradingStrategyAPI.DTOs;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service for comprehensive strategy management including CRUD, versioning, and organization.
/// NOTE: All methods use userId = 1 until Phase 1 (Authentication) is completed.
/// </summary>
public class StrategyManager : IStrategyManager
{
    private readonly TradingDbContext _context;
    private readonly ILogger<StrategyManager> _logger;

    public StrategyManager(TradingDbContext context, ILogger<StrategyManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StrategyDetailResponse> CreateStrategyAsync(CreateStrategyRequest request, int userId = 1)
    {
        _logger.LogInformation("Creating new strategy: {Name} for user {UserId}", request.Name, userId);

        var strategy = new Strategy
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Direction = request.Direction,
            Symbol = request.Symbol,
            Timeframe = request.Timeframe,
            MaxPositions = request.MaxPositions,
            PositionSize = request.PositionSize,
            Tags = request.Tags,
            Notes = request.Notes,
            IsFavorite = request.IsFavorite,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add entry conditions
        if (request.EntryConditions != null)
        {
            foreach (var condDto in request.EntryConditions)
            {
                strategy.EntryConditions.Add(new Condition
                {
                    Indicator = condDto.Indicator,
                    Operator = condDto.Operator,
                    Value = condDto.Value
                });
            }
        }

        // Add stop loss
        if (request.StopLoss != null)
        {
            strategy.StopLoss = new StopLoss
            {
                Type = request.StopLoss.Type,
                Value = request.StopLoss.Value
            };
        }

        // Add take profit
        if (request.TakeProfit != null)
        {
            strategy.TakeProfit = new TakeProfit
            {
                Type = request.TakeProfit.Type,
                Value = request.TakeProfit.Value
            };
        }

        _context.Strategies.Add(strategy);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Strategy created successfully with ID: {StrategyId}", strategy.Id);

        return await GetStrategyAsync(strategy.Id, userId);
    }

    public async Task<StrategyDetailResponse> UpdateStrategyAsync(int strategyId, UpdateStrategyRequest request, int userId = 1)
    {
        _logger.LogInformation("Updating strategy {StrategyId} for user {UserId}", strategyId, userId);

        var strategy = await _context.Strategies
            .Include(s => s.EntryConditions)
            .Include(s => s.StopLoss)
            .Include(s => s.TakeProfit)
            .FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId);

        if (strategy == null)
        {
            throw new KeyNotFoundException($"Strategy {strategyId} not found for user {userId}");
        }

        // Update basic properties
        if (request.Name != null) strategy.Name = request.Name;
        if (request.Description != null) strategy.Description = request.Description;
        if (request.Direction != null) strategy.Direction = request.Direction;
        if (request.Symbol != null) strategy.Symbol = request.Symbol;
        if (request.Timeframe != null) strategy.Timeframe = request.Timeframe;
        if (request.MaxPositions.HasValue) strategy.MaxPositions = request.MaxPositions.Value;
        if (request.PositionSize.HasValue) strategy.PositionSize = request.PositionSize.Value;
        if (request.IsActive.HasValue) strategy.IsActive = request.IsActive.Value;
        if (request.Tags != null) strategy.Tags = request.Tags;
        if (request.Notes != null) strategy.Notes = request.Notes;
        if (request.IsFavorite.HasValue) strategy.IsFavorite = request.IsFavorite.Value;
        if (request.IsArchived.HasValue) strategy.IsArchived = request.IsArchived.Value;

        strategy.UpdatedAt = DateTime.UtcNow;

        // Update entry conditions if provided
        if (request.EntryConditions != null)
        {
            // Remove old conditions
            _context.Conditions.RemoveRange(strategy.EntryConditions);
            strategy.EntryConditions.Clear();

            // Add new conditions
            foreach (var condDto in request.EntryConditions)
            {
                strategy.EntryConditions.Add(new Condition
                {
                    Indicator = condDto.Indicator,
                    Operator = condDto.Operator,
                    Value = condDto.Value
                });
            }
        }

        // Update stop loss if provided
        if (request.StopLoss != null)
        {
            if (strategy.StopLoss != null)
            {
                strategy.StopLoss.Type = request.StopLoss.Type;
                strategy.StopLoss.Value = request.StopLoss.Value;
            }
            else
            {
                strategy.StopLoss = new StopLoss
                {
                    Type = request.StopLoss.Type,
                    Value = request.StopLoss.Value
                };
            }
        }

        // Update take profit if provided
        if (request.TakeProfit != null)
        {
            if (strategy.TakeProfit != null)
            {
                strategy.TakeProfit.Type = request.TakeProfit.Type;
                strategy.TakeProfit.Value = request.TakeProfit.Value;
            }
            else
            {
                strategy.TakeProfit = new TakeProfit
                {
                    Type = request.TakeProfit.Type,
                    Value = request.TakeProfit.Value
                };
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Strategy {StrategyId} updated successfully", strategyId);

        return await GetStrategyAsync(strategyId, userId);
    }

    public async Task DeleteStrategyAsync(int strategyId, int userId = 1)
    {
        _logger.LogInformation("Deleting (soft) strategy {StrategyId} for user {UserId}", strategyId, userId);

        var strategy = await _context.Strategies
            .FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId);

        if (strategy == null)
        {
            throw new KeyNotFoundException($"Strategy {strategyId} not found for user {userId}");
        }

        strategy.IsArchived = true;
        strategy.IsActive = false;
        strategy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Strategy {StrategyId} soft deleted successfully", strategyId);
    }

    public async Task<StrategyDetailResponse> DuplicateStrategyAsync(int strategyId, string newName, int userId = 1)
    {
        _logger.LogInformation("Duplicating strategy {StrategyId} as '{NewName}' for user {UserId}",
            strategyId, newName, userId);

        var source = await _context.Strategies
            .Include(s => s.EntryConditions)
            .Include(s => s.StopLoss)
            .Include(s => s.TakeProfit)
            .FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId);

        if (source == null)
        {
            throw new KeyNotFoundException($"Strategy {strategyId} not found for user {userId}");
        }

        var duplicate = new Strategy
        {
            UserId = userId,
            Name = newName,
            Description = source.Description,
            Direction = source.Direction,
            Symbol = source.Symbol,
            Timeframe = source.Timeframe,
            MaxPositions = source.MaxPositions,
            PositionSize = source.PositionSize,
            Tags = source.Tags,
            Notes = source.Notes,
            IsFavorite = false, // Reset favorite status for duplicate
            VersionNumber = 1, // Reset version number
            ParentStrategyId = null, // Not a version, just a duplicate
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Copy entry conditions
        foreach (var condition in source.EntryConditions)
        {
            duplicate.EntryConditions.Add(new Condition
            {
                Indicator = condition.Indicator,
                Operator = condition.Operator,
                Value = condition.Value
            });
        }

        // Copy stop loss
        if (source.StopLoss != null)
        {
            duplicate.StopLoss = new StopLoss
            {
                Type = source.StopLoss.Type,
                Value = source.StopLoss.Value
            };
        }

        // Copy take profit
        if (source.TakeProfit != null)
        {
            duplicate.TakeProfit = new TakeProfit
            {
                Type = source.TakeProfit.Type,
                Value = source.TakeProfit.Value
            };
        }

        _context.Strategies.Add(duplicate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Strategy duplicated successfully with new ID: {DuplicateId}", duplicate.Id);

        return await GetStrategyAsync(duplicate.Id, userId);
    }

    public async Task<StrategyDetailResponse> CreateVersionAsync(int parentStrategyId, CreateVersionRequest request, int userId = 1)
    {
        _logger.LogInformation("Creating new version of strategy {ParentStrategyId} for user {UserId}",
            parentStrategyId, userId);

        var parent = await _context.Strategies
            .Include(s => s.EntryConditions)
            .Include(s => s.StopLoss)
            .Include(s => s.TakeProfit)
            .Include(s => s.Versions)
            .FirstOrDefaultAsync(s => s.Id == parentStrategyId && s.UserId == userId);

        if (parent == null)
        {
            throw new KeyNotFoundException($"Parent strategy {parentStrategyId} not found for user {userId}");
        }

        // Determine the root parent (if this is already a version)
        var rootParentId = parent.ParentStrategyId ?? parent.Id;

        // Find the highest version number in the chain
        var maxVersionNumber = await _context.Strategies
            .Where(s => (s.ParentStrategyId == rootParentId || s.Id == rootParentId) && s.UserId == userId)
            .MaxAsync(s => (int?)s.VersionNumber) ?? 1;

        var newVersion = new Strategy
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description ?? parent.Description,
            Direction = parent.Direction,
            Symbol = parent.Symbol,
            Timeframe = parent.Timeframe,
            MaxPositions = parent.MaxPositions,
            PositionSize = parent.PositionSize,
            Tags = parent.Tags,
            Notes = request.Notes ?? parent.Notes,
            IsFavorite = false,
            ParentStrategyId = rootParentId,
            VersionNumber = maxVersionNumber + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Copy or override entry conditions
        if (request.EntryConditions != null && request.EntryConditions.Any())
        {
            foreach (var condDto in request.EntryConditions)
            {
                newVersion.EntryConditions.Add(new Condition
                {
                    Indicator = condDto.Indicator,
                    Operator = condDto.Operator,
                    Value = condDto.Value
                });
            }
        }
        else
        {
            // Copy from parent
            foreach (var condition in parent.EntryConditions)
            {
                newVersion.EntryConditions.Add(new Condition
                {
                    Indicator = condition.Indicator,
                    Operator = condition.Operator,
                    Value = condition.Value
                });
            }
        }

        // Copy or override stop loss
        if (request.StopLoss != null)
        {
            newVersion.StopLoss = new StopLoss
            {
                Type = request.StopLoss.Type,
                Value = request.StopLoss.Value
            };
        }
        else if (parent.StopLoss != null)
        {
            newVersion.StopLoss = new StopLoss
            {
                Type = parent.StopLoss.Type,
                Value = parent.StopLoss.Value
            };
        }

        // Copy or override take profit
        if (request.TakeProfit != null)
        {
            newVersion.TakeProfit = new TakeProfit
            {
                Type = request.TakeProfit.Type,
                Value = request.TakeProfit.Value
            };
        }
        else if (parent.TakeProfit != null)
        {
            newVersion.TakeProfit = new TakeProfit
            {
                Type = parent.TakeProfit.Type,
                Value = parent.TakeProfit.Value
            };
        }

        _context.Strategies.Add(newVersion);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Strategy version created successfully with ID: {VersionId}, version number: {VersionNumber}",
            newVersion.Id, newVersion.VersionNumber);

        return await GetStrategyAsync(newVersion.Id, userId);
    }

    public async Task<List<StrategyVersionSummary>> GetVersionsAsync(int strategyId, int userId = 1)
    {
        _logger.LogInformation("Getting versions for strategy {StrategyId}", strategyId);

        var strategy = await _context.Strategies
            .FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId);

        if (strategy == null)
        {
            throw new KeyNotFoundException($"Strategy {strategyId} not found for user {userId}");
        }

        // Find the root parent
        var rootParentId = strategy.ParentStrategyId ?? strategy.Id;

        // Get all versions in the chain (including the root)
        var versions = await _context.Strategies
            .Where(s => (s.Id == rootParentId || s.ParentStrategyId == rootParentId) && s.UserId == userId)
            .Include(s => s.Results)
            .OrderBy(s => s.VersionNumber)
            .Select(s => new StrategyVersionSummary
            {
                Id = s.Id,
                Name = s.Name,
                VersionNumber = s.VersionNumber,
                CreatedAt = s.CreatedAt,
                LatestResult = s.Results
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new LatestResultSummary
                    {
                        ResultId = r.Id,
                        TotalTrades = r.TotalTrades,
                        WinRate = r.WinRate,
                        TotalPnl = r.TotalPnl,
                        MaxDrawdown = r.MaxDrawdown,
                        CreatedAt = r.CreatedAt
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return versions;
    }

    public async Task<bool> ToggleFavoriteAsync(int strategyId, int userId = 1)
    {
        _logger.LogInformation("Toggling favorite for strategy {StrategyId}", strategyId);

        var strategy = await _context.Strategies
            .FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId);

        if (strategy == null)
        {
            throw new KeyNotFoundException($"Strategy {strategyId} not found for user {userId}");
        }

        strategy.IsFavorite = !strategy.IsFavorite;
        strategy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Strategy {StrategyId} favorite status set to: {IsFavorite}",
            strategyId, strategy.IsFavorite);

        return strategy.IsFavorite;
    }

    public async Task ArchiveStrategyAsync(int strategyId, bool archive, int userId = 1)
    {
        _logger.LogInformation("Setting archive status to {Archive} for strategy {StrategyId}",
            archive, strategyId);

        var strategy = await _context.Strategies
            .FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId);

        if (strategy == null)
        {
            throw new KeyNotFoundException($"Strategy {strategyId} not found for user {userId}");
        }

        strategy.IsArchived = archive;
        strategy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Strategy {StrategyId} archive status updated", strategyId);
    }

    public async Task<StrategyExportFormat> ExportStrategyAsync(int strategyId, int userId = 1)
    {
        _logger.LogInformation("Exporting strategy {StrategyId}", strategyId);

        var strategy = await _context.Strategies
            .Include(s => s.EntryConditions)
            .Include(s => s.StopLoss)
            .Include(s => s.TakeProfit)
            .FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId);

        if (strategy == null)
        {
            throw new KeyNotFoundException($"Strategy {strategyId} not found for user {userId}");
        }

        return new StrategyExportFormat
        {
            Version = "1.0",
            Strategy = new StrategyExportData
            {
                Name = strategy.Name,
                Description = strategy.Description,
                Direction = strategy.Direction,
                Symbol = strategy.Symbol,
                Timeframe = strategy.Timeframe,
                MaxPositions = strategy.MaxPositions,
                PositionSize = strategy.PositionSize,
                Tags = strategy.Tags,
                Notes = strategy.Notes,
                EntryConditions = strategy.EntryConditions.Select(c => new ConditionDto
                {
                    Indicator = c.Indicator,
                    Operator = c.Operator,
                    Value = c.Value
                }).ToList(),
                StopLoss = strategy.StopLoss != null ? new StopLossDto
                {
                    Type = strategy.StopLoss.Type,
                    Value = strategy.StopLoss.Value
                } : null,
                TakeProfit = strategy.TakeProfit != null ? new TakeProfitDto
                {
                    Type = strategy.TakeProfit.Type,
                    Value = strategy.TakeProfit.Value
                } : null
            }
        };
    }

    public async Task<StrategyDetailResponse> ImportStrategyAsync(ImportStrategyRequest request, int userId = 1)
    {
        _logger.LogInformation("Importing strategy for user {UserId}", userId);

        StrategyExportFormat exportData;
        try
        {
            exportData = JsonSerializer.Deserialize<StrategyExportFormat>(request.JsonData)
                ?? throw new ArgumentException("Invalid JSON data");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse import JSON");
            throw new ArgumentException("Invalid JSON format", ex);
        }

        var createRequest = new CreateStrategyRequest
        {
            Name = exportData.Strategy.Name,
            Description = exportData.Strategy.Description,
            Direction = exportData.Strategy.Direction,
            Symbol = exportData.Strategy.Symbol,
            Timeframe = exportData.Strategy.Timeframe,
            MaxPositions = exportData.Strategy.MaxPositions,
            PositionSize = exportData.Strategy.PositionSize,
            Tags = exportData.Strategy.Tags,
            Notes = exportData.Strategy.Notes,
            IsFavorite = request.SetAsFavorite,
            EntryConditions = exportData.Strategy.EntryConditions,
            StopLoss = exportData.Strategy.StopLoss,
            TakeProfit = exportData.Strategy.TakeProfit
        };

        return await CreateStrategyAsync(createRequest, userId);
    }

    public async Task<StrategyDetailResponse> GetStrategyAsync(int strategyId, int userId = 1)
    {
        var strategy = await _context.Strategies
            .Include(s => s.EntryConditions)
            .Include(s => s.StopLoss)
            .Include(s => s.TakeProfit)
            .Include(s => s.Versions)
            .Include(s => s.Results)
            .FirstOrDefaultAsync(s => s.Id == strategyId && s.UserId == userId);

        if (strategy == null)
        {
            throw new KeyNotFoundException($"Strategy {strategyId} not found for user {userId}");
        }

        return new StrategyDetailResponse
        {
            Id = strategy.Id,
            Name = strategy.Name,
            Description = strategy.Description,
            Direction = strategy.Direction,
            Symbol = strategy.Symbol,
            Timeframe = strategy.Timeframe,
            IsActive = strategy.IsActive,
            IsFavorite = strategy.IsFavorite,
            IsArchived = strategy.IsArchived,
            VersionNumber = strategy.VersionNumber,
            ParentStrategyId = strategy.ParentStrategyId,
            MaxPositions = strategy.MaxPositions,
            PositionSize = strategy.PositionSize,
            Tags = strategy.Tags,
            Notes = strategy.Notes,
            CreatedAt = strategy.CreatedAt,
            UpdatedAt = strategy.UpdatedAt,
            LastBacktestedAt = strategy.LastBacktestedAt,
            EntryConditions = strategy.EntryConditions.Select(c => new ConditionDto
            {
                Indicator = c.Indicator,
                Operator = c.Operator,
                Value = c.Value
            }).ToList(),
            StopLoss = strategy.StopLoss != null ? new StopLossDto
            {
                Type = strategy.StopLoss.Type,
                Value = strategy.StopLoss.Value
            } : null,
            TakeProfit = strategy.TakeProfit != null ? new TakeProfitDto
            {
                Type = strategy.TakeProfit.Type,
                Value = strategy.TakeProfit.Value
            } : null,
            Versions = strategy.Versions.Select(v => new StrategyVersionSummary
            {
                Id = v.Id,
                Name = v.Name,
                VersionNumber = v.VersionNumber,
                CreatedAt = v.CreatedAt,
                LatestResult = v.Results
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new LatestResultSummary
                    {
                        ResultId = r.Id,
                        TotalTrades = r.TotalTrades,
                        WinRate = r.WinRate,
                        TotalPnl = r.TotalPnl,
                        MaxDrawdown = r.MaxDrawdown,
                        CreatedAt = r.CreatedAt
                    })
                    .FirstOrDefault()
            }).ToList(),
            Results = strategy.Results.Select(r => new ResultSummary
            {
                Id = r.Id,
                TotalTrades = r.TotalTrades,
                WinRate = r.WinRate,
                TotalPnl = r.TotalPnl,
                AvgWin = r.AvgWin,
                AvgLoss = r.AvgLoss,
                MaxDrawdown = r.MaxDrawdown,
                ProfitFactor = r.ProfitFactor ?? 0,
                CreatedAt = r.CreatedAt
            }).ToList()
        };
    }

    public async Task<List<StrategyListItem>> GetStrategiesAsync(
        string[]? tags = null,
        bool? favorite = null,
        bool? archived = null,
        int userId = 1)
    {
        var query = _context.Strategies
            .Where(s => s.UserId == userId)
            .Include(s => s.Results)
            .AsQueryable();

        // Apply filters
        if (tags != null && tags.Any())
        {
            // PostgreSQL array overlap check
            query = query.Where(s => s.Tags != null && s.Tags.Any(t => tags.Contains(t)));
        }

        if (favorite.HasValue)
        {
            query = query.Where(s => s.IsFavorite == favorite.Value);
        }

        if (archived.HasValue)
        {
            query = query.Where(s => s.IsArchived == archived.Value);
        }

        var strategies = await query
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new StrategyListItem
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Direction = s.Direction,
                Symbol = s.Symbol,
                Timeframe = s.Timeframe,
                IsActive = s.IsActive,
                IsFavorite = s.IsFavorite,
                IsArchived = s.IsArchived,
                VersionNumber = s.VersionNumber,
                ParentStrategyId = s.ParentStrategyId,
                Tags = s.Tags,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                LastBacktestedAt = s.LastBacktestedAt,
                LatestResult = s.Results
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new LatestResultSummary
                    {
                        ResultId = r.Id,
                        TotalTrades = r.TotalTrades,
                        WinRate = r.WinRate,
                        TotalPnl = r.TotalPnl,
                        MaxDrawdown = r.MaxDrawdown,
                        CreatedAt = r.CreatedAt
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return strategies;
    }

    public async Task<SearchStrategiesResponse> SearchStrategiesAsync(SearchStrategiesRequest request, int userId = 1)
    {
        var query = _context.Strategies
            .Where(s => s.UserId == userId)
            .Include(s => s.Results)
            .AsQueryable();

        // Apply search query
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchTerm = request.Query.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(searchTerm) ||
                (s.Description != null && s.Description.ToLower().Contains(searchTerm)) ||
                (s.Notes != null && s.Notes.ToLower().Contains(searchTerm)));
        }

        // Apply filters
        if (request.Tags != null && request.Tags.Any())
        {
            query = query.Where(s => s.Tags != null && s.Tags.Any(t => request.Tags.Contains(t)));
        }

        if (!string.IsNullOrWhiteSpace(request.Symbol))
        {
            query = query.Where(s => s.Symbol == request.Symbol);
        }

        if (!string.IsNullOrWhiteSpace(request.Direction))
        {
            query = query.Where(s => s.Direction == request.Direction);
        }

        if (request.IsFavorite.HasValue)
        {
            query = query.Where(s => s.IsFavorite == request.IsFavorite.Value);
        }

        if (request.IsArchived.HasValue)
        {
            query = query.Where(s => s.IsArchived == request.IsArchived.Value);
        }

        // Performance filters (requires results)
        if (request.MinWinRate.HasValue || request.MinPnl.HasValue)
        {
            query = query.Where(s => s.Results.Any());

            if (request.MinWinRate.HasValue)
            {
                query = query.Where(s => s.Results.Any(r => r.WinRate >= request.MinWinRate.Value));
            }

            if (request.MinPnl.HasValue)
            {
                query = query.Where(s => s.Results.Any(r => r.TotalPnl >= request.MinPnl.Value));
            }
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var strategies = await query
            .OrderByDescending(s => s.UpdatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StrategyListItem
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Direction = s.Direction,
                Symbol = s.Symbol,
                Timeframe = s.Timeframe,
                IsActive = s.IsActive,
                IsFavorite = s.IsFavorite,
                IsArchived = s.IsArchived,
                VersionNumber = s.VersionNumber,
                ParentStrategyId = s.ParentStrategyId,
                Tags = s.Tags,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                LastBacktestedAt = s.LastBacktestedAt,
                LatestResult = s.Results
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new LatestResultSummary
                    {
                        ResultId = r.Id,
                        TotalTrades = r.TotalTrades,
                        WinRate = r.WinRate,
                        TotalPnl = r.TotalPnl,
                        MaxDrawdown = r.MaxDrawdown,
                        CreatedAt = r.CreatedAt
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return new SearchStrategiesResponse
        {
            Strategies = strategies,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }

    public async Task<StrategyComparisonResponse> CompareStrategiesAsync(CompareStrategiesRequest request, int userId = 1)
    {
        _logger.LogInformation("Comparing {Count} strategies for user {UserId}", request.StrategyIds.Length, userId);

        var strategies = await _context.Strategies
            .Where(s => request.StrategyIds.Contains(s.Id) && s.UserId == userId)
            .Include(s => s.EntryConditions)
            .Include(s => s.Results)
            .ToListAsync();

        if (strategies.Count != request.StrategyIds.Length)
        {
            throw new ArgumentException("One or more strategy IDs not found or not accessible");
        }

        var comparisonItems = strategies.Select(s =>
        {
            var latestResult = s.Results.OrderByDescending(r => r.CreatedAt).FirstOrDefault();

            return new StrategyComparisonItem
            {
                Id = s.Id,
                Name = s.Name,
                Direction = s.Direction,
                Symbol = s.Symbol,
                EntryConditionsCount = s.EntryConditions.Count,
                LatestResult = latestResult != null ? new LatestResultSummary
                {
                    ResultId = latestResult.Id,
                    TotalTrades = latestResult.TotalTrades,
                    WinRate = latestResult.WinRate,
                    TotalPnl = latestResult.TotalPnl,
                    MaxDrawdown = latestResult.MaxDrawdown,
                    CreatedAt = latestResult.CreatedAt
                } : null
            };
        }).ToList();

        // Calculate ranks
        var strategiesWithResults = comparisonItems.Where(c => c.LatestResult != null).ToList();

        if (strategiesWithResults.Any())
        {
            var orderedByWinRate = strategiesWithResults.OrderByDescending(c => c.LatestResult!.WinRate).ToList();
            var orderedByPnl = strategiesWithResults.OrderByDescending(c => c.LatestResult!.TotalPnl).ToList();
            var orderedByDrawdown = strategiesWithResults.OrderBy(c => c.LatestResult!.MaxDrawdown).ToList();

            foreach (var item in strategiesWithResults)
            {
                item.WinRateRank = orderedByWinRate.IndexOf(item) + 1;
                item.PnlRank = orderedByPnl.IndexOf(item) + 1;
                item.DrawdownRank = orderedByDrawdown.IndexOf(item) + 1;
            }
        }

        // Save comparison if requested
        if (request.SaveComparison && !string.IsNullOrWhiteSpace(request.ComparisonName))
        {
            var comparison = new StrategyComparison
            {
                UserId = userId,
                Name = request.ComparisonName,
                StrategyIds = request.StrategyIds,
                CreatedAt = DateTime.UtcNow
            };

            _context.StrategyComparisons.Add(comparison);
            await _context.SaveChangesAsync();
        }

        return new StrategyComparisonResponse
        {
            ComparisonName = request.ComparisonName,
            ComparedAt = DateTime.UtcNow,
            Strategies = comparisonItems
        };
    }
}
