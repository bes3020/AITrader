"use client";

import { useState } from "react";
import { X, Plus, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { IndicatorQuickTooltip } from "@/components/indicators/IndicatorTooltip";
import {
  INDICATOR_DEFINITIONS,
  getIndicator,
  type IndicatorDefinition,
} from "@/lib/indicator-definitions";

export interface Condition {
  id: string;
  indicator: string;
  operator: string;
  value: string;
}

interface VisualConditionBuilderProps {
  conditions: Condition[];
  onChange: (conditions: Condition[]) => void;
}

const OPERATORS = [
  { value: ">", label: ">" },
  { value: "<", label: "<" },
  { value: ">=", label: ">=" },
  { value: "<=", label: "<=" },
  { value: "==", label: "==" },
  { value: "crosses_above", label: "Crosses Above" },
  { value: "crosses_below", label: "Crosses Below" },
];

// Multi-value indicators with their components
const MULTI_VALUE_INDICATORS: Record<string, { label: string; value: string }[]> = {
  "Bollinger Bands": [
    { label: "BB Upper", value: "bb_upper" },
    { label: "BB Middle", value: "bb_middle" },
    { label: "BB Lower", value: "bb_lower" },
  ],
  MACD: [
    { label: "MACD Line", value: "macd_line" },
    { label: "MACD Signal", value: "macd_signal" },
    { label: "MACD Histogram", value: "macd_histogram" },
  ],
  Stochastic: [
    { label: "Stoch %K", value: "stoch_k" },
    { label: "Stoch %D", value: "stoch_d" },
  ],
  Ichimoku: [
    { label: "Tenkan", value: "ichimoku_tenkan" },
    { label: "Kijun", value: "ichimoku_kijun" },
    { label: "Senkou A", value: "ichimoku_senkou_a" },
    { label: "Senkou B", value: "ichimoku_senkou_b" },
    { label: "Chikou", value: "ichimoku_chikou" },
  ],
};

// Group indicators by category for dropdown
const INDICATOR_GROUPS = {
  "Moving Averages": ["ema9", "ema20", "ema50"],
  "Bollinger Bands": ["bb_upper", "bb_middle", "bb_lower"],
  MACD: ["macd_line", "macd_signal", "macd_histogram"],
  Stochastic: ["stoch_k", "stoch_d"],
  "Single Indicators": [
    "rsi",
    "adx",
    "cci",
    "williams_r",
    "atr",
    "psar",
    "obv",
  ],
  Ichimoku: [
    "ichimoku_tenkan",
    "ichimoku_kijun",
    "ichimoku_senkou_a",
    "ichimoku_senkou_b",
    "ichimoku_chikou",
  ],
  "Price & Volume": [
    "price",
    "open",
    "high",
    "low",
    "volume",
    "vwap",
    "avgVolume20",
  ],
  "Previous Day": ["prev_day_high", "prev_day_low"],
  Other: ["time"],
};

// Common value suggestions based on indicator
const VALUE_SUGGESTIONS: Record<string, string[]> = {
  rsi: ["30", "70", "50"],
  stoch_k: ["20", "80", "50"],
  stoch_d: ["20", "80", "50"],
  adx: ["25", "20", "40"],
  cci: ["100", "-100", "0"],
  williams_r: ["-20", "-80", "-50"],
  volume: ["1.5x_avgVolume20", "2x_avgVolume20", "avgVolume20"],
  price: ["vwap", "ema9", "ema20", "ema50", "bb_upper", "bb_lower"],
  macd_histogram: ["0"],
  macd_line: ["macd_signal", "0"],
};

export function VisualConditionBuilder({
  conditions,
  onChange,
}: VisualConditionBuilderProps) {
  const [warnings, setWarnings] = useState<string[]>([]);

  const addCondition = () => {
    const newCondition: Condition = {
      id: `condition-${Date.now()}`,
      indicator: "",
      operator: ">",
      value: "",
    };
    onChange([...conditions, newCondition]);
  };

  const removeCondition = (id: string) => {
    onChange(conditions.filter((c) => c.id !== id));
  };

  const updateCondition = (
    id: string,
    field: keyof Condition,
    value: string
  ) => {
    onChange(
      conditions.map((c) => (c.id === id ? { ...c, [field]: value } : c))
    );

    // Check for warnings
    checkForWarnings([...conditions.map((c) => (c.id === id ? { ...c, [field]: value } : c))]);
  };

  const checkForWarnings = (conds: Condition[]) => {
    const newWarnings: string[] = [];

    // Check for conflicting conditions
    const indicators = conds.map((c) => c.indicator).filter(Boolean);
    const hasBothTrend = indicators.includes("ema9") && indicators.includes("ema50");
    if (hasBothTrend) {
      newWarnings.push("Using multiple trend indicators may cause conflicts");
    }

    // Check for oscillator overbought/oversold conflicts
    const hasRSI = conds.find(
      (c) =>
        c.indicator === "rsi" &&
        (c.value === "70" || c.value === "30") &&
        c.operator === ">"
    );
    if (hasRSI) {
      newWarnings.push("RSI overbought - consider mean reversion strategy");
    }

    // Check for high volume requirements
    const hasHighVolume = conds.find(
      (c) => c.indicator === "volume" && c.value.includes("2x")
    );
    if (hasHighVolume) {
      newWarnings.push("2x volume requirement may result in very few signals");
    }

    setWarnings(newWarnings);
  };

  const renderValueInput = (condition: Condition) => {
    const suggestions = VALUE_SUGGESTIONS[condition.indicator] || [];

    if (suggestions.length > 0) {
      return (
        <Select
          value={condition.value}
          onValueChange={(value) =>
            updateCondition(condition.id, "value", value)
          }
        >
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Select or type value..." />
          </SelectTrigger>
          <SelectContent>
            {suggestions.map((suggestion) => (
              <SelectItem key={suggestion} value={suggestion}>
                {suggestion}
              </SelectItem>
            ))}
            <SelectItem value="custom">Custom Value...</SelectItem>
          </SelectContent>
        </Select>
      );
    }

    return (
      <Input
        type="text"
        placeholder="Value"
        value={condition.value}
        onChange={(e) =>
          updateCondition(condition.id, "value", e.target.value)
        }
      />
    );
  };

  return (
    <div className="space-y-4">
      {/* Conditions List */}
      {conditions.map((condition, index) => {
        const indicatorDef = condition.indicator
          ? getIndicator(condition.indicator)
          : null;

        return (
          <Card key={condition.id}>
            <CardContent className="pt-6">
              <div className="flex items-start gap-3">
                {/* Condition Number */}
                <div className="flex items-center justify-center h-10 w-10 rounded-full bg-primary/10 text-primary font-semibold shrink-0">
                  {index + 1}
                </div>

                {/* Condition Builder */}
                <div className="flex-1 grid grid-cols-1 md:grid-cols-3 gap-3">
                  {/* Indicator Selector */}
                  <div className="space-y-1">
                    <label className="text-xs font-medium text-muted-foreground flex items-center gap-1">
                      Indicator
                      {indicatorDef && <IndicatorQuickTooltip indicator={indicatorDef} />}
                    </label>
                    <Select
                      value={condition.indicator}
                      onValueChange={(value) =>
                        updateCondition(condition.id, "indicator", value)
                      }
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select indicator..." />
                      </SelectTrigger>
                      <SelectContent>
                        {Object.entries(INDICATOR_GROUPS).map(
                          ([groupName, indicators]) => (
                            <div key={groupName}>
                              <div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground">
                                {groupName}
                              </div>
                              {indicators.map((indId) => {
                                const ind = getIndicator(indId);
                                if (!ind) return null;
                                return (
                                  <SelectItem key={indId} value={indId}>
                                    <div className="flex items-center gap-2">
                                      <div
                                        className="h-2 w-2 rounded-full"
                                        style={{ backgroundColor: ind.color }}
                                      />
                                      {ind.shortName}
                                    </div>
                                  </SelectItem>
                                );
                              })}
                            </div>
                          )
                        )}
                      </SelectContent>
                    </Select>
                    {indicatorDef && (
                      <Badge
                        variant="outline"
                        className="text-xs"
                        style={{
                          borderColor: indicatorDef.color,
                          color: indicatorDef.color,
                        }}
                      >
                        {indicatorDef.category}
                      </Badge>
                    )}
                  </div>

                  {/* Operator Selector */}
                  <div className="space-y-1">
                    <label className="text-xs font-medium text-muted-foreground">
                      Operator
                    </label>
                    <Select
                      value={condition.operator}
                      onValueChange={(value) =>
                        updateCondition(condition.id, "operator", value)
                      }
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {OPERATORS.map((op) => (
                          <SelectItem key={op.value} value={op.value}>
                            {op.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  {/* Value Input */}
                  <div className="space-y-1">
                    <label className="text-xs font-medium text-muted-foreground">
                      Value
                    </label>
                    {renderValueInput(condition)}
                  </div>
                </div>

                {/* Remove Button */}
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => removeCondition(condition.id)}
                  className="shrink-0"
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>

              {/* Preview */}
              {condition.indicator && condition.operator && condition.value && (
                <div className="mt-3 p-2 bg-muted rounded-md">
                  <code className="text-xs font-mono">
                    {condition.indicator} {condition.operator} {condition.value}
                  </code>
                </div>
              )}
            </CardContent>
          </Card>
        );
      })}

      {/* Add Condition Button */}
      <Button
        onClick={addCondition}
        variant="outline"
        className="w-full border-dashed"
      >
        <Plus className="mr-2 h-4 w-4" />
        Add Condition
      </Button>

      {/* Warnings */}
      {warnings.length > 0 && (
        <Alert>
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>
            <ul className="list-disc list-inside space-y-1">
              {warnings.map((warning, idx) => (
                <li key={idx} className="text-sm">
                  {warning}
                </li>
              ))}
            </ul>
          </AlertDescription>
        </Alert>
      )}

      {/* Logic Note */}
      {conditions.length > 1 && (
        <p className="text-xs text-muted-foreground text-center">
          All conditions must be true (AND logic)
        </p>
      )}
    </div>
  );
}
