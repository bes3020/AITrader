// TypeScript interfaces matching C# backend models
// All DateTime types from C# are represented as ISO 8601 strings in TypeScript

/**
 * Supported futures symbols
 */
export type FuturesSymbol = "ES" | "NQ" | "YM" | "BTC" | "CL";

/**
 * Represents a 1-minute market data bar for futures trading.
 * Contains OHLCV data and pre-calculated technical indicators.
 * Supports multiple symbols: ES, NQ, YM, BTC, CL.
 * Uses composite primary key: (Symbol, Timestamp).
 */
export interface Bar {
  /**
   * The futures symbol (ES, NQ, YM, BTC, CL).
   * Part of composite primary key with Timestamp.
   */
  symbol: string;

  /**
   * The timestamp of the bar (UTC) in ISO 8601 format.
   * Part of composite primary key with Symbol.
   */
  timestamp: string;

  /**
   * Opening price of the bar.
   */
  open: number;

  /**
   * Highest price during the bar period.
   */
  high: number;

  /**
   * Lowest price during the bar period.
   */
  low: number;

  /**
   * Closing price of the bar.
   */
  close: number;

  /**
   * Total volume traded during the bar period.
   */
  volume: number;

  /**
   * Volume-Weighted Average Price for the bar.
   */
  vwap: number;

  /**
   * 9-period Exponential Moving Average.
   */
  ema9: number;

  /**
   * 20-period Exponential Moving Average.
   */
  ema20: number;

  /**
   * 50-period Exponential Moving Average.
   */
  ema50: number;

  /**
   * 20-period average volume.
   */
  avgVolume20: number;
}

/**
 * Represents a trading condition (entry or exit) for a strategy.
 * Conditions use indicators, operators, and values to define trading logic.
 */
export interface Condition {
  /**
   * Unique identifier for the condition.
   */
  id?: number;

  /**
   * The indicator to evaluate.
   * Valid values: "price", "volume", "vwap", "rsi", "atr", "ema9", "ema20", "ema50", "macd", "bb_upper", "bb_lower", "adx", "stoch"
   * Or "custom-{id}" for custom indicators.
   */
  indicator: string;

  /**
   * Comparison operator.
   * Valid values: ">", "<", ">=", "<=", "=", "crosses_above", "crosses_below"
   */
  operator: string;

  /**
   * The value to compare against.
   * Can be a number (e.g., "100"), an indicator name (e.g., "ema20"),
   * or an expression (e.g., "1.5x_average", "0.8x_vwap").
   */
  value: string;

  /**
   * Optional description of the condition logic.
   */
  description?: string | null;

  /**
   * Foreign key to the parent strategy.
   */
  strategyId?: number;

  /**
   * Foreign key to a custom indicator (if using custom indicator).
   * If set, this condition uses the custom indicator instead of the built-in indicator field.
   */
  customIndicatorId?: number | null;
}

/**
 * Represents a stop loss configuration for a trading strategy.
 * Defines how to exit losing trades to limit risk.
 */
export interface StopLoss {
  /**
   * Unique identifier for the stop loss.
   */
  id?: number;

  /**
   * Type of stop loss calculation.
   * Valid values: "points", "percentage", "atr" (Average True Range multiplier)
   */
  type: string;

  /**
   * The stop loss value.
   * - For "points": the number of points/ticks from entry
   * - For "percentage": the percentage (e.g., 2.0 for 2%)
   * - For "atr": the ATR multiplier (e.g., 1.5 for 1.5x ATR)
   */
  value: number;

  /**
   * Optional description of the stop loss logic.
   */
  description?: string | null;

  /**
   * Foreign key to the parent strategy.
   */
  strategyId?: number;
}

/**
 * Represents a take profit configuration for a trading strategy.
 * Defines how to exit winning trades to lock in profits.
 */
export interface TakeProfit {
  /**
   * Unique identifier for the take profit.
   */
  id?: number;

  /**
   * Type of take profit calculation.
   * Valid values: "points", "percentage", "atr" (Average True Range multiplier)
   */
  type: string;

