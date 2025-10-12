using Microsoft.Extensions.Logging;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.DataLoader.Services;

/// <summary>
/// Calculates technical indicators for bar data.
/// </summary>
public class IndicatorCalculator
{
    private readonly ILogger<IndicatorCalculator> _logger;

    public IndicatorCalculator(ILogger<IndicatorCalculator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates all indicators for a list of bars.
    /// Must be called with bars in chronological order.
    /// </summary>
    public void CalculateAllIndicators(List<Bar> bars)
    {
        if (!bars.Any())
        {
            _logger.LogWarning("No bars to calculate indicators for");
            return;
        }

        _logger.LogInformation("Calculating indicators for {Count} bars", bars.Count);

        // Calculate VWAP (daily reset)
        CalculateVWAP(bars);

        // Calculate EMAs
        CalculateEMA(bars, 9);
        CalculateEMA(bars, 20);
        CalculateEMA(bars, 50);

        // Calculate average volume
        CalculateAvgVolume(bars);

        _logger.LogInformation("Indicator calculation complete");
    }

    /// <summary>
    /// Calculates VWAP for each trading day.
    /// VWAP = Σ(Typical Price × Volume) / Σ(Volume)
    /// Typical Price = (High + Low + Close) / 3
    /// </summary>
    private void CalculateVWAP(List<Bar> bars)
    {
        DateTime? currentDay = null;
        decimal cumulativePV = 0;
        decimal cumulativeVolume = 0;

        for (int i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            var barDay = bar.Timestamp.Date;

            // Reset on new day
            if (currentDay == null || barDay != currentDay)
            {
                currentDay = barDay;
                cumulativePV = 0;
                cumulativeVolume = 0;
            }

            // Typical price = (H + L + C) / 3
            var typicalPrice = (bar.High + bar.Low + bar.Close) / 3;

            // Accumulate
            cumulativePV += typicalPrice * bar.Volume;
            cumulativeVolume += bar.Volume;

            // Calculate VWAP
            bar.Vwap = cumulativeVolume > 0 ? cumulativePV / cumulativeVolume : bar.Close;

            if (i % 50000 == 0 && i > 0)
            {
                _logger.LogDebug("VWAP calculated for {Count} bars", i);
            }
        }
    }

    /// <summary>
    /// Calculates EMA for a specific period.
    /// EMA = (Close - PreviousEMA) × Multiplier + PreviousEMA
    /// Multiplier = 2 / (Period + 1)
    /// </summary>
    private void CalculateEMA(List<Bar> bars, int period)
    {
        if (bars.Count < period)
        {
            _logger.LogWarning("Not enough bars ({Count}) to calculate EMA{Period}", bars.Count, period);
            return;
        }

        var multiplier = 2.0m / (period + 1);

        // Start with SMA for the first value
        decimal sum = 0;
        for (int i = 0; i < period; i++)
        {
            sum += bars[i].Close;
        }
        var ema = sum / period;

        // Set the first EMA value
        SetEMA(bars[period - 1], period, ema);

        // Calculate EMA for remaining bars
        for (int i = period; i < bars.Count; i++)
        {
            ema = (bars[i].Close - ema) * multiplier + ema;
            SetEMA(bars[i], period, ema);

            if (i % 50000 == 0)
            {
                _logger.LogDebug("EMA{Period} calculated for {Count} bars", period, i);
            }
        }
    }

    /// <summary>
    /// Sets the EMA value on the bar based on the period.
    /// </summary>
    private void SetEMA(Bar bar, int period, decimal value)
    {
        switch (period)
        {
            case 9:
                bar.Ema9 = value;
                break;
            case 20:
                bar.Ema20 = value;
                break;
            case 50:
                bar.Ema50 = value;
                break;
        }
    }

    /// <summary>
    /// Calculates 20-period average volume.
    /// </summary>
    private void CalculateAvgVolume(List<Bar> bars)
    {
        const int period = 20;

        if (bars.Count < period)
        {
            _logger.LogWarning("Not enough bars ({Count}) to calculate AvgVolume20", bars.Count);
            return;
        }

        // Start with simple average for first period
        long sum = 0;
        for (int i = 0; i < period; i++)
        {
            sum += bars[i].Volume;
        }
        bars[period - 1].AvgVolume20 = sum / period;

        // Calculate rolling average for remaining bars
        for (int i = period; i < bars.Count; i++)
        {
            sum = 0;
            for (int j = i - period + 1; j <= i; j++)
            {
                sum += bars[j].Volume;
            }
            bars[i].AvgVolume20 = sum / period;

            if (i % 50000 == 0)
            {
                _logger.LogDebug("AvgVolume20 calculated for {Count} bars", i);
            }
        }
    }
}
