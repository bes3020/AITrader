using TradingStrategyAPI.DTOs;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service interface for comprehensive strategy management including CRUD, versioning, and organization.
/// NOTE: All methods use userId = 1 until Phase 1 (Authentication) is completed.
/// </summary>
public interface IStrategyManager
{
    /// <summary>
    /// Creates a new strategy.
    /// </summary>
    /// <param name="request">Strategy creation request</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>Created strategy with full details</returns>
    Task<StrategyDetailResponse> CreateStrategyAsync(CreateStrategyRequest request, int userId = 1);

    /// <summary>
    /// Updates an existing strategy.
    /// </summary>
    /// <param name="strategyId">Strategy ID to update</param>
    /// <param name="request">Update request with changes</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>Updated strategy with full details</returns>
    Task<StrategyDetailResponse> UpdateStrategyAsync(int strategyId, UpdateStrategyRequest request, int userId = 1);

    /// <summary>
    /// Soft deletes a strategy (sets IsArchived = true, IsActive = false).
    /// </summary>
    /// <param name="strategyId">Strategy ID to delete</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    Task DeleteStrategyAsync(int strategyId, int userId = 1);

    /// <summary>
    /// Duplicates an existing strategy with a new name.
    /// </summary>
    /// <param name="strategyId">Strategy ID to duplicate</param>
    /// <param name="newName">Name for the duplicated strategy</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>Duplicated strategy with full details</returns>
    Task<StrategyDetailResponse> DuplicateStrategyAsync(int strategyId, string newName, int userId = 1);

    /// <summary>
    /// Creates a new version of an existing strategy.
    /// Copies parent strategy and increments version number.
    /// </summary>
    /// <param name="parentStrategyId">Parent strategy ID</param>
    /// <param name="request">Version creation request</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>New strategy version with full details</returns>
    Task<StrategyDetailResponse> CreateVersionAsync(int parentStrategyId, CreateVersionRequest request, int userId = 1);

    /// <summary>
    /// Gets all versions of a strategy.
    /// </summary>
    /// <param name="strategyId">Strategy ID (can be any version in the chain)</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>List of all versions</returns>
    Task<List<StrategyVersionSummary>> GetVersionsAsync(int strategyId, int userId = 1);

    /// <summary>
    /// Toggles the favorite status of a strategy.
    /// </summary>
    /// <param name="strategyId">Strategy ID</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>Updated favorite status</returns>
    Task<bool> ToggleFavoriteAsync(int strategyId, int userId = 1);

    /// <summary>
    /// Archives or unarchives a strategy.
    /// </summary>
    /// <param name="strategyId">Strategy ID</param>
    /// <param name="archive">True to archive, false to unarchive</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    Task ArchiveStrategyAsync(int strategyId, bool archive, int userId = 1);

    /// <summary>
    /// Exports a strategy to JSON format.
    /// </summary>
    /// <param name="strategyId">Strategy ID to export</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>Strategy in export format</returns>
    Task<StrategyExportFormat> ExportStrategyAsync(int strategyId, int userId = 1);

    /// <summary>
    /// Imports a strategy from JSON format.
    /// </summary>
    /// <param name="request">Import request with JSON data</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>Imported strategy with full details</returns>
    Task<StrategyDetailResponse> ImportStrategyAsync(ImportStrategyRequest request, int userId = 1);

    /// <summary>
    /// Gets a single strategy by ID with full details.
    /// </summary>
    /// <param name="strategyId">Strategy ID</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>Strategy with full details</returns>
    Task<StrategyDetailResponse> GetStrategyAsync(int strategyId, int userId = 1);

    /// <summary>
    /// Gets a list of strategies with optional filters.
    /// </summary>
    /// <param name="tags">Filter by tags</param>
    /// <param name="favorite">Filter by favorite status</param>
    /// <param name="archived">Filter by archived status</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>List of strategies</returns>
    Task<List<StrategyListItem>> GetStrategiesAsync(
        string[]? tags = null,
        bool? favorite = null,
        bool? archived = null,
        int userId = 1);

    /// <summary>
    /// Searches strategies with advanced filters and pagination.
    /// </summary>
    /// <param name="request">Search request with filters</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>Paginated search results</returns>
    Task<SearchStrategiesResponse> SearchStrategiesAsync(SearchStrategiesRequest request, int userId = 1);

    /// <summary>
    /// Compares multiple strategies side-by-side.
    /// </summary>
    /// <param name="request">Comparison request</param>
    /// <param name="userId">User ID (defaults to 1 for anonymous user)</param>
    /// <returns>Comparison results</returns>
    Task<StrategyComparisonResponse> CompareStrategiesAsync(CompareStrategiesRequest request, int userId = 1);
}