  /**
   * The take profit value.
   * - For "points": the number of points/ticks from entry
   * - For "percentage": the percentage (e.g., 5.0 for 5%)
   * - For "atr": the ATR multiplier (e.g., 2.0 for 2x ATR)
   */
  value: number;

  /**
   * Optional description of the take profit logic.
   */
  description?: string | null;

  /**
   * Foreign key to the parent strategy.
   */
  strategyId?: number;
}

/**
 * Represents the result of a single trade execution.
 * Contains entry/exit details and performance metrics.
 */
export interface TradeResult {
  /**
   * Unique identifier for the trade result.
   */
  id?: number;

  /**
   * Timestamp when the trade was entered (ISO 8601 format).
   */
  entryTime: string;

  /**
   * Timestamp when the trade was exited (ISO 8601 format, null if still open).
   */
  exitTime: string | null;

  /**
   * Price at which the trade was entered.
   */
  entryPrice: number;

  /**
   * Price at which the trade was exited (null if still open).
   */
  exitPrice: number | null;

  /**
   * Profit or loss for this trade in points/currency.
   * Negative values indicate a loss.
   */
  pnl: number;

  /**
   * Trade outcome classification.
   * Valid values: "win", "loss", "timeout" (exited due to time/session end)
   */
  result: string;

  /**
   * Number of bars the trade was held.
   */
  barsHeld: number;

  /**
   * Maximum Adverse Excursion - the worst unrealized loss during the trade.
   * Always a non-positive value representing the peak drawdown.
   */
  maxAdverseExcursion: number;

  /**
   * Maximum Favorable Excursion - the best unrealized profit during the trade.
   * Always a non-negative value representing the peak profit.
   */
  maxFavorableExcursion: number;

  /**
   * Foreign key to the parent strategy result.
   */
  strategyResultId?: number;
}

/**
 * Represents the aggregate results of a strategy backtest or live run.
 * Contains performance metrics and AI-generated insights.
 */
export interface StrategyResult {
  /**
   * Unique identifier for the strategy result.
   */
  id?: number;

  /**
   * Foreign key to the strategy that was tested.
   */
  strategyId: number;

  /**
   * Total number of trades executed.
   */
  totalTrades: number;

  /**
   * Win rate as a decimal (e.g., 0.65 for 65% win rate).
   */
  winRate: number;

  /**
   * Total profit/loss across all trades.
   */
  totalPnl: number;

  /**
   * Average profit for winning trades.
   */
  avgWin: number;

  /**
   * Average loss for losing trades (always negative or zero).
   */
  avgLoss: number;

  /**
   * Maximum drawdown experienced during the test period.
   * Represents the largest peak-to-trough decline.
   */
  maxDrawdown: number;

  /**
   * Profit factor (gross profit / gross loss).
   * Values > 1.0 indicate profitability.
   */
  profitFactor: number | null;

  /**
   * Sharpe ratio - risk-adjusted return metric.
   * Higher values indicate better risk-adjusted performance.
   */
  sharpeRatio: number | null;

  /**
   * AI-generated insights and analysis of the strategy performance.
   * Includes observations about patterns, weaknesses, and suggestions.
   */
  insights: string | null;

  /**
   * Timestamp when this result was generated (ISO 8601 format).
   */
  createdAt: string;

  /**
   * Start date of the backtest period (ISO 8601 format).
   */
  backtestStart: string;

  /**
   * End date of the backtest period (ISO 8601 format).
   */
  backtestEnd: string;

  /**
   * Navigation property to the parent strategy.
   */
  strategy?: Strategy | null;

  /**
   * Navigation property to all individual trade results.
   */
  allTrades: TradeResult[];

  /**
   * Gets the worst performing trades (highest losses).
   */
  worstTrades?: TradeResult[];

  /**
   * Gets the best performing trades (highest profits).
   */
  bestTrades?: TradeResult[];
}

/**
 * Represents a complete trading strategy with entry conditions, exit rules, and metadata.
 */
