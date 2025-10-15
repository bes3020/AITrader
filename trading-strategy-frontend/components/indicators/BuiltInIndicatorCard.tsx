"use client";

import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Plus, Copy, TrendingUp, Activity, GitBranch, BarChart3, Waves } from "lucide-react";
import type { BuiltInIndicator } from "@/lib/types/indicator";
import { CATEGORY_COLORS } from "@/lib/types/indicator";

interface BuiltInIndicatorCardProps {
  indicator: BuiltInIndicator;
  onClone: (indicator: BuiltInIndicator) => void;
  onUse: (indicator: BuiltInIndicator) => void;
}

const ICON_MAP: Record<string, any> = {
  EMA: TrendingUp,
  SMA: TrendingUp,
  RSI: Activity,
  BollingerBands: GitBranch,
  MACD: BarChart3,
  ATR: Waves,
  ADX: TrendingUp,
  Stochastic: Activity,
};

export function BuiltInIndicatorCard({ indicator, onClone, onUse }: BuiltInIndicatorCardProps) {
  const Icon = ICON_MAP[indicator.type] || Activity;
  const categoryColor = CATEGORY_COLORS[indicator.category] || "#6b7280";

  return (
    <Card className="p-4 hover:shadow-md transition-shadow">
      {/* Header */}
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-3">
          <div
            className="p-2 rounded-lg"
            style={{ backgroundColor: categoryColor + "20" }}
          >
            <Icon className="h-5 w-5" style={{ color: categoryColor }} />
          </div>
          <div>
            <h3 className="font-semibold">{indicator.displayName}</h3>
            <Badge variant="outline" className="text-xs mt-1">
              {indicator.category}
            </Badge>
          </div>
        </div>
      </div>

      {/* Description */}
      <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
        {indicator.description}
      </p>

      {/* Parameters */}
      <div className="mb-3">
        <p className="text-xs font-medium text-muted-foreground mb-1">Parameters:</p>
        <div className="flex flex-wrap gap-1">
          {indicator.parameters.map((param) => (
            <Badge key={param.name} variant="secondary" className="text-xs">
              {param.name}: {param.defaultValue}
            </Badge>
          ))}
        </div>
      </div>

      {/* Common Presets */}
      {indicator.commonPresets && indicator.commonPresets.length > 0 && (
        <div className="mb-3">
          <p className="text-xs font-medium text-muted-foreground mb-1">Common Presets:</p>
          <div className="flex flex-wrap gap-1">
            {indicator.commonPresets.map((preset, idx) => (
              <Badge key={idx} variant="outline" className="text-xs">
                {preset.name}
              </Badge>
            ))}
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="flex gap-2">
        <Button
          variant="outline"
          size="sm"
          className="flex-1"
          onClick={() => onClone(indicator)}
        >
          <Copy className="h-4 w-4 mr-1" />
          Clone & Customize
        </Button>
        <Button
          variant="default"
          size="sm"
          className="flex-1"
          onClick={() => onUse(indicator)}
        >
          <Plus className="h-4 w-4 mr-1" />
          Use
        </Button>
      </div>
    </Card>
  );
}
