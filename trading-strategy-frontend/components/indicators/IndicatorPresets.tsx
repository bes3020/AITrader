"use client";

import { Zap, TrendingUp, Target, Activity } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { INDICATOR_PRESETS, getIndicator, type IndicatorPreset } from "@/lib/indicator-definitions";

interface IndicatorPresetsProps {
  onSelectPreset: (preset: IndicatorPreset) => void;
}

const PRESET_ICONS: Record<string, React.ReactNode> = {
  scalping: <Zap className="h-5 w-5" />,
  "day-trading": <Activity className="h-5 w-5" />,
  "swing-trading": <TrendingUp className="h-5 w-5" />,
  "mean-reversion": <Target className="h-5 w-5" />,
};

const PRESET_COLORS: Record<string, string> = {
  scalping: "#f59e0b",
  "day-trading": "#3b82f6",
  "swing-trading": "#8b5cf6",
  "mean-reversion": "#10b981",
};

export function IndicatorPresets({ onSelectPreset }: IndicatorPresetsProps) {
  return (
    <div className="space-y-4">
      <div>
        <h3 className="text-lg font-semibold mb-1">Quick Start Presets</h3>
        <p className="text-sm text-muted-foreground">
          Pre-configured indicator sets for common trading strategies
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {INDICATOR_PRESETS.map((preset) => {
          const color = PRESET_COLORS[preset.category];

          return (
            <Card
              key={preset.id}
              className="hover:border-primary hover:shadow-sm transition-all cursor-pointer"
              onClick={() => onSelectPreset(preset)}
            >
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div
                    className="p-2 rounded-lg mb-2"
                    style={{ backgroundColor: `${color}20` }}
                  >
                    <div style={{ color }}>
                      {PRESET_ICONS[preset.category]}
                    </div>
                  </div>
                  <Badge
                    variant="outline"
                    style={{ borderColor: color, color }}
                  >
                    {preset.category.replace("-", " ")}
                  </Badge>
                </div>
                <CardTitle className="text-lg">{preset.name}</CardTitle>
                <CardDescription className="text-sm">
                  {preset.description}
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {/* Indicators List */}
                  <div>
                    <p className="text-xs font-medium text-muted-foreground mb-2">
                      Includes {preset.indicators.length} indicators:
                    </p>
                    <div className="flex flex-wrap gap-1">
                      {preset.indicators.map((indId) => {
                        const ind = getIndicator(indId);
                        if (!ind) return null;
                        return (
                          <Badge
                            key={indId}
                            variant="secondary"
                            className="text-xs"
                          >
                            {ind.shortName}
                          </Badge>
                        );
                      })}
                    </div>
                  </div>

                  {/* Apply Button */}
                  <Button
                    onClick={(e) => {
                      e.stopPropagation();
                      onSelectPreset(preset);
                    }}
                    size="sm"
                    className="w-full"
                    style={{ backgroundColor: color }}
                  >
                    Apply Preset
                  </Button>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Educational Note */}
      <div className="p-4 bg-muted/50 rounded-lg">
        <p className="text-xs text-muted-foreground">
          <strong>Tip:</strong> Presets provide a starting point. Customize
          conditions based on your specific symbol and timeframe for best results.
        </p>
      </div>
    </div>
  );
}
