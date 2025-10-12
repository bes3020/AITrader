using System.ComponentModel.DataAnnotations;

namespace TradingStrategyAPI.Models;

/// <summary>
/// Supported futures symbols with contract specifications.
/// </summary>
public enum FuturesSymbol
{
    /// <summary>
    /// E-mini S&P 500 futures (ES)
    /// Point value: $50 per point
    /// Tick size: 0.25 = $12.50
    /// </summary>
    [Display(Name = "E-mini S&P 500")]
    ES,

    /// <summary>
    /// E-mini Nasdaq 100 futures (NQ)
    /// Point value: $20 per point
    /// Tick size: 0.25 = $5.00
    /// </summary>
    [Display(Name = "E-mini Nasdaq 100")]
    NQ,

    /// <summary>
    /// E-mini Dow futures (YM)
    /// Point value: $5 per point
    /// Tick size: 1.00 = $5.00
    /// </summary>
    [Display(Name = "E-mini Dow")]
    YM,

    /// <summary>
    /// Bitcoin futures (BTC)
    /// Point value: $5 per point
    /// Tick size: 5.00 = $25.00
    /// </summary>
    [Display(Name = "Bitcoin Futures")]
    BTC,

    /// <summary>
    /// Crude Oil futures (CL)
    /// Point value: $1000 per point
    /// Tick size: 0.01 = $10.00
    /// </summary>
    [Display(Name = "Crude Oil")]
    CL
}

/// <summary>
/// Contract specifications for futures symbols.
/// </summary>
public static class FuturesContractSpecs
{
    /// <summary>
    /// Gets the point value (dollar multiplier per full point move) for a symbol.
    /// </summary>
    public static decimal GetPointValue(string symbol)
    {
        return symbol.ToUpperInvariant() switch
        {
            "ES" => 50m,
            "NQ" => 20m,
            "YM" => 5m,
            "BTC" => 5m,
            "CL" => 1000m,
            _ => throw new ArgumentException($"Unknown symbol: {symbol}", nameof(symbol))
        };
    }

    /// <summary>
    /// Gets the tick size (minimum price increment) for a symbol.
    /// </summary>
    public static decimal GetTickSize(string symbol)
    {
        return symbol.ToUpperInvariant() switch
        {
            "ES" => 0.25m,
            "NQ" => 0.25m,
            "YM" => 1.00m,
            "BTC" => 5.00m,
            "CL" => 0.01m,
            _ => throw new ArgumentException($"Unknown symbol: {symbol}", nameof(symbol))
        };
    }

    /// <summary>
    /// Gets the dollar value of a single tick for a symbol.
    /// </summary>
    public static decimal GetTickValue(string symbol)
    {
        return GetTickSize(symbol) * GetPointValue(symbol);
    }

    /// <summary>
    /// Gets the display name for a symbol.
    /// </summary>
    public static string GetDisplayName(string symbol)
    {
        return symbol.ToUpperInvariant() switch
        {
            "ES" => "E-mini S&P 500",
            "NQ" => "E-mini Nasdaq 100",
            "YM" => "E-mini Dow",
            "BTC" => "Bitcoin Futures",
            "CL" => "Crude Oil",
            _ => symbol
        };
    }

    /// <summary>
    /// Validates if a symbol is supported.
    /// </summary>
    public static bool IsValidSymbol(string symbol)
    {
        return symbol.ToUpperInvariant() is "ES" or "NQ" or "YM" or "BTC" or "CL";
    }

    /// <summary>
    /// Gets all supported symbol codes.
    /// </summary>
    public static string[] GetSupportedSymbols()
    {
        return new[] { "ES", "NQ", "YM", "BTC", "CL" };
    }

    /// <summary>
    /// Calculates the slippage cost (2 ticks) for a symbol.
    /// </summary>
    public static decimal GetSlippageCost(string symbol)
    {
        return GetTickValue(symbol) * 2; // 2 ticks per trade
    }
}
