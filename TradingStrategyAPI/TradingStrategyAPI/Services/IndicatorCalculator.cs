using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Static utility class for calculating technical indicators.
/// Provides optimized calculations for all built-in indicators.
/// </summary>
public static class IndicatorCalculator
{
    /// <summary>
    /// Calculates Exponential Moving Average (EMA).
    /// </summary>
    public static decimal[] CalculateEMA(decimal[] source, int period)
    {
        if (source.Length < period)
            throw new ArgumentException($"Not enough data points. Need at least {period}, got {source.Length}");

        var ema = new decimal[source.Length];
        var multiplier = 2.0m / (period + 1);

        // Start with SMA for first value
        ema[period - 1] = source.Take(period).Average();

        // Calculate EMA for remaining values
        for (int i = period; i < source.Length; i++)
        {
            ema[i] = (source[i] - ema[i - 1]) * multiplier + ema[i - 1];
        }

        return ema;
    }

    /// <summary>
    /// Calculates Simple Moving Average (SMA).
    /// </summary>
    public static decimal[] CalculateSMA(decimal[] source, int period)
    {
        if (source.Length < period)
            throw new ArgumentException($"Not enough data points. Need at least {period}, got {source.Length}");

        var sma = new decimal[source.Length];

        for (int i = period - 1; i < source.Length; i++)
        {
            sma[i] = source.Skip(i - period + 1).Take(period).Average();
        }

        return sma;
    }

    /// <summary>
    /// Calculates Relative Strength Index (RSI).
    /// </summary>
    public static decimal[] CalculateRSI(decimal[] closes, int period = 14)
    {
        if (closes.Length < period + 1)
            throw new ArgumentException($"Not enough data points. Need at least {period + 1}, got {closes.Length}");

        var rsi = new decimal[closes.Length];
        var gains = new decimal[closes.Length];
        var losses = new decimal[closes.Length];

        // Calculate price changes
        for (int i = 1; i < closes.Length; i++)
        {
            var change = closes[i] - closes[i - 1];
            gains[i] = Math.Max(change, 0);
            losses[i] = Math.Max(-change, 0);
        }

        // Calculate first average gain/loss
        var avgGain = gains.Skip(1).Take(period).Average();
        var avgLoss = losses.Skip(1).Take(period).Average();

        // Calculate RSI
        for (int i = period; i < closes.Length; i++)
        {
            if (i > period)
            {
                avgGain = (avgGain * (period - 1) + gains[i]) / period;
                avgLoss = (avgLoss * (period - 1) + losses[i]) / period;
            }

            if (avgLoss == 0)
            {
                rsi[i] = 100;
            }
            else
            {
                var rs = avgGain / avgLoss;
                rsi[i] = 100 - (100 / (1 + rs));
            }
        }

        return rsi;
    }

    /// <summary>
    /// Calculates Bollinger Bands.
    /// Returns tuple of (upper, middle, lower).
    /// </summary>
    public static (decimal[] upper, decimal[] middle, decimal[] lower) CalculateBollingerBands(
        decimal[] closes, int period = 20, decimal stdDevMultiplier = 2.0m)
    {
        if (closes.Length < period)
            throw new ArgumentException($"Not enough data points. Need at least {period}, got {closes.Length}");

        var middle = CalculateSMA(closes, period);
        var upper = new decimal[closes.Length];
        var lower = new decimal[closes.Length];

        for (int i = period - 1; i < closes.Length; i++)
        {
            var slice = closes.Skip(i - period + 1).Take(period).ToArray();
            var stdDev = CalculateStandardDeviation(slice);

            upper[i] = middle[i] + (stdDev * stdDevMultiplier);
            lower[i] = middle[i] - (stdDev * stdDevMultiplier);
        }

        return (upper, middle, lower);
    }

