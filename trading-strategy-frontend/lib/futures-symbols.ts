/**
 * Futures symbols configuration and utilities
 */

export type FuturesSymbol = "ES" | "NQ" | "YM" | "BTC" | "CL";

/**
 * Symbol information interface
 */
export interface SymbolInfo {
  symbol: FuturesSymbol;
  name: string;
  pointValue: number;
  tickSize: number;
  tickValue: number;
  description: string;
}

/**
 * Array of all supported futures symbols with their specifications
 */
export const FUTURES_SYMBOLS: SymbolInfo[] = [
  {
    symbol: "ES",
    name: "E-mini S&P 500",
    pointValue: 50,
    tickSize: 0.25,
    tickValue: 12.5,
    description: "Most liquid equity index futures",
  },
  {
    symbol: "NQ",
    name: "E-mini Nasdaq 100",
    pointValue: 20,
    tickSize: 0.25,
    tickValue: 5.0,
    description: "Tech-heavy index futures",
  },
  {
    symbol: "YM",
    name: "E-mini Dow",
    pointValue: 5,
    tickSize: 1.0,
    tickValue: 5.0,
    description: "Dow Jones Industrial Average futures",
  },
  {
    symbol: "BTC",
    name: "Bitcoin Futures",
    pointValue: 5,
    tickSize: 5.0,
    tickValue: 25.0,
    description: "Cryptocurrency futures with high volatility",
  },
  {
    symbol: "CL",
    name: "Crude Oil",
    pointValue: 1000,
    tickSize: 0.01,
    tickValue: 10.0,
    description: "Energy sector futures",
  },
];

/**
 * Gets symbol information by symbol code
 * @param symbol - The futures symbol code
 * @returns Symbol information or undefined if not found
 */
export function getSymbolInfo(symbol: string): SymbolInfo | undefined {
  return FUTURES_SYMBOLS.find((s) => s.symbol === symbol);
}

/**
 * Formats P&L with symbol-specific formatting
 * @param pnl - Profit/Loss amount in dollars
 * @param symbol - The futures symbol code
 * @returns Formatted P&L string with symbol context
 */
export function formatPnL(pnl: number, symbol: string): string {
  const symbolInfo = getSymbolInfo(symbol);

  if (!symbolInfo) {
    return `$${pnl.toFixed(2)}`;
  }

  const sign = pnl >= 0 ? "+" : "";
  const formatted = `${sign}$${pnl.toFixed(2)}`;

  // Calculate points moved for context
  const points = pnl / symbolInfo.pointValue;
  const pointsFormatted =
    symbolInfo.tickSize >= 1
      ? points.toFixed(0)
      : points.toFixed(2);

  return `${formatted} (${sign}${pointsFormatted} pts)`;
}

/**
 * Calculates the dollar value from points for a given symbol
 * @param points - Number of points
 * @param symbol - The futures symbol code
 * @returns Dollar value
 */
export function pointsToDollars(points: number, symbol: string): number {
  const symbolInfo = getSymbolInfo(symbol);
  if (!symbolInfo) return 0;
  return points * symbolInfo.pointValue;
}

/**
 * Calculates points from dollar value for a given symbol
 * @param dollars - Dollar amount
 * @param symbol - The futures symbol code
 * @returns Points value
 */
export function dollarsToPoints(dollars: number, symbol: string): number {
  const symbolInfo = getSymbolInfo(symbol);
  if (!symbolInfo) return 0;
  return dollars / symbolInfo.pointValue;
}

/**
 * Validates if a string is a valid futures symbol
 * @param symbol - The symbol to validate
 * @returns True if valid, false otherwise
 */
export function isValidSymbol(symbol: string): symbol is FuturesSymbol {
  return FUTURES_SYMBOLS.some((s) => s.symbol === symbol);
}
