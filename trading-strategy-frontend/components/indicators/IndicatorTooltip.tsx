"use client";

import { Info } from "lucide-react";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { Badge } from "@/components/ui/badge";
import type { IndicatorDefinition } from "@/lib/indicator-definitions";

interface IndicatorTooltipProps {
  indicator: IndicatorDefinition;
  children?: React.ReactNode;
  side?: "top" | "right" | "bottom" | "left";
  align?: "start" | "center" | "end";
}

export function IndicatorTooltip({
  indicator,
  children,
  side = "right",
  align = "start",
}: IndicatorTooltipProps) {
  return (
    <TooltipProvider delayDuration={300}>
      <Tooltip>
        <TooltipTrigger asChild>
          {children || (
            <button
              type="button"
              className="inline-flex items-center justify-center w-4 h-4 rounded-full hover:bg-muted transition-colors"
            >
              <Info className="h-3 w-3 text-muted-foreground" />
            </button>
          )}
        </TooltipTrigger>
        <TooltipContent side={side} align={align} className="max-w-sm p-4">
          <div className="space-y-2">
            {/* Header */}
            <div className="flex items-start justify-between gap-2">
              <div>
                <h4 className="font-semibold text-sm">{indicator.shortName}</h4>
                <p className="text-xs text-muted-foreground">
                  {indicator.name}
                </p>
              </div>
              <Badge
                variant="outline"
                className="text-xs shrink-0"
                style={{
                  borderColor: indicator.color,
                  color: indicator.color,
                }}
              >
                {indicator.category}
              </Badge>
            </div>

            {/* Description */}
            <p className="text-xs leading-relaxed">{indicator.description}</p>

            {/* Range */}
            {indicator.range && (
              <div className="flex items-center gap-2 text-xs">
                <span className="text-muted-foreground">Range:</span>
                <Badge variant="secondary" className="text-xs">
                  {indicator.range.min} - {indicator.range.max}
                </Badge>
              </div>
            )}

            {/* Common Usage */}
            {indicator.commonUsage.length > 0 && (
              <div>
                <p className="text-xs font-medium mb-1">Best for:</p>
                <div className="flex flex-wrap gap-1">
                  {indicator.commonUsage.slice(0, 3).map((usage) => (
                    <Badge
                      key={usage}
                      variant="secondary"
                      className="text-xs"
                    >
                      {usage}
                    </Badge>
                  ))}
                </div>
              </div>
            )}

            {/* Quick interpretation */}
            <div className="pt-2 border-t">
              <div className="grid grid-cols-3 gap-2 text-xs">
                <div className="text-center">
                  <div className="text-green-600 dark:text-green-400 font-medium">
                    Bullish
                  </div>
                  <div className="text-muted-foreground text-[10px] leading-tight mt-0.5">
                    {indicator.interpretations.bullish.split(" ").slice(0, 5).join(" ")}...
                  </div>
                </div>
                <div className="text-center">
                  <div className="text-gray-600 dark:text-gray-400 font-medium">
                    Neutral
                  </div>
                  <div className="text-muted-foreground text-[10px] leading-tight mt-0.5">
                    {indicator.interpretations.neutral.split(" ").slice(0, 5).join(" ")}...
                  </div>
                </div>
                <div className="text-center">
                  <div className="text-red-600 dark:text-red-400 font-medium">
                    Bearish
                  </div>
                  <div className="text-muted-foreground text-[10px] leading-tight mt-0.5">
                    {indicator.interpretations.bearish.split(" ").slice(0, 5).join(" ")}...
                  </div>
                </div>
              </div>
            </div>
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

/**
 * Simple inline tooltip for quick indicator info
 */
export function IndicatorQuickTooltip({
  indicator,
}: {
  indicator: IndicatorDefinition;
}) {
  return (
    <TooltipProvider delayDuration={200}>
      <Tooltip>
        <TooltipTrigger asChild>
          <button
            type="button"
            className="inline-flex items-center justify-center"
          >
            <Info className="h-3 w-3 text-muted-foreground hover:text-foreground transition-colors" />
          </button>
        </TooltipTrigger>
        <TooltipContent side="top" className="max-w-xs">
          <p className="text-xs">{indicator.description}</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}
