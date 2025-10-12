using System.ComponentModel.DataAnnotations;

namespace TradingStrategyAPI.DTOs;

/// <summary>
/// Request to analyze a trading strategy from natural language description.
/// Supports multiple futures symbols: ES, NQ, YM, BTC, CL.
/// </summary>
public class AnalyzeStrategyRequest
{
    /// <summary>
    /// Natural language description of the trading strategy.
    /// Example: "Buy when price crosses above VWAP with stop at 10 points and target at 20 points"
    /// </summary>
    [Required(ErrorMessage = "Strategy description is required")]
    [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
    [MaxLength(2000, ErrorMessage = "Description must not exceed 2000 characters")]
    public required string Description { get; set; }

    /// <summary>
    /// The futures symbol to backtest.
    /// Supported symbols: ES (E-mini S&P 500), NQ (E-mini Nasdaq 100), YM (E-mini Dow), BTC (Bitcoin Futures), CL (Crude Oil).
    /// Default: ES
    /// </summary>
    [Required(ErrorMessage = "Symbol is required")]
    [RegularExpression("^(ES|NQ|YM|BTC|CL)$", ErrorMessage = "Symbol must be one of: ES, NQ, YM, BTC, CL")]
    public string Symbol { get; set; } = "ES";

    /// <summary>
    /// Start date for backtesting (inclusive).
    /// </summary>
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date for backtesting (inclusive).
    /// </summary>
    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }
}
