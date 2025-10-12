using System.ComponentModel.DataAnnotations;

namespace TradingStrategyAPI.DTOs;

/// <summary>
/// Request to refine an existing strategy by adding new conditions.
/// </summary>
public class RefineStrategyRequest
{
    /// <summary>
    /// The ID of the original strategy to refine.
    /// </summary>
    [Required(ErrorMessage = "Strategy ID is required")]
    public int StrategyId { get; set; }

    /// <summary>
    /// New condition to add to the strategy.
    /// Example: "and volume > 1.5x_average"
    /// </summary>
    [Required(ErrorMessage = "Additional condition is required")]
    [MinLength(5, ErrorMessage = "Condition must be at least 5 characters")]
    [MaxLength(500, ErrorMessage = "Condition must not exceed 500 characters")]
    public required string AdditionalCondition { get; set; }

    /// <summary>
    /// The futures symbol to backtest the refined strategy.
    /// Supported symbols: ES (E-mini S&P 500), NQ (E-mini Nasdaq 100), YM (E-mini Dow), BTC (Bitcoin Futures), CL (Crude Oil).
    /// Default: ES
    /// </summary>
    [Required(ErrorMessage = "Symbol is required")]
    [RegularExpression("^(ES|NQ|YM|BTC|CL)$", ErrorMessage = "Symbol must be one of: ES, NQ, YM, BTC, CL")]
    public string Symbol { get; set; } = "ES";

    /// <summary>
    /// Start date for backtesting the refined strategy.
    /// </summary>
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date for backtesting the refined strategy.
    /// </summary>
    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }
}
