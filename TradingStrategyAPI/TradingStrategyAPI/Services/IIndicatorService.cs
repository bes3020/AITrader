using TradingStrategyAPI.DTOs;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Service interface for managing custom indicators.
/// NOTE: Uses userId = 1 (anonymous user) until Phase 1 (Authentication) is completed.
/// </summary>
public interface IIndicatorService
{
    /// <summary>
    /// Gets all custom indicators for a user.
    /// </summary>
    Task<List<CustomIndicator>> GetUserIndicatorsAsync(int userId = 1);

    /// <summary>
    /// Gets all public indicators.
    /// </summary>
    Task<List<CustomIndicator>> GetPublicIndicatorsAsync();

    /// <summary>
    /// Gets all built-in indicator definitions.
    /// </summary>
    List<BuiltInIndicatorResponse> GetBuiltInIndicators();

    /// <summary>
    /// Creates a new custom indicator.
    /// </summary>
    Task<CustomIndicator> CreateIndicatorAsync(CreateIndicatorRequest request, int userId = 1);

    /// <summary>
    /// Updates an existing custom indicator.
    /// </summary>
    Task<CustomIndicator> UpdateIndicatorAsync(int id, UpdateIndicatorRequest request, int userId = 1);

    /// <summary>
    /// Deletes a custom indicator.
    /// </summary>
    Task DeleteIndicatorAsync(int id, int userId = 1);

    /// <summary>
    /// Calculates indicator values for a specific date range.
    /// </summary>
    Task<CalculateIndicatorResponse> CalculateIndicatorAsync(
        int indicatorId,
        string symbol,
        DateTime startDate,
        DateTime endDate,
        int userId = 1);
}
