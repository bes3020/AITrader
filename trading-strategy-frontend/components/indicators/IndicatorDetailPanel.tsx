"use client";

import { useState } from "react";
import { Info, Plus, TrendingUp, TrendingDown, Minus, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Slider } from "@/components/ui/slider";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import { Alert, AlertDescription } from "@/components/ui/alert";
import type { IndicatorDefinition, IndicatorParameter } from "@/lib/indicator-definitions";

interface IndicatorDetailPanelProps {
  indicator: IndicatorDefinition;
  onAddCondition: (indicator: IndicatorDefinition, config?: Record<string, any>) => void;
}

export function IndicatorDetailPanel({
  indicator,
  onAddCondition,
}: IndicatorDetailPanelProps) {
  const [parameterValues, setParameterValues] = useState<Record<string, any>>(
    () => {
      const defaults: Record<string, any> = {};
      indicator.parameters.forEach((param) => {
        defaults[param.name] = param.default;
      });
      return defaults;
    }
  );

  const handleParameterChange = (paramName: string, value: any) => {
    setParameterValues((prev) => ({ ...prev, [paramName]: value }));
  };

  const handleAddCondition = () => {
    onAddCondition(indicator, parameterValues);
  };

  const renderParameter = (param: IndicatorParameter) => {
    const value = parameterValues[param.name] ?? param.default;

    if (param.type === "number") {
      return (
        <div key={param.name} className="space-y-2">
          <div className="flex items-center justify-between">
            <label className="text-sm font-medium">{param.label}</label>
            <span className="text-sm text-muted-foreground">{value}</span>
          </div>
          <Slider
            value={[value as number]}
            onValueChange={([newValue]) =>
              handleParameterChange(param.name, newValue)
            }
            min={param.min ?? 0}
            max={param.max ?? 100}
            step={param.step ?? 1}
            className="w-full"
          />
          <p className="text-xs text-muted-foreground">{param.description}</p>
        </div>
      );
    }

    if (param.type === "select" && param.options) {
      return (
        <div key={param.name} className="space-y-2">
          <label className="text-sm font-medium">{param.label}</label>
          <select
            value={value as string}
            onChange={(e) => handleParameterChange(param.name, e.target.value)}
            className="w-full px-3 py-2 border rounded-md"
          >
            {param.options.map((opt) => (
              <option key={opt.value.toString()} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
          <p className="text-xs text-muted-foreground">{param.description}</p>
        </div>
      );
    }

    if (param.type === "boolean") {
      return (
        <div key={param.name} className="flex items-center justify-between">
          <div>
            <label className="text-sm font-medium">{param.label}</label>
            <p className="text-xs text-muted-foreground">{param.description}</p>
          </div>
          <input
            type="checkbox"
            checked={value as boolean}
            onChange={(e) =>
              handleParameterChange(param.name, e.target.checked)
            }
            className="h-4 w-4"
          />
        </div>
      );
    }

    return null;
  };

  return (
    <div className="h-full overflow-y-auto">
      <div className="p-6 space-y-6">
        {/* Header */}
        <div>
          <div className="flex items-start justify-between mb-2">
            <div>
              <h2 className="text-2xl font-bold">{indicator.name}</h2>
              <p className="text-sm text-muted-foreground mt-1">
                {indicator.shortName}
              </p>
            </div>
            <Badge
              variant="outline"
              style={{
                borderColor: indicator.color,
                backgroundColor: `${indicator.color}10`,
                color: indicator.color,
              }}
            >
              {indicator.category.toUpperCase()}
            </Badge>
          </div>
          <p className="text-sm text-muted-foreground">{indicator.description}</p>
        </div>

        <Separator />

        {/* Formula */}
        <div>
          <h3 className="text-sm font-semibold mb-2 flex items-center gap-2">
            <Info className="h-4 w-4" />
            Formula
          </h3>
          <code className="text-xs bg-muted p-3 rounded-md block font-mono">
            {indicator.formula}
          </code>
        </div>

        {/* Value Range */}
        {indicator.range && (
          <div>
            <h3 className="text-sm font-semibold mb-2">Value Range</h3>
            <div className="flex items-center gap-2 text-sm">
              <Badge variant="outline">{indicator.range.min}</Badge>
              <span className="text-muted-foreground">to</span>
              <Badge variant="outline">{indicator.range.max}</Badge>
            </div>
          </div>
        )}

        {/* Parameters */}
        {indicator.parameters.length > 0 && (
          <div>
            <h3 className="text-sm font-semibold mb-3">Parameters</h3>
            <div className="space-y-4">
              {indicator.parameters.map(renderParameter)}
            </div>
          </div>
        )}

        {/* Interpretations */}
        <div>
          <h3 className="text-sm font-semibold mb-3">Interpretations</h3>
          <div className="space-y-2">
            <div className="flex items-start gap-2 p-2 bg-green-50 dark:bg-green-950 rounded-md">
              <TrendingUp className="h-4 w-4 text-green-600 dark:text-green-400 mt-0.5 shrink-0" />
              <div>
                <p className="text-xs font-medium text-green-900 dark:text-green-100">
                  Bullish
                </p>
                <p className="text-xs text-green-700 dark:text-green-300">
                  {indicator.interpretations.bullish}
                </p>
              </div>
            </div>

            <div className="flex items-start gap-2 p-2 bg-red-50 dark:bg-red-950 rounded-md">
              <TrendingDown className="h-4 w-4 text-red-600 dark:text-red-400 mt-0.5 shrink-0" />
              <div>
                <p className="text-xs font-medium text-red-900 dark:text-red-100">
                  Bearish
                </p>
                <p className="text-xs text-red-700 dark:text-red-300">
                  {indicator.interpretations.bearish}
                </p>
              </div>
            </div>

            <div className="flex items-start gap-2 p-2 bg-gray-50 dark:bg-gray-900 rounded-md">
              <Minus className="h-4 w-4 text-gray-600 dark:text-gray-400 mt-0.5 shrink-0" />
              <div>
                <p className="text-xs font-medium text-gray-900 dark:text-gray-100">
                  Neutral
                </p>
                <p className="text-xs text-gray-700 dark:text-gray-300">
                  {indicator.interpretations.neutral}
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Common Usage */}
        <div>
          <h3 className="text-sm font-semibold mb-2">Common Usage</h3>
          <div className="flex flex-wrap gap-2">
            {indicator.commonUsage.map((usage) => (
              <Badge key={usage} variant="secondary">
                {usage}
              </Badge>
            ))}
          </div>
        </div>

        {/* Examples */}
        {indicator.examples.length > 0 && (
          <div>
            <h3 className="text-sm font-semibold mb-2">Usage Examples</h3>
            <Accordion type="single" collapsible className="w-full">
              {indicator.examples.map((example, idx) => (
                <AccordionItem key={idx} value={`example-${idx}`}>
                  <AccordionTrigger className="text-sm">
                    {example.description}
                  </AccordionTrigger>
                  <AccordionContent>
                    <code className="text-xs bg-muted p-2 rounded block font-mono">
                      {example.condition}
                    </code>
                  </AccordionContent>
                </AccordionItem>
              ))}
            </Accordion>
          </div>
        )}

        {/* Warnings */}
        {indicator.warnings && indicator.warnings.length > 0 && (
          <Alert>
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              <ul className="list-disc list-inside space-y-1">
                {indicator.warnings.map((warning, idx) => (
                  <li key={idx} className="text-sm">
                    {warning}
                  </li>
                ))}
              </ul>
            </AlertDescription>
          </Alert>
        )}

        {/* Chart Type Info */}
        <div>
          <h3 className="text-sm font-semibold mb-2">Chart Display</h3>
          <Badge variant="outline">
            {indicator.chartType === "overlay"
              ? "Overlays on price chart"
              : indicator.chartType === "separate"
              ? "Displays in separate panel"
              : indicator.chartType === "histogram"
              ? "Histogram display"
              : "Band overlay"}
          </Badge>
        </div>

        {/* Add to Condition Button */}
        <div className="pt-4">
          <Button onClick={handleAddCondition} className="w-full" size="lg">
            <Plus className="mr-2 h-4 w-4" />
            Add to Condition
          </Button>
        </div>
      </div>
    </div>
  );
}
