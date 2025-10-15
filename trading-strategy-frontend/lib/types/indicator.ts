/**
 * TypeScript types for custom indicator system.
 * Matches backend C# models.
 */

export interface IndicatorParameter {
  name: string;
  type: "int" | "decimal" | "string" | "bool";
  defaultValue: string;
  minValue?: number;
  maxValue?: number;
  required: boolean;
  description?: string;
}

export interface IndicatorPreset {
  name: string;
  parameters: string; // JSON string
}

export interface BuiltInIndicator {
  type: string;
  displayName: string;
  description: string;
  category: "Trend" | "Momentum" | "Volatility" | "Volume";
  parameters: IndicatorParameter[];
  commonPresets?: IndicatorPreset[];
}

export interface CustomIndicator {
  id: number;
  userId: number;
  name: string;
  displayName: string;
  type: string;
  parameters: string; // JSON string
  formula?: string;
  description?: string;
  isPublic: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateIndicatorRequest {
  name: string;
  displayName: string;
  type: string;
  parameters: string; // JSON string
  formula?: string;
  description?: string;
  isPublic?: boolean;
}

export interface UpdateIndicatorRequest {
  displayName?: string;
  parameters?: string;
  formula?: string;
  description?: string;
  isPublic?: boolean;
}

export interface CalculateIndicatorRequest {
  symbol: string;
  startDate: string;
  endDate: string;
}

export interface CalculateIndicatorResponse {
  indicatorId: number;
  indicatorName: string;
  type: string;
  values: number[];
  timestamps: string[];
  additionalSeries?: Record<string, number[]>;
  parameters: string;
}

// Helper type for parsed parameters
export type IndicatorParameters = Record<string, string | number | boolean>;

// Category colors
export const CATEGORY_COLORS: Record<string, string> = {
  Trend: "#3b82f6", // blue
  Momentum: "#8b5cf6", // purple
  Volatility: "#f59e0b", // amber
  Volume: "#10b981", // green
};

// Type icons (use with lucide-react)
export const TYPE_ICONS: Record<string, string> = {
  EMA: "TrendingUp",
  SMA: "TrendingUp",
  RSI: "Activity",
  BollingerBands: "GitBranch",
  MACD: "BarChart3",
  ATR: "Waves",
  ADX: "TrendingUp",
  Stochastic: "Activity",
  Custom: "Code",
};
