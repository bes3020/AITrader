/**
 * Central registry of all supported technical indicators
 * Provides metadata, configuration, and display information
 */

export type IndicatorCategory = "trend" | "momentum" | "volatility" | "volume" | "price" | "other";
export type ChartType = "overlay" | "separate" | "histogram" | "bands";

export interface IndicatorParameter {
  name: string;
  label: string;
  type: "number" | "select" | "boolean";
  default: number | string | boolean;
  min?: number;
  max?: number;
  step?: number;
  options?: { value: string | number; label: string }[];
  description: string;
}

export interface IndicatorOutput {
  name: string;
  label: string;
  color?: string;
  style?: "line" | "dashed" | "area" | "histogram";
}

export interface IndicatorDefinition {
  id: string;
  name: string;
  shortName: string;
  category: IndicatorCategory;
  description: string;
  formula: string;
  parameters: IndicatorParameter[];
  outputs: IndicatorOutput[];
  chartType: ChartType;
  icon: string; // lucide-react icon name
  color: string; // Category color
  range?: { min: number; max: number }; // Value range (e.g., RSI 0-100)
  interpretations: {
    bullish: string;
    bearish: string;
    neutral: string;
  };
  commonUsage: string[];
  examples: {
    condition: string;
    description: string;
  }[];
  warnings?: string[];
  learnMoreUrl?: string;
}

/**
 * Complete indicator registry
 */
