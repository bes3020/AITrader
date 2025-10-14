import { useState, useEffect } from "react";
import type { IndicatorDefinition } from "@/lib/indicator-definitions";

/**
 * Sample market data for indicator preview calculations
 */
const SAMPLE_DATA = {
  prices: [100, 102, 101, 103, 105, 104, 106, 108, 107, 109, 111, 110, 112, 114, 113],
  volumes: [1000, 1200, 900, 1500, 1800, 1100, 1600, 2000, 1300, 1700, 2100, 1400, 1900, 2200, 1500],
};

/**
 * Simple moving average calculation
 */
function calculateSMA(data: number[], period: number): number {
  if (data.length < period) return data[data.length - 1] || 0;
  const slice = data.slice(-period);
  return slice.reduce((a, b) => a + b, 0) / period;
}

/**
 * Exponential moving average calculation
 */
function calculateEMA(data: number[], period: number): number {
  if (data.length === 0) return 0;
  if (data.length < period) return calculateSMA(data, data.length);

  const multiplier = 2 / (period + 1);
  let ema = calculateSMA(data.slice(0, period), period);

  for (let i = period; i < data.length; i++) {
    ema = (data[i] - ema) * multiplier + ema;
  }

  return ema;
}

/**
 * RSI calculation
 */
function calculateRSI(data: number[], period: number = 14): number {
  if (data.length < period + 1) return 50;

  const changes = [];
  for (let i = 1; i < data.length; i++) {
    changes.push(data[i] - data[i - 1]);
  }

  const gains = changes.slice(-period).map((c) => (c > 0 ? c : 0));
  const losses = changes.slice(-period).map((c) => (c < 0 ? Math.abs(c) : 0));

  const avgGain = gains.reduce((a, b) => a + b, 0) / period;
  const avgLoss = losses.reduce((a, b) => a + b, 0) / period;

  if (avgLoss === 0) return 100;

  const rs = avgGain / avgLoss;
  return 100 - 100 / (1 + rs);
}

/**
 * Bollinger Bands calculation
 */
function calculateBollingerBands(
  data: number[],
  period: number = 20,
  stdDev: number = 2
): { upper: number; middle: number; lower: number } {
  const middle = calculateSMA(data, period);
  const slice = data.slice(-period);
  const squaredDiffs = slice.map((val) => Math.pow(val - middle, 2));
  const variance = squaredDiffs.reduce((a, b) => a + b, 0) / period;
  const standardDeviation = Math.sqrt(variance);

  return {
    upper: middle + standardDeviation * stdDev,
    middle,
    lower: middle - standardDeviation * stdDev,
  };
}

/**
 * MACD calculation
 */
function calculateMACD(data: number[]): { macd: number; signal: number; histogram: number } {
  const ema12 = calculateEMA(data, 12);
  const ema26 = calculateEMA(data, 26);
  const macdLine = ema12 - ema26;

  // For simplicity, using a fixed signal value
  // In a real implementation, you'd calculate EMA of MACD line
  const signalLine = macdLine * 0.9;
  const histogram = macdLine - signalLine;

  return {
    macd: macdLine,
    signal: signalLine,
    histogram,
  };
}

/**
 * ATR calculation
 */
function calculateATR(highs: number[], lows: number[], closes: number[], period: number = 14): number {
  const trueRanges = [];
  for (let i = 1; i < closes.length; i++) {
    const tr = Math.max(
      highs[i] - lows[i],
      Math.abs(highs[i] - closes[i - 1]),
      Math.abs(lows[i] - closes[i - 1])
    );
    trueRanges.push(tr);
  }

  return calculateSMA(trueRanges, Math.min(period, trueRanges.length));
}

/**
 * Calculate preview value for an indicator
 */