    /// <summary>
    /// Calculates MACD (Moving Average Convergence Divergence).
    /// Returns tuple of (macd, signal, histogram).
    /// </summary>
    public static (decimal[] macd, decimal[] signal, decimal[] histogram) CalculateMACD(
        decimal[] closes, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        var fastEMA = CalculateEMA(closes, fastPeriod);
        var slowEMA = CalculateEMA(closes, slowPeriod);

        var macd = new decimal[closes.Length];
        for (int i = 0; i < closes.Length; i++)
        {
            macd[i] = fastEMA[i] - slowEMA[i];
        }

        var signal = CalculateEMA(macd.Where(v => v != 0).ToArray(), signalPeriod);
        var fullSignal = new decimal[closes.Length];
        Array.Copy(signal, 0, fullSignal, closes.Length - signal.Length, signal.Length);

        var histogram = new decimal[closes.Length];
        for (int i = 0; i < closes.Length; i++)
        {
            histogram[i] = macd[i] - fullSignal[i];
        }

        return (macd, fullSignal, histogram);
    }

    /// <summary>
    /// Calculates Average True Range (ATR).
    /// </summary>
    public static decimal[] CalculateATR(Bar[] bars, int period = 14)
    {
        if (bars.Length < period)
            throw new ArgumentException($"Not enough data points. Need at least {period}, got {bars.Length}");

        var tr = new decimal[bars.Length];
        var atr = new decimal[bars.Length];

        // Calculate True Range
        for (int i = 1; i < bars.Length; i++)
        {
            var high = bars[i].High;
            var low = bars[i].Low;
            var prevClose = bars[i - 1].Close;

            tr[i] = Math.Max(
                high - low,
                Math.Max(
                    Math.Abs(high - prevClose),
                    Math.Abs(low - prevClose)
                )
            );
        }

        // Calculate ATR (use SMA for first value, then exponential smoothing)
        atr[period - 1] = tr.Skip(1).Take(period).Average();

        for (int i = period; i < bars.Length; i++)
        {
            atr[i] = (atr[i - 1] * (period - 1) + tr[i]) / period;
        }

        return atr;
    }

    /// <summary>
    /// Calculates Stochastic Oscillator.
    /// Returns tuple of (%K, %D).
    /// </summary>
    public static (decimal[] k, decimal[] d) CalculateStochastic(
        Bar[] bars, int kPeriod = 14, int dPeriod = 3)
    {
        if (bars.Length < kPeriod)
            throw new ArgumentException($"Not enough data points. Need at least {kPeriod}, got {bars.Length}");

        var k = new decimal[bars.Length];

        // Calculate %K
        for (int i = kPeriod - 1; i < bars.Length; i++)
        {
            var period = bars.Skip(i - kPeriod + 1).Take(kPeriod).ToArray();
            var high = period.Max(b => b.High);
            var low = period.Min(b => b.Low);
            var close = bars[i].Close;

            if (high - low == 0)
            {
                k[i] = 50; // Avoid division by zero
            }
            else
            {
                k[i] = ((close - low) / (high - low)) * 100;
            }
        }

        // Calculate %D (SMA of %K)
        var validK = k.Where(v => v != 0).ToArray();
        var d = CalculateSMA(validK, dPeriod);
        var fullD = new decimal[bars.Length];
        Array.Copy(d, 0, fullD, bars.Length - d.Length, d.Length);

        return (k, fullD);
    }

    /// <summary>
    /// Helper method to calculate standard deviation.
    /// </summary>
    private static decimal CalculateStandardDeviation(decimal[] values)
    {
        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        return (decimal)Math.Sqrt((double)(sumOfSquares / values.Length));
    }

    /// <summary>
    /// Gets the source values from bars based on source type.
    /// </summary>
    public static decimal[] GetSource(Bar[] bars, string source)
    {
        return source.ToLower() switch
        {
            "close" => bars.Select(b => b.Close).ToArray(),
            "open" => bars.Select(b => b.Open).ToArray(),
            "high" => bars.Select(b => b.High).ToArray(),
            "low" => bars.Select(b => b.Low).ToArray(),
            "hl2" => bars.Select(b => (b.High + b.Low) / 2).ToArray(),
            "hlc3" => bars.Select(b => (b.High + b.Low + b.Close) / 3).ToArray(),
            "ohlc4" => bars.Select(b => (b.Open + b.High + b.Low + b.Close) / 4).ToArray(),
            _ => bars.Select(b => b.Close).ToArray()
        };
    }
}