export interface Strategy {
  /**
   * Unique identifier for the strategy.
   */
  id?: number;

  /**
   * Foreign key to the user who created this strategy.
   */
  userId?: number;

  /**
   * Name of the strategy.
   */
  name: string;

  /**
   * Detailed description of the strategy logic and goals.
   */
  description?: string | null;

  /**
   * Trading direction for this strategy.
   * Valid values: "long" (buy), "short" (sell), "both"
   */
  direction: string;

  /**
   * Symbol or ticker to trade (e.g., "ES", "NQ", "AAPL").
   */
  symbol?: string | null;

  /**
   * Timeframe for the strategy (e.g., "1m", "5m", "1h", "1d").
   */
  timeframe?: string | null;

  /**
   * Timestamp when the strategy was created (ISO 8601 format).
   */
  createdAt?: string;

  /**
   * Timestamp when the strategy was last updated (ISO 8601 format).
   */
  updatedAt?: string;

  /**
   * Indicates if the strategy is currently active.
   */
  isActive?: boolean;

  /**
   * Version number for tracking strategy iterations.
   */
  version?: number;

  /**
   * Maximum number of concurrent positions allowed.
   */
  maxPositions?: number;

  /**
   * Position size (number of contracts or shares).
   */
  positionSize?: number;

  /**
   * Parent strategy ID for version chains (null for root strategies).
   */
  parentStrategyId?: number | null;

  /**
   * Version number in the version chain (1 for root, 2+ for versions).
   */
  versionNumber?: number;

  /**
   * Tags for organizing strategies (stored as JSONB array).
   */
  tags?: string[] | null;

  /**
   * User notes about the strategy.
   */
  notes?: string | null;

  /**
   * Whether this strategy is marked as a favorite.
   */
  isFavorite?: boolean;

  /**
   * Timestamp when this strategy was last backtested.
   */
  lastBacktestedAt?: string | null;

  /**
   * Whether this strategy is archived (soft deleted).
   */
  isArchived?: boolean;

  /**
   * List of entry conditions that must be met to enter a trade.
   * All conditions are typically combined with AND logic.
   */
  entryConditions: Condition[];

  /**
   * Stop loss configuration for risk management.
   */
  stopLoss: StopLoss | null;

  /**
   * Take profit configuration for profit taking.
   */
  takeProfit: TakeProfit | null;

  /**
   * Historical results from backtests of this strategy.
   */
  results?: StrategyResult[];

  /**
   * Parent strategy (for version chains).
   */
  parentStrategy?: Strategy | null;

  /**
   * Child versions of this strategy.
   */
  versions?: Strategy[];
}

/**
 * Request to analyze a trading strategy from natural language description.
 * Supports multiple futures symbols: ES, NQ, YM, BTC, CL.
 */
export interface AnalyzeStrategyRequest {
  /**
   * Natural language description of the trading strategy.
   * Example: "Buy when price crosses above VWAP with stop at 10 points and target at 20 points"
   */
  description: string;

  /**
   * The futures symbol to backtest.
   * Supported symbols: ES (E-mini S&P 500), NQ (E-mini Nasdaq 100), YM (E-mini Dow), BTC (Bitcoin Futures), CL (Crude Oil).
   * Default: ES
   */
  symbol: string;

  /**
   * Start date for backtesting (inclusive) in ISO 8601 format.
   */
  startDate: string;

  /**
   * End date for backtesting (inclusive) in ISO 8601 format.
   */
  endDate: string;
}

/**
 * Response containing strategy analysis results.
 */
export interface AnalyzeStrategyResponse {
  /**
   * The parsed strategy with entry conditions, stop loss, and take profit.
   */
  strategy: Strategy;

  /**
   * Comprehensive analysis results including performance metrics and AI insights.
   */
  result: StrategyResult;

  /**
   * Total time taken to complete the analysis in milliseconds.
   */
  elapsedMilliseconds: number;

  /**
   * AI provider used for strategy parsing (Claude or Gemini).
   */
  aiProvider: string;
}