export const INDICATOR_DEFINITIONS: Record<string, IndicatorDefinition> = {
  // ============================================================================
  // TREND INDICATORS
  // ============================================================================

  ema9: {
    id: "ema9",
    name: "Exponential Moving Average (9)",
    shortName: "EMA 9",
    category: "trend",
    description: "Fast exponential moving average, gives more weight to recent prices. Useful for identifying short-term trends.",
    formula: "EMA = Price(t) × k + EMA(y) × (1 − k), where k = 2/(N+1)",
    parameters: [],
    outputs: [{ name: "value", label: "EMA 9", color: "#3b82f6", style: "line" }],
    chartType: "overlay",
    icon: "TrendingUp",
    color: "#3b82f6",
    interpretations: {
      bullish: "Price above EMA 9 indicates short-term uptrend",
      bearish: "Price below EMA 9 indicates short-term downtrend",
      neutral: "Price crossing EMA 9 may signal trend change",
    },
    commonUsage: ["Scalping", "Day trading", "Quick entries"],
    examples: [
      {
        condition: "price > ema9",
        description: "Enter long when price is above the 9 EMA",
      },
      {
        condition: "price crosses_above ema9",
        description: "Enter when price crosses above the 9 EMA",
      },
    ],
  },

  ema20: {
    id: "ema20",
    name: "Exponential Moving Average (20)",
    shortName: "EMA 20",
    category: "trend",
    description: "Medium-term exponential moving average, balances responsiveness and smoothness.",
    formula: "EMA = Price(t) × k + EMA(y) × (1 − k), where k = 2/(N+1)",
    parameters: [],
    outputs: [{ name: "value", label: "EMA 20", color: "#8b5cf6", style: "line" }],
    chartType: "overlay",
    icon: "TrendingUp",
    color: "#3b82f6",
    interpretations: {
      bullish: "Price above EMA 20 indicates medium-term uptrend",
      bearish: "Price below EMA 20 indicates medium-term downtrend",
      neutral: "EMA 20 acts as dynamic support/resistance",
    },
    commonUsage: ["Swing trading", "Trend confirmation", "Support/Resistance"],
    examples: [
      {
        condition: "ema9 crosses_above ema20",
        description: "Golden cross - fast EMA crosses above slow EMA",
      },
      {
        condition: "price > ema20 AND volume > 1.5x_avgVolume20",
        description: "Trend confirmation with volume",
      },
    ],
  },

  ema50: {
    id: "ema50",
    name: "Exponential Moving Average (50)",
    shortName: "EMA 50",
    category: "trend",
    description: "Long-term exponential moving average, identifies major trends and acts as significant support/resistance.",
    formula: "EMA = Price(t) × k + EMA(y) × (1 − k), where k = 2/(N+1)",
    parameters: [],
    outputs: [{ name: "value", label: "EMA 50", color: "#ec4899", style: "line" }],
    chartType: "overlay",
    icon: "TrendingUp",
    color: "#3b82f6",
    interpretations: {
      bullish: "Price above EMA 50 indicates strong uptrend",
      bearish: "Price below EMA 50 indicates strong downtrend",
      neutral: "EMA 50 is a major support/resistance level",
    },
    commonUsage: ["Position trading", "Long-term trends", "Major reversals"],
    examples: [
      {
        condition: "price > ema50 AND ema20 > ema50",
        description: "Strong uptrend confirmation",
      },
    ],
  },

  macd_line: {
    id: "macd_line",
    name: "MACD Line",
    shortName: "MACD",
    category: "trend",
    description: "MACD line (12 EMA - 26 EMA). Shows momentum and trend direction.",
    formula: "MACD Line = 12-period EMA − 26-period EMA",
    parameters: [],
    outputs: [{ name: "value", label: "MACD Line", color: "#3b82f6", style: "line" }],
    chartType: "separate",
    icon: "Activity",
    color: "#3b82f6",
    interpretations: {
      bullish: "MACD above zero and rising",
      bearish: "MACD below zero and falling",
      neutral: "MACD crossing zero line",
    },
    commonUsage: ["Momentum", "Trend following", "Divergence"],
    examples: [
      {
        condition: "macd_line crosses_above macd_signal",
        description: "Classic MACD bullish crossover",
      },
      {
        condition: "macd_histogram > 0",
        description: "MACD momentum is positive",
      },
    ],
  },

  macd_signal: {
    id: "macd_signal",
    name: "MACD Signal Line",
    shortName: "MACD Signal",
    category: "trend",
    description: "9-period EMA of the MACD line. Used to identify MACD crossovers.",
    formula: "Signal Line = 9-period EMA of MACD Line",
    parameters: [],
    outputs: [{ name: "value", label: "Signal", color: "#f59e0b", style: "dashed" }],
    chartType: "separate",
    icon: "Activity",
    color: "#3b82f6",
    interpretations: {
      bullish: "MACD line above signal line",
      bearish: "MACD line below signal line",
      neutral: "MACD and signal crossing",
    },
    commonUsage: ["Crossover signals", "Momentum confirmation"],
    examples: [
      {
        condition: "macd_line > macd_signal AND macd_histogram > 0",
        description: "Strong bullish momentum",
      },
    ],
  },

  macd_histogram: {
    id: "macd_histogram",
    name: "MACD Histogram",
    shortName: "MACD Hist",
    category: "trend",
    description: "MACD Line - Signal Line. Visualizes the distance between MACD and signal.",
    formula: "Histogram = MACD Line − Signal Line",
    parameters: [],
    outputs: [{ name: "value", label: "Histogram", color: "#10b981", style: "histogram" }],
    chartType: "histogram",
    icon: "BarChart3",
    color: "#3b82f6",
    interpretations: {
      bullish: "Histogram positive and growing",
      bearish: "Histogram negative and declining",
      neutral: "Histogram crossing zero",
    },
    commonUsage: ["Momentum strength", "Trend acceleration"],
    examples: [
      {
        condition: "macd_histogram > 0",
        description: "Bullish momentum present",
      },
    ],
  },

  adx: {
    id: "adx",
    name: "Average Directional Index",
    shortName: "ADX",
    category: "trend",
    description: "Measures trend strength regardless of direction. Values above 25 indicate strong trend.",
    formula: "ADX = 100 × Moving Average of DX / ATR",
    parameters: [
      {
        name: "period",
        label: "Period",
        type: "number",
        default: 14,
        min: 7,
        max: 30,
        step: 1,
        description: "Number of periods for ADX calculation",
      },
    ],
    outputs: [{ name: "value", label: "ADX", color: "#8b5cf6", style: "line" }],
    chartType: "separate",
    icon: "Gauge",
    color: "#3b82f6",
    range: { min: 0, max: 100 },
    interpretations: {
      bullish: "ADX > 25 with price trending up = strong uptrend",
      bearish: "ADX > 25 with price trending down = strong downtrend",
      neutral: "ADX < 25 = weak/no trend (choppy market)",
    },
    commonUsage: ["Trend strength", "Filter choppy markets", "Trend following"],
    examples: [
      {
        condition: "adx > 25 AND price > ema20",
        description: "Strong uptrend confirmation",
      },
      {
        condition: "adx < 20",
        description: "Avoid trading - weak trend",
      },
    ],
  },

  psar: {
    id: "psar",
    name: "Parabolic SAR",
    shortName: "PSAR",
    category: "trend",
    description: "Stop and Reverse indicator. Dots above price = downtrend, below = uptrend.",
    formula: "SAR = SAR(previous) + α × (EP − SAR(previous))",
    parameters: [
      {
        name: "acceleration",
        label: "Acceleration",
        type: "number",
        default: 0.02,
        min: 0.01,
        max: 0.1,
        step: 0.01,
        description: "Acceleration factor",
      },
      {
        name: "max",
        label: "Max Acceleration",
        type: "number",
        default: 0.2,
        min: 0.1,
        max: 0.5,
        step: 0.05,
        description: "Maximum acceleration factor",
      },
    ],
    outputs: [{ name: "value", label: "PSAR", color: "#f59e0b", style: "line" }],
    chartType: "overlay",
    icon: "GitBranch",
    color: "#3b82f6",
    interpretations: {
      bullish: "PSAR dots below price",
      bearish: "PSAR dots above price",
      neutral: "PSAR flips = trend reversal signal",
    },
    commonUsage: ["Trailing stops", "Trend reversal", "Exit signals"],
    examples: [
      {
        condition: "price > psar",
        description: "Enter/stay long while above PSAR",
      },
    ],
    warnings: ["Whipsaws in sideways markets", "Best in trending markets"],
  },

  ichimoku_tenkan: {
    id: "ichimoku_tenkan",
    name: "Ichimoku Tenkan-sen (Conversion Line)",
    shortName: "Tenkan",
    category: "trend",
    description: "Short-term trend indicator. (9-period high + 9-period low) / 2",
    formula: "Tenkan = (9-period High + 9-period Low) / 2",
    parameters: [],
    outputs: [{ name: "value", label: "Tenkan", color: "#ef4444", style: "line" }],
    chartType: "overlay",
    icon: "Cloud",
    color: "#3b82f6",
    interpretations: {
      bullish: "Price above Tenkan",
      bearish: "Price below Tenkan",
      neutral: "Tenkan crosses Kijun",
    },
    commonUsage: ["Ichimoku system", "Short-term trend"],
    examples: [
      {
        condition: "ichimoku_tenkan crosses_above ichimoku_kijun",
        description: "Ichimoku bullish signal",
      },
    ],
  },

  ichimoku_kijun: {
    id: "ichimoku_kijun",
    name: "Ichimoku Kijun-sen (Base Line)",
    shortName: "Kijun",
    category: "trend",
    description: "Medium-term trend indicator. (26-period high + 26-period low) / 2",
    formula: "Kijun = (26-period High + 26-period Low) / 2",
    parameters: [],
    outputs: [{ name: "value", label: "Kijun", color: "#3b82f6", style: "line" }],
    chartType: "overlay",
    icon: "Cloud",
    color: "#3b82f6",
    interpretations: {
      bullish: "Price above Kijun and Kijun rising",
      bearish: "Price below Kijun and Kijun falling",
      neutral: "Kijun flat = consolidation",
    },
    commonUsage: ["Ichimoku system", "Support/Resistance"],
    examples: [
      {
        condition: "price > ichimoku_kijun",
        description: "Above Kijun support",
      },
    ],
  },

  ichimoku_senkou_a: {
    id: "ichimoku_senkou_a",
    name: "Ichimoku Senkou Span A (Leading Span A)",
    shortName: "Senkou A",
    category: "trend",
    description: "Cloud boundary. (Tenkan + Kijun) / 2, plotted 26 periods ahead.",
    formula: "Senkou A = (Tenkan + Kijun) / 2",
    parameters: [],
    outputs: [{ name: "value", label: "Senkou A", color: "#22c55e", style: "area" }],
    chartType: "overlay",
    icon: "Cloud",
    color: "#3b82f6",
    interpretations: {
      bullish: "Price above cloud (Senkou A & B)",
      bearish: "Price below cloud",
      neutral: "Price in cloud = consolidation",
    },
    commonUsage: ["Cloud support/resistance", "Future trend"],
    examples: [
      {
        condition: "price > ichimoku_senkou_a AND price > ichimoku_senkou_b",
        description: "Above the cloud - bullish",
      },
    ],
  },

  ichimoku_senkou_b: {
    id: "ichimoku_senkou_b",
    name: "Ichimoku Senkou Span B (Leading Span B)",
    shortName: "Senkou B",
    category: "trend",
    description: "Cloud boundary. (52-period high + 52-period low) / 2, plotted 26 periods ahead.",
    formula: "Senkou B = (52-period High + 52-period Low) / 2",
    parameters: [],
    outputs: [{ name: "value", label: "Senkou B", color: "#ef4444", style: "area" }],
    chartType: "overlay",
    icon: "Cloud",
    color: "#3b82f6",
    interpretations: {
      bullish: "Senkou A above Senkou B (green cloud)",
      bearish: "Senkou A below Senkou B (red cloud)",
      neutral: "Cloud twist = potential reversal",
    },
    commonUsage: ["Strong support/resistance", "Long-term trend"],
    examples: [
      {
        condition: "ichimoku_senkou_a > ichimoku_senkou_b",
        description: "Bullish cloud formation",
      },
    ],
  },

  ichimoku_chikou: {
    id: "ichimoku_chikou",
    name: "Ichimoku Chikou Span (Lagging Span)",
    shortName: "Chikou",
    category: "trend",
    description: "Current close plotted 26 periods back. Confirms trend strength.",
    formula: "Chikou = Current Close (plotted 26 periods back)",
    parameters: [],
    outputs: [{ name: "value", label: "Chikou", color: "#a855f7", style: "line" }],
    chartType: "overlay",
    icon: "Cloud",
    color: "#3b82f6",
    interpretations: {
      bullish: "Chikou above price (from 26 bars ago)",
      bearish: "Chikou below price (from 26 bars ago)",
      neutral: "Chikou crossing price = momentum shift",
    },
    commonUsage: ["Momentum confirmation", "Trend validation"],
    examples: [],
  },

  // ============================================================================
  // MOMENTUM INDICATORS
  // ============================================================================

  rsi: {
    id: "rsi",
    name: "Relative Strength Index",
    shortName: "RSI",
    category: "momentum",
    description: "Momentum oscillator measuring speed and change of price movements. Range: 0-100.",
    formula: "RSI = 100 − [100 / (1 + (Average Gain / Average Loss))]",
    parameters: [
      {
        name: "period",
        label: "Period",
        type: "number",
        default: 14,
        min: 5,
        max: 30,
        step: 1,
        description: "Number of periods for RSI calculation",
      },
      {
        name: "overbought",
        label: "Overbought Level",
        type: "number",
        default: 70,
        min: 60,
        max: 90,
        step: 5,
        description: "Overbought threshold",
      },
      {
        name: "oversold",
        label: "Oversold Level",
        type: "number",
        default: 30,
        min: 10,
        max: 40,
        step: 5,
        description: "Oversold threshold",
      },
    ],
    outputs: [{ name: "value", label: "RSI", color: "#8b5cf6", style: "line" }],
    chartType: "separate",
    icon: "Gauge",
    color: "#8b5cf6",
    range: { min: 0, max: 100 },
    interpretations: {
      bullish: "RSI < 30 (oversold) - potential reversal up",
      bearish: "RSI > 70 (overbought) - potential reversal down",
      neutral: "RSI 40-60 - neutral momentum",
    },
    commonUsage: ["Overbought/oversold", "Divergence", "Momentum"],
    examples: [
      {
        condition: "rsi < 30",
        description: "Oversold - potential buy signal",
      },
      {
        condition: "rsi > 70",
        description: "Overbought - potential sell signal",
      },
      {
        condition: "rsi > 50 AND price > ema20",
        description: "Bullish momentum confirmation",
      },
    ],
    warnings: ["Can stay overbought/oversold in strong trends"],
  },

  stoch_k: {
    id: "stoch_k",
    name: "Stochastic %K",
    shortName: "Stoch %K",
    category: "momentum",
    description: "Fast stochastic oscillator. Shows where price closed relative to range. Range: 0-100.",
    formula: "%K = 100 × [(Close − Low14) / (High14 − Low14)]",
    parameters: [
      {
        name: "k_period",
        label: "%K Period",
        type: "number",
        default: 14,
        min: 5,
        max: 30,
        step: 1,
        description: "Period for %K calculation",
      },
    ],
    outputs: [{ name: "value", label: "%K", color: "#3b82f6", style: "line" }],
    chartType: "separate",
    icon: "Activity",
    color: "#8b5cf6",
    range: { min: 0, max: 100 },
    interpretations: {
      bullish: "%K < 20 and rising = oversold reversal",
      bearish: "%K > 80 and falling = overbought reversal",
      neutral: "%K crosses %D = momentum shift",
    },
    commonUsage: ["Overbought/oversold", "Crossover signals"],
    examples: [
      {
        condition: "stoch_k < 20",
        description: "Oversold condition",
      },
      {
        condition: "stoch_k crosses_above stoch_d",
        description: "Bullish stochastic crossover",
      },
    ],
  },

  stoch_d: {
    id: "stoch_d",
    name: "Stochastic %D",
    shortName: "Stoch %D",
    category: "momentum",
    description: "Slow stochastic oscillator. 3-period SMA of %K. Smoother signal line.",
    formula: "%D = 3-period SMA of %K",
    parameters: [],
    outputs: [{ name: "value", label: "%D", color: "#f59e0b", style: "dashed" }],
    chartType: "separate",
    icon: "Activity",
    color: "#8b5cf6",
    range: { min: 0, max: 100 },
    interpretations: {
      bullish: "%K crosses above %D while oversold",
      bearish: "%K crosses below %D while overbought",
      neutral: "%D acts as signal line for %K",
    },
    commonUsage: ["Signal line for stochastic", "Crossover confirmation"],
    examples: [
      {
        condition: "stoch_k > stoch_d AND stoch_k > 20",
        description: "Bullish momentum after oversold",
      },
    ],
  },

  cci: {
    id: "cci",
    name: "Commodity Channel Index",
    shortName: "CCI",
    category: "momentum",
    description: "Measures deviation from average price. Values beyond ±100 indicate overbought/oversold.",
    formula: "CCI = (Typical Price − SMA) / (0.015 × Mean Deviation)",
    parameters: [
      {
        name: "period",
        label: "Period",
        type: "number",
        default: 20,
        min: 10,
        max: 50,
        step: 5,
        description: "Period for CCI calculation",
      },
    ],
    outputs: [{ name: "value", label: "CCI", color: "#06b6d4", style: "line" }],
    chartType: "separate",
    icon: "Activity",
    color: "#8b5cf6",
    interpretations: {
      bullish: "CCI crosses above +100 = strong upward move",
      bearish: "CCI crosses below -100 = strong downward move",
      neutral: "CCI between ±100 = normal range",
    },
    commonUsage: ["Overbought/oversold", "Trend strength", "Breakouts"],
    examples: [
      {
        condition: "cci > 100",
        description: "Overbought - strong momentum up",
      },
      {
        condition: "cci < -100",
        description: "Oversold - strong momentum down",
      },
    ],
  },

  williams_r: {
    id: "williams_r",
    name: "Williams %R",
    shortName: "Williams %R",
    category: "momentum",
    description: "Momentum indicator similar to stochastic. Range: -100 to 0.",
    formula: "%R = -100 × [(High14 − Close) / (High14 − Low14)]",
    parameters: [
      {
        name: "period",
        label: "Period",
        type: "number",
        default: 14,
        min: 5,
        max: 30,
        step: 1,
        description: "Lookback period",
      },
    ],
    outputs: [{ name: "value", label: "Williams %R", color: "#ec4899", style: "line" }],
    chartType: "separate",
    icon: "Activity",
    color: "#8b5cf6",
    range: { min: -100, max: 0 },
    interpretations: {
      bullish: "%R > -20 = overbought",
      bearish: "%R < -80 = oversold",
      neutral: "%R between -20 and -80 = neutral",
    },
    commonUsage: ["Overbought/oversold", "Entry timing"],
    examples: [
      {
        condition: "williams_r < -80",
        description: "Oversold - potential buy",
      },
      {
        condition: "williams_r > -20",
        description: "Overbought - potential sell",
      },
    ],
  },

  // ============================================================================
  // VOLATILITY INDICATORS
  // ============================================================================

  atr: {
    id: "atr",
    name: "Average True Range",
    shortName: "ATR",
    category: "volatility",
    description: "Measures market volatility. Higher ATR = more volatile. Used for position sizing and stops.",
    formula: "ATR = Moving Average of True Range",
    parameters: [
      {
        name: "period",
        label: "Period",
        type: "number",
        default: 14,
        min: 7,
        max: 30,
        step: 1,
        description: "Period for ATR calculation",
      },
    ],
    outputs: [{ name: "value", label: "ATR", color: "#f97316", style: "line" }],
    chartType: "separate",
    icon: "Activity",
    color: "#f97316",
    interpretations: {
      bullish: "High ATR = high volatility (opportunity)",
      bearish: "Low ATR = low volatility (consolidation)",
      neutral: "Rising ATR = increasing volatility",
    },
    commonUsage: ["Position sizing", "Stop placement", "Volatility filter"],
    examples: [
      {
        condition: "atr > 2.0",
        description: "High volatility - adjust position size",
      },
    ],
    warnings: ["ATR doesn't indicate direction, only volatility"],
  },

  bb_upper: {
    id: "bb_upper",
    name: "Bollinger Bands Upper",
    shortName: "BB Upper",
    category: "volatility",
    description: "Upper Bollinger Band. SMA + (2 × Standard Deviation). Price touching = overbought.",
    formula: "Upper Band = SMA(20) + 2 × σ",
    parameters: [
      {
        name: "period",
        label: "Period",
        type: "number",
        default: 20,
        min: 10,
        max: 50,
        step: 5,
        description: "Period for SMA",
      },
      {
        name: "stddev",
        label: "Std Deviation",
        type: "number",
        default: 2,
        min: 1,
        max: 3,
        step: 0.5,
        description: "Standard deviation multiplier",
      },
    ],
    outputs: [{ name: "value", label: "Upper Band", color: "#ef4444", style: "line" }],
    chartType: "bands",
    icon: "ChevronsUp",
    color: "#f97316",
    interpretations: {
      bullish: "Price bounces off lower band",
      bearish: "Price touches upper band repeatedly",
      neutral: "Price between bands = normal",
    },
    commonUsage: ["Overbought", "Breakout detection", "Volatility"],
    examples: [
      {
        condition: "price > bb_upper",
        description: "Price above upper band - overbought or breakout",
      },
      {
        condition: "price crosses_above bb_upper AND volume > 1.5x_avgVolume20",
        description: "Breakout with volume confirmation",
      },
    ],
  },

  bb_middle: {
    id: "bb_middle",
    name: "Bollinger Bands Middle (SMA)",
    shortName: "BB Middle",
    category: "volatility",
    description: "Middle Bollinger Band. Simple 20-period moving average.",
    formula: "Middle Band = SMA(20)",
    parameters: [],
    outputs: [{ name: "value", label: "Middle Band", color: "#6b7280", style: "dashed" }],
    chartType: "overlay",
    icon: "Minus",
    color: "#f97316",
    interpretations: {
      bullish: "Price above middle band",
      bearish: "Price below middle band",
      neutral: "Middle band = mean reversion target",
    },
    commonUsage: ["Mean reversion", "Trend reference"],
    examples: [
      {
        condition: "price crosses_above bb_middle",
        description: "Price crossing above mean",
      },
    ],
  },

  bb_lower: {
    id: "bb_lower",
    name: "Bollinger Bands Lower",
    shortName: "BB Lower",
    category: "volatility",
    description: "Lower Bollinger Band. SMA − (2 × Standard Deviation). Price touching = oversold.",
    formula: "Lower Band = SMA(20) − 2 × σ",
    parameters: [],
    outputs: [{ name: "value", label: "Lower Band", color: "#22c55e", style: "line" }],
    chartType: "bands",
    icon: "ChevronsDown",
    color: "#f97316",
    interpretations: {
      bullish: "Price touches lower band = oversold",
      bearish: "Price bounces off upper band",
      neutral: "Squeeze = low volatility before breakout",
    },
    commonUsage: ["Oversold", "Mean reversion", "Support"],
    examples: [
      {
        condition: "price < bb_lower",
        description: "Price below lower band - oversold",
      },
      {
        condition: "price crosses_above bb_lower",
        description: "Bounce from oversold",
      },
    ],
  },

  // ============================================================================
  // VOLUME INDICATORS
  // ============================================================================

  vwap: {
    id: "vwap",
    name: "Volume Weighted Average Price",
    shortName: "VWAP",
    category: "volume",
    description: "Average price weighted by volume. Institutional traders use it as a benchmark.",
    formula: "VWAP = Σ(Price × Volume) / Σ(Volume)",
    parameters: [],
    outputs: [{ name: "value", label: "VWAP", color: "#10b981", style: "line" }],
    chartType: "overlay",
    icon: "BarChart3",
    color: "#10b981",
    interpretations: {
      bullish: "Price above VWAP = buyers in control",
      bearish: "Price below VWAP = sellers in control",
      neutral: "VWAP acts as intraday support/resistance",
    },
    commonUsage: ["Intraday trading", "Fair value", "Institutional benchmark"],
    examples: [
      {
        condition: "price > vwap",
        description: "Trading above VWAP - bullish",
      },
      {
        condition: "price crosses_above vwap AND volume > 1.5x_avgVolume20",
        description: "VWAP breakout with volume",
      },
    ],
    warnings: ["Resets daily", "Best for intraday"],
  },

  obv: {
    id: "obv",
    name: "On-Balance Volume",
    shortName: "OBV",
    category: "volume",
    description: "Cumulative volume indicator. Rising OBV = accumulation, falling = distribution.",
    formula: "OBV = OBV(prev) + Volume (if close up) or − Volume (if close down)",
    parameters: [],
    outputs: [{ name: "value", label: "OBV", color: "#14b8a6", style: "line" }],
    chartType: "separate",
    icon: "TrendingUp",
    color: "#10b981",
    interpretations: {
      bullish: "OBV rising with price = confirmed uptrend",
      bearish: "OBV falling with price = confirmed downtrend",
      neutral: "OBV divergence = potential reversal",
    },
    commonUsage: ["Volume confirmation", "Divergence", "Accumulation/Distribution"],
    examples: [
      {
        condition: "obv > 0 AND price > ema20",
        description: "Volume supporting uptrend",
      },
    ],
  },

  volume: {
    id: "volume",
    name: "Volume",
    shortName: "Volume",
    category: "volume",
    description: "Number of shares/contracts traded. Confirms price moves.",
    formula: "Volume = Count of traded units",
    parameters: [],
    outputs: [{ name: "value", label: "Volume", color: "#06b6d4", style: "histogram" }],
    chartType: "histogram",
    icon: "BarChart4",
    color: "#10b981",
    interpretations: {
      bullish: "High volume on up days = strong buying",
      bearish: "High volume on down days = strong selling",
      neutral: "Low volume = weak conviction",
    },
    commonUsage: ["Confirmation", "Breakout validation", "Trend strength"],
    examples: [
      {
        condition: "volume > 1.5x_avgVolume20",
        description: "Above average volume - significant move",
      },
      {
        condition: "volume > 2x_avgVolume20 AND price > prev_day_high",
        description: "Breakout with strong volume",
      },
    ],
  },

  avgVolume20: {
    id: "avgVolume20",
    name: "Average Volume (20)",
    shortName: "Avg Vol",
    category: "volume",
    description: "20-period average volume. Baseline for comparing current volume.",
    formula: "Average Volume = Sum of Volume over 20 periods / 20",
    parameters: [],
    outputs: [{ name: "value", label: "Avg Volume", color: "#64748b", style: "line" }],
    chartType: "separate",
    icon: "BarChart2",
    color: "#10b981",
    interpretations: {
      bullish: "Volume > average = increased interest",
      bearish: "Volume < average = decreased interest",
      neutral: "Compare current vs average volume",
    },
    commonUsage: ["Volume baseline", "Relative volume"],
    examples: [
      {
        condition: "volume > 1.5x_avgVolume20",
        description: "50% above average volume",
      },
    ],
  },

  // ============================================================================
  // BASIC PRICE INDICATORS
  // ============================================================================

  price: {
    id: "price",
    name: "Price (Close)",
    shortName: "Price",
    category: "trend",
    description: "Current closing price. The most basic indicator.",
    formula: "Close price of the current bar",
    parameters: [],
    outputs: [{ name: "value", label: "Price", color: "#000000", style: "line" }],
    chartType: "overlay",
    icon: "DollarSign",
    color: "#3b82f6",
    interpretations: {
      bullish: "Price rising",
      bearish: "Price falling",
      neutral: "Price flat",
    },
    commonUsage: ["Primary data", "Comparison base"],
    examples: [
      {
        condition: "price > ema20",
        description: "Price above moving average",
      },
    ],
  },

  open: {
    id: "open",
    name: "Open Price",
    shortName: "Open",
    category: "trend",
    description: "Opening price of the bar.",
    formula: "Open price of the current bar",
    parameters: [],
    outputs: [{ name: "value", label: "Open", color: "#64748b", style: "line" }],
    chartType: "overlay",
    icon: "Circle",
    color: "#3b82f6",
    interpretations: {
      bullish: "Close > Open = bullish bar",
      bearish: "Close < Open = bearish bar",
      neutral: "Open price reference",
    },
    commonUsage: ["Bar analysis", "Gap detection"],
    examples: [],
  },

  high: {
    id: "high",
    name: "High Price",
    shortName: "High",
    category: "trend",
    description: "Highest price reached during the bar.",
    formula: "High price of the current bar",
    parameters: [],
    outputs: [{ name: "value", label: "High", color: "#22c55e", style: "line" }],
    chartType: "overlay",
    icon: "ArrowUp",
    color: "#3b82f6",
    interpretations: {
      bullish: "Higher highs = uptrend",
      bearish: "Lower highs = downtrend",
      neutral: "High price reference",
    },
    commonUsage: ["Trend analysis", "Resistance"],
    examples: [],
  },

  low: {
    id: "low",
    name: "Low Price",
    shortName: "Low",
    category: "trend",
    description: "Lowest price reached during the bar.",
    formula: "Low price of the current bar",
    parameters: [],
    outputs: [{ name: "value", label: "Low", color: "#ef4444", style: "line" }],
    chartType: "overlay",
    icon: "ArrowDown",
    color: "#3b82f6",
    interpretations: {
      bullish: "Higher lows = uptrend",
      bearish: "Lower lows = downtrend",
      neutral: "Low price reference",
    },
    commonUsage: ["Trend analysis", "Support"],
    examples: [],
  },

  prev_day_high: {
    id: "prev_day_high",
    name: "Previous Day High",
    shortName: "Prev High",
    category: "trend",
    description: "Highest price from the previous trading day. Key resistance level.",
    formula: "Max(High) from previous day",
    parameters: [],
    outputs: [{ name: "value", label: "Prev Day High", color: "#dc2626", style: "dashed" }],
    chartType: "overlay",
    icon: "TrendingUp",
    color: "#3b82f6",
    interpretations: {
      bullish: "Break above prev day high = bullish",
      bearish: "Rejection at prev day high = resistance",
      neutral: "Key intraday level",
    },
    commonUsage: ["Day trading", "Breakout", "Resistance"],
    examples: [
      {
        condition: "price > prev_day_high",
        description: "Breakout above yesterday's high",
      },
    ],
  },

  prev_day_low: {
    id: "prev_day_low",
    name: "Previous Day Low",
    shortName: "Prev Low",
    category: "trend",
    description: "Lowest price from the previous trading day. Key support level.",
    formula: "Min(Low) from previous day",
    parameters: [],
    outputs: [{ name: "value", label: "Prev Day Low", color: "#16a34a", style: "dashed" }],
    chartType: "overlay",
    icon: "TrendingDown",
    color: "#3b82f6",
    interpretations: {
      bullish: "Bounce at prev day low = support",
      bearish: "Break below prev day low = bearish",
      neutral: "Key intraday level",
    },
    commonUsage: ["Day trading", "Breakdown", "Support"],
    examples: [
      {
        condition: "price < prev_day_low",
        description: "Breakdown below yesterday's low",
      },
    ],
  },

  time: {
    id: "time",
    name: "Time of Day",
    shortName: "Time",
    category: "trend",
    description: "Time in minutes since midnight. Used for time-based filters.",
    formula: "Minutes since midnight (Hour × 60 + Minute)",
    parameters: [],
    outputs: [{ name: "value", label: "Time", color: "#6b7280", style: "line" }],
    chartType: "separate",
    icon: "Clock",
    color: "#3b82f6",
    interpretations: {
      bullish: "Market open = high activity",
      bearish: "Market close = reduced activity",
      neutral: "Time-based filtering",
    },
    commonUsage: ["Time filters", "Session trading"],
    examples: [
      {
        condition: "time >= 570 AND time <= 600",
        description: "Trade only 9:30-10:00 AM",
      },
    ],
  },
};