function calculateIndicatorPreview(
  indicator: IndicatorDefinition,
  parameters?: Record<string, any>
): number | { [key: string]: number } {
  const { prices, volumes } = SAMPLE_DATA;

  switch (indicator.id) {
    // EMAs
    case "ema9":
      return calculateEMA(prices, 9);
    case "ema20":
      return calculateEMA(prices, 20);
    case "ema50":
      return calculateEMA(prices, 50);

    // RSI
    case "rsi": {
      const period = parameters?.period || 14;
      return calculateRSI(prices, period);
    }

    // Bollinger Bands
    case "bb_upper":
    case "bb_middle":
    case "bb_lower": {
      const period = parameters?.period || 20;
      const stdDev = parameters?.stddev || 2;
      const bb = calculateBollingerBands(prices, period, stdDev);
      if (indicator.id === "bb_upper") return bb.upper;
      if (indicator.id === "bb_middle") return bb.middle;
      return bb.lower;
    }

    // MACD
    case "macd_line":
    case "macd_signal":
    case "macd_histogram": {
      const macd = calculateMACD(prices);
      if (indicator.id === "macd_line") return macd.macd;
      if (indicator.id === "macd_signal") return macd.signal;
      return macd.histogram;
    }

    // ATR
    case "atr": {
      const period = parameters?.period || 14;
      // Generate fake highs/lows based on prices
      const highs = prices.map((p) => p + Math.random() * 2);
      const lows = prices.map((p) => p - Math.random() * 2);
      return calculateATR(highs, lows, prices, period);
    }

    // Volume
    case "volume":
      return volumes[volumes.length - 1];
    case "avgVolume20":
      return calculateSMA(volumes, 20);

    // Price
    case "price":
    case "close":
      return prices[prices.length - 1];
    case "open":
      return prices[0];
    case "high":
      return Math.max(...prices);
    case "low":
      return Math.min(...prices);

    // VWAP (simplified)
    case "vwap": {
      const totalVolumePrice = prices.reduce((sum, price, i) => sum + price * volumes[i], 0);
      const totalVolume = volumes.reduce((a, b) => a + b, 0);
      return totalVolumePrice / totalVolume;
    }

    // Stochastic (simplified)
    case "stoch_k":
    case "stoch_d": {
      const period = 14;
      const recentPrices = prices.slice(-period);
      const highest = Math.max(...recentPrices);
      const lowest = Math.min(...recentPrices);
      const current = prices[prices.length - 1];
      const k = ((current - lowest) / (highest - lowest)) * 100;
      return indicator.id === "stoch_k" ? k : k * 0.9; // %D is smoothed %K
    }

    // Simple indicators with range
    case "adx":
      return 25 + Math.random() * 30; // 25-55
    case "cci":
      return (Math.random() - 0.5) * 200; // -100 to 100
    case "williams_r":
      return -50 - Math.random() * 50; // -100 to -50
    case "obv":
      return volumes.reduce((a, b) => a + b, 0);

    default:
      return 0;
  }
}

/**
 * Hook for calculating indicator preview values
 */
export function useIndicatorPreview(
  indicator: IndicatorDefinition | null,
  parameters?: Record<string, any>
) {
  const [previewValue, setPreviewValue] = useState<number | { [key: string]: number } | null>(null);
  const [isCalculating, setIsCalculating] = useState(false);

  useEffect(() => {
    if (!indicator) {
      setPreviewValue(null);
      return;
    }

    setIsCalculating(true);

    // Simulate async calculation with small delay
    const timer = setTimeout(() => {
      try {
        const value = calculateIndicatorPreview(indicator, parameters);
        setPreviewValue(value);
      } catch (error) {
        console.error("Error calculating indicator preview:", error);
        setPreviewValue(null);
      } finally {
        setIsCalculating(false);
      }
    }, 100);

    return () => clearTimeout(timer);
  }, [indicator, parameters]);

  return {
    previewValue,
    isCalculating,
    formattedValue: formatPreviewValue(indicator, previewValue),
  };
}

/**
 * Format preview value for display
 */
function formatPreviewValue(
  indicator: IndicatorDefinition | null,
  value: number | { [key: string]: number } | null
): string {
  if (!indicator || value === null) return "N/A";

  if (typeof value === "number") {
    // Check if indicator has a range (oscillators)
    if (indicator.range) {
      return `${value.toFixed(2)} (${indicator.range.min}-${indicator.range.max})`;
    }

    // Price-based indicators
    if (
      indicator.id.includes("price") ||
      indicator.id.includes("vwap") ||
      indicator.id.includes("ema") ||
      indicator.id.includes("bb_")
    ) {
      return `$${value.toFixed(2)}`;
    }

    // Volume-based
    if (indicator.id.includes("volume") || indicator.id === "obv") {
      return value.toLocaleString();
    }

    // ATR
    if (indicator.id === "atr") {
      return `$${value.toFixed(2)}`;
    }

    return value.toFixed(2);
  }

  // Multi-value indicators
  return JSON.stringify(value);
}

/**
 * Hook for getting indicator interpretation based on current value
 */
export function useIndicatorInterpretation(
  indicator: IndicatorDefinition | null,
  value: number | null
): "bullish" | "bearish" | "neutral" {
  if (!indicator || value === null) return "neutral";

  const { id, range } = indicator;

  // RSI
  if (id === "rsi") {
    if (value < 30) return "bullish"; // Oversold
    if (value > 70) return "bearish"; // Overbought
    return "neutral";
  }

  // Stochastic
  if (id === "stoch_k" || id === "stoch_d") {
    if (value < 20) return "bullish"; // Oversold
    if (value > 80) return "bearish"; // Overbought
    return "neutral";
  }

  // Williams %R
  if (id === "williams_r") {
    if (value < -80) return "bullish"; // Oversold
    if (value > -20) return "bearish"; // Overbought
    return "neutral";
  }

  // CCI
  if (id === "cci") {
    if (value < -100) return "bullish"; // Oversold
    if (value > 100) return "bearish"; // Overbought
    return "neutral";
  }

  // MACD Histogram
  if (id === "macd_histogram") {
    if (value > 0) return "bullish";
    if (value < 0) return "bearish";
    return "neutral";
  }

  // ADX (trend strength, not direction)
  if (id === "adx") {
    if (value > 25) return "bullish"; // Strong trend (could be either direction)
    return "neutral";
  }

  return "neutral";
}