/**
 * Request to refine an existing strategy by adding new conditions.
 */
export interface RefineStrategyRequest {
  /**
   * The ID of the original strategy to refine.
   */
  strategyId: number;

  /**
   * New condition to add to the strategy.
   * Example: "and volume > 1.5x_average"
   */
  additionalCondition: string;

  /**
   * The futures symbol to backtest the refined strategy.
   * Supported symbols: ES (E-mini S&P 500), NQ (E-mini Nasdaq 100), YM (E-mini Dow), BTC (Bitcoin Futures), CL (Crude Oil).
   * Default: ES
   */
  symbol: string;

  /**
   * Start date for backtesting the refined strategy (ISO 8601 format).
   */
  startDate: string;

  /**
   * End date for backtesting the refined strategy (ISO 8601 format).
   */
  endDate: string;
}

/**
 * Symbol information including metadata and available data range.
 */
export interface SymbolInfo {
  /**
   * The futures symbol (ES, NQ, YM, BTC, CL).
   */
  symbol: string;

  /**
   * Human-readable name of the symbol.
   */
  name: string;

  /**
   * Point value (dollar value per point of movement).
   */
  pointValue: number;

  /**
   * Minimum tick size.
   */
  tickSize: number;

  /**
   * Dollar value of a single tick.
   */
  tickValue: number;

  /**
   * Earliest available data timestamp (ISO 8601 format).
   */
  minDate: string | null;

  /**
   * Latest available data timestamp (ISO 8601 format).
   */
  maxDate: string | null;

  /**
   * Total number of bars available for this symbol.
   */
  barCount: number;
}

/**
 * User information.
 */
export interface User {
  /**
   * Unique identifier for the user.
   */
  id: number;

  /**
   * User's email address.
   */
  email: string;

  /**
   * User's display name.
   */
  name?: string | null;

  /**
   * Timestamp when the user was created (ISO 8601 format).
   */
  createdAt: string;
}

/**
 * Detailed trade analysis with AI insights.
 */
export interface TradeAnalysis {
  id: number;
  tradeResultId: number;
  entryReason: string;
  exitReason: string;
  marketCondition: string;
  timeOfDay: string;
  dayOfWeek: string;
  vixLevel: number | null;
  adxValue: number | null;
  atrValue: number | null;
  whatWentWrong: string | null;
  whatWentRight: string | null;
  narrative: string | null;
  lessonsLearned: string | null;
  createdAt: string;
}

/**
 * Simplified bar data for charts.
 */
export interface BarData {
  timestamp: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

/**
 * Complete trade detail response with chart data and analysis.
 */
export interface TradeDetailResponse {
  trade: TradeResult;
  analysis: TradeAnalysis | null;
  chartData: BarData[] | null;
  indicatorSeries: Record<string, number[]> | null;
}

/**
 * Trade list summary statistics.
 */
export interface TradeListSummary {
  totalTrades: number;
  wins: number;
  losses: number;
  timeouts: number;
  totalPnl: number;
  avgPnl: number;
  winRate: number;
  avgWin: number;
  avgLoss: number;
  largestWin: number;
  largestLoss: number;
}

/**
 * Paginated trade list response.
 */
export interface TradeListResponse {
  trades: TradeResult[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  summary: TradeListSummary | null;
}

/**
 * Identified pattern in trades.
 */
export interface TradePattern {
  name: string;
  description: string;
  frequency: number;
  avgImpact: number;
  type: string;
  confidence: number;
}

/**
 * Heatmap cell data.
 */
export interface HeatmapCell {
  label: string;
  value: number;
  count: number;
  color: string;
  tooltip: string | null;
}

/**
 * Heatmap data for visualization.
 */
export interface HeatmapData {
  dimension: string;
  label: string;
  cells: HeatmapCell[];
}

/**
 * Trade list filters.
 */
export interface TradeFilters {
  result?: "win" | "loss" | "timeout";
  page?: number;
  pageSize?: number;
  sortBy?: "pnl" | "entryTime" | "duration";
}