/**
 * Get indicators by category
 */
export function getIndicatorsByCategory(
  category: IndicatorCategory
): IndicatorDefinition[] {
  return Object.values(INDICATOR_DEFINITIONS).filter(
    (ind) => ind.category === category
  );
}

/**
 * Get all category colors
 */
export const CATEGORY_COLORS: Record<IndicatorCategory, string> = {
  trend: "#3b82f6", // Blue
  momentum: "#8b5cf6", // Purple
  volatility: "#f97316", // Orange
  volume: "#10b981", // Green
  price: "#64748b", // Slate
  other: "#6b7280", // Gray
};

/**
 * Get indicator by ID
 */
export function getIndicator(id: string): IndicatorDefinition | undefined {
  return INDICATOR_DEFINITIONS[id];
}

/**
 * Search indicators by name or description
 */
export function searchIndicators(query: string): IndicatorDefinition[] {
  const lowerQuery = query.toLowerCase();
  return Object.values(INDICATOR_DEFINITIONS).filter(
    (ind) =>
      ind.name.toLowerCase().includes(lowerQuery) ||
      ind.shortName.toLowerCase().includes(lowerQuery) ||
      ind.description.toLowerCase().includes(lowerQuery)
  );
}

/**
 * Get recently used indicators from localStorage
 */
