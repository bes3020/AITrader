namespace TradingStrategyAPI.Models;

/// <summary>
/// Static definitions for built-in indicators.
/// These are system-provided indicators that are always available to all users.
/// </summary>
public static class BuiltInIndicator
{
    /// <summary>
    /// Gets all available built-in indicator definitions.
    /// </summary>
    public static List<IndicatorDefinition> GetAll()
    {
        return new List<IndicatorDefinition>
        {
            new IndicatorDefinition
            {
                Type = "EMA",
                DisplayName = "Exponential Moving Average",
                Description = "Exponential moving average that gives more weight to recent prices",
                Category = "Trend",
                Parameters = new List<IndicatorParameter>
                {
                    new IndicatorParameter
                    {
                        Name = "period",
                        Type = "int",
                        DefaultValue = "20",
                        MinValue = 1,
                        MaxValue = 500,
                        Required = true,
                        Description = "Number of periods to average"
                    },
                    new IndicatorParameter
                    {
                        Name = "source",
                        Type = "string",
                        DefaultValue = "close",
                        Required = true,
                        Description = "Price source: close, open, high, low, or hl2 (high+low)/2"
                    }
                },
                CommonPresets = new List<IndicatorPreset>
                {
                    new IndicatorPreset { Name = "EMA 9", Parameters = "{ \"period\": 9, \"source\": \"close\" }" },
                    new IndicatorPreset { Name = "EMA 20", Parameters = "{ \"period\": 20, \"source\": \"close\" }" },
                    new IndicatorPreset { Name = "EMA 50", Parameters = "{ \"period\": 50, \"source\": \"close\" }" }
                }
            },
            new IndicatorDefinition
            {
                Type = "SMA",
                DisplayName = "Simple Moving Average",
                Description = "Simple moving average of price over a specified period",
                Category = "Trend",
                Parameters = new List<IndicatorParameter>
                {
                    new IndicatorParameter
                    {
                        Name = "period",
                        Type = "int",
                        DefaultValue = "20",
                        MinValue = 1,
                        MaxValue = 500,
                        Required = true,
                        Description = "Number of periods to average"
                    },
                    new IndicatorParameter
                    {
                        Name = "source",
                        Type = "string",
                        DefaultValue = "close",
                        Required = true,
                        Description = "Price source: close, open, high, low, or hl2"
                    }
                },
                CommonPresets = new List<IndicatorPreset>
                {
                    new IndicatorPreset { Name = "SMA 10", Parameters = "{ \"period\": 10, \"source\": \"close\" }" },
                    new IndicatorPreset { Name = "SMA 20", Parameters = "{ \"period\": 20, \"source\": \"close\" }" },
                    new IndicatorPreset { Name = "SMA 50", Parameters = "{ \"period\": 50, \"source\": \"close\" }" }
                }
            },
            new IndicatorDefinition
            {
                Type = "RSI",
                DisplayName = "Relative Strength Index",
                Description = "Momentum oscillator measuring speed and magnitude of price changes (0-100)",
                Category = "Momentum",
                Parameters = new List<IndicatorParameter>
                {
                    new IndicatorParameter
                    {
                        Name = "period",
                        Type = "int",
                        DefaultValue = "14",
                        MinValue = 2,
                        MaxValue = 100,
                        Required = true,
                        Description = "Number of periods for RSI calculation"
                    }
                },
                CommonPresets = new List<IndicatorPreset>
                {
                    new IndicatorPreset { Name = "RSI 14", Parameters = "{ \"period\": 14 }" },
                    new IndicatorPreset { Name = "RSI 9", Parameters = "{ \"period\": 9 }" },
                    new IndicatorPreset { Name = "RSI 21", Parameters = "{ \"period\": 21 }" }
                }
            },
            new IndicatorDefinition
            {
                Type = "BollingerBands",
                DisplayName = "Bollinger Bands",
                Description = "Volatility bands placed above and below a moving average",
                Category = "Volatility",
                Parameters = new List<IndicatorParameter>
                {
                    new IndicatorParameter
                    {
                        Name = "period",
                        Type = "int",
                        DefaultValue = "20",
                        MinValue = 2,
                        MaxValue = 100,
                        Required = true,
                        Description = "Period for moving average calculation"
                    },
                    new IndicatorParameter
                    {
                        Name = "stdDev",
                        Type = "decimal",
                        DefaultValue = "2.0",
                        MinValue = 0.1m,
                        MaxValue = 5.0m,
                        Required = true,
                        Description = "Number of standard deviations for bands"
                    }
                },
                CommonPresets = new List<IndicatorPreset>
                {
                    new IndicatorPreset { Name = "BB (20, 2)", Parameters = "{ \"period\": 20, \"stdDev\": 2.0 }" },
                    new IndicatorPreset { Name = "BB (20, 2.5)", Parameters = "{ \"period\": 20, \"stdDev\": 2.5 }" }
                }
            },
            new IndicatorDefinition
            {
                Type = "MACD",
                DisplayName = "Moving Average Convergence Divergence",
                Description = "Trend-following momentum indicator showing relationship between two EMAs",
                Category = "Momentum",
                Parameters = new List<IndicatorParameter>
                {
                    new IndicatorParameter
                    {
                        Name = "fastPeriod",
                        Type = "int",
                        DefaultValue = "12",
                        MinValue = 2,
                        MaxValue = 100,
                        Required = true,
                        Description = "Fast EMA period"
                    },
                    new IndicatorParameter
                    {
                        Name = "slowPeriod",
                        Type = "int",
                        DefaultValue = "26",
                        MinValue = 2,
                        MaxValue = 100,
                        Required = true,
                        Description = "Slow EMA period"
                    },
                    new IndicatorParameter
                    {
                        Name = "signalPeriod",
                        Type = "int",
                        DefaultValue = "9",
                        MinValue = 2,
                        MaxValue = 50,
                        Required = true,
                        Description = "Signal line EMA period"
                    }
                },
                CommonPresets = new List<IndicatorPreset>
                {
                    new IndicatorPreset { Name = "MACD (12, 26, 9)", Parameters = "{ \"fastPeriod\": 12, \"slowPeriod\": 26, \"signalPeriod\": 9 }" }
                }
            },
            new IndicatorDefinition
            {
                Type = "ATR",
                DisplayName = "Average True Range",
                Description = "Volatility indicator measuring the average range of price movement",
                Category = "Volatility",
                Parameters = new List<IndicatorParameter>
                {
                    new IndicatorParameter
                    {
                        Name = "period",
                        Type = "int",
                        DefaultValue = "14",
                        MinValue = 1,
                        MaxValue = 100,
                        Required = true,
                        Description = "Number of periods for ATR calculation"
                    }
                },
                CommonPresets = new List<IndicatorPreset>
                {
                    new IndicatorPreset { Name = "ATR 14", Parameters = "{ \"period\": 14 }" },
                    new IndicatorPreset { Name = "ATR 10", Parameters = "{ \"period\": 10 }" }
                }
            },
            new IndicatorDefinition
            {
                Type = "ADX",
                DisplayName = "Average Directional Index",
                Description = "Trend strength indicator (0-100), values above 25 indicate strong trend",
                Category = "Trend",
                Parameters = new List<IndicatorParameter>
                {
                    new IndicatorParameter
                    {
                        Name = "period",
                        Type = "int",
                        DefaultValue = "14",
                        MinValue = 2,
                        MaxValue = 100,
                        Required = true,
                        Description = "Number of periods for ADX calculation"
                    }
                },
                CommonPresets = new List<IndicatorPreset>
                {
                    new IndicatorPreset { Name = "ADX 14", Parameters = "{ \"period\": 14 }" }
                }
            },
            new IndicatorDefinition
            {
                Type = "Stochastic",
                DisplayName = "Stochastic Oscillator",
                Description = "Momentum indicator comparing closing price to price range (0-100)",
                Category = "Momentum",
                Parameters = new List<IndicatorParameter>
                {
                    new IndicatorParameter
                    {
                        Name = "kPeriod",
                        Type = "int",
                        DefaultValue = "14",
                        MinValue = 1,
                        MaxValue = 100,
                        Required = true,
                        Description = "Period for %K calculation"
                    },
                    new IndicatorParameter
                    {
                        Name = "dPeriod",
                        Type = "int",
                        DefaultValue = "3",
                        MinValue = 1,
                        MaxValue = 50,
                        Required = true,
                        Description = "Period for %D (signal line) smoothing"
                    }
                },
                CommonPresets = new List<IndicatorPreset>
                {
                    new IndicatorPreset { Name = "Stoch (14, 3)", Parameters = "{ \"kPeriod\": 14, \"dPeriod\": 3 }" }
                }
            }
        };
    }

    /// <summary>
    /// Gets a specific built-in indicator definition by type.
    /// </summary>
    public static IndicatorDefinition? GetByType(string type)
    {
        return GetAll().FirstOrDefault(i => i.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Represents a complete definition of an indicator.
/// </summary>
public class IndicatorDefinition
{
    public required string Type { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }
    public required List<IndicatorParameter> Parameters { get; set; }
    public List<IndicatorPreset>? CommonPresets { get; set; }
}

/// <summary>
/// Represents a preset configuration for an indicator.
/// </summary>
public class IndicatorPreset
{
    public required string Name { get; set; }
    public required string Parameters { get; set; }
}