export function getRecentIndicators(limit: number = 5): IndicatorDefinition[] {
  if (typeof window === "undefined") return [];

  const recent = localStorage.getItem("recentIndicators");
  if (!recent) return [];

  const ids = JSON.parse(recent) as string[];
  return ids
    .map((id) => INDICATOR_DEFINITIONS[id])
    .filter(Boolean)
    .slice(0, limit);
}

/**
 * Add indicator to recently used
 */
export function addRecentIndicator(id: string): void {
  if (typeof window === "undefined") return;

  const recent = localStorage.getItem("recentIndicators");
  const ids = recent ? (JSON.parse(recent) as string[]) : [];

  // Remove if exists, then add to front
  const filtered = ids.filter((i) => i !== id);
  filtered.unshift(id);

  // Keep max 10
  const limited = filtered.slice(0, 10);

  localStorage.setItem("recentIndicators", JSON.stringify(limited));
}

/**
 * Get favorite indicators from localStorage
 */
export function getFavoriteIndicators(): IndicatorDefinition[] {
  if (typeof window === "undefined") return [];

  const favorites = localStorage.getItem("favoriteIndicators");
  if (!favorites) return [];

  const ids = JSON.parse(favorites) as string[];
  return ids.map((id) => INDICATOR_DEFINITIONS[id]).filter(Boolean);
}

/**
 * Toggle indicator favorite
 */
export function toggleFavoriteIndicator(id: string): void {
  if (typeof window === "undefined") return;

  const favorites = localStorage.getItem("favoriteIndicators");
  const ids = favorites ? (JSON.parse(favorites) as string[]) : [];

  if (ids.includes(id)) {
    // Remove
    const filtered = ids.filter((i) => i !== id);
    localStorage.setItem("favoriteIndicators", JSON.stringify(filtered));
  } else {
    // Add
    ids.push(id);
    localStorage.setItem("favoriteIndicators", JSON.stringify(ids));
  }
}

/**
 * Check if indicator is favorited
 */
export function isIndicatorFavorited(id: string): boolean {
  if (typeof window === "undefined") return false;

  const favorites = localStorage.getItem("favoriteIndicators");
  if (!favorites) return false;

  const ids = JSON.parse(favorites) as string[];
  return ids.includes(id);
}

/**
 * Indicator presets for common strategies
 */
export interface IndicatorPreset {
  id: string;
  name: string;
  description: string;
  indicators: string[];
  category: "scalping" | "day-trading" | "swing-trading" | "mean-reversion";
}

export const INDICATOR_PRESETS: IndicatorPreset[] = [
  {
    id: "scalping",
    name: "Scalping Setup",
    description: "Fast EMAs and VWAP for quick entries",
    indicators: ["ema9", "ema20", "vwap", "volume"],
    category: "scalping",
  },
  {
    id: "trend-following",
    name: "Trend Following",
    description: "Long-term trend identification and confirmation",
    indicators: ["ema50", "adx", "macd_line", "macd_signal"],
    category: "swing-trading",
  },
  {
    id: "mean-reversion",
    name: "Mean Reversion",
    description: "Identify overbought/oversold conditions",
    indicators: ["bb_upper", "bb_lower", "rsi", "cci"],
    category: "mean-reversion",
  },
  {
    id: "momentum",
    name: "Momentum Trading",
    description: "Strong momentum with volume confirmation",
    indicators: ["rsi", "macd_histogram", "obv", "volume"],
    category: "day-trading",
  },
  {
    id: "ichimoku-cloud",
    name: "Ichimoku Cloud System",
    description: "Complete Ichimoku trading system",
    indicators: [
      "ichimoku_tenkan",
      "ichimoku_kijun",
      "ichimoku_senkou_a",
      "ichimoku_senkou_b",
    ],
    category: "swing-trading",
  },
];
