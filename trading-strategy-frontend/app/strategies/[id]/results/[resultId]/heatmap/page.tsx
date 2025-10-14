"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, Loader2, AlertCircle, TrendingUp } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { useHeatmap } from "@/lib/hooks/useTrades";
import { formatCurrency } from "@/lib/utils";

interface HeatmapPageProps {
  params: Promise<{ id: string; resultId: string }>;
}

/**
 * Heatmap visualization page showing trade performance by dimension
 */
export default function HeatmapPage({ params }: HeatmapPageProps) {
  const router = useRouter();
  const [strategyId, setStrategyId] = useState<number>(0);
  const [resultId, setResultId] = useState<number>(0);
  const [selectedDimension, setSelectedDimension] = useState("hour");

  useEffect(() => {
    // Unwrap params Promise
    params.then((p) => {
      setStrategyId(parseInt(p.id, 10));
      setResultId(parseInt(p.resultId, 10));
    });
  }, [params]);

  const { data, loading, error, changeDimension } = useHeatmap(
    strategyId,
    resultId,
    selectedDimension
  );

  const handleDimensionChange = (dimension: string) => {
    setSelectedDimension(dimension);
    changeDimension(dimension);
  };

  if (strategyId === 0 || resultId === 0) {
    return null; // Loading params
  }

  if (loading && !data) {
    return (
      <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center space-y-4">
              <Loader2 className="h-12 w-12 animate-spin mx-auto text-primary" />
              <p className="text-lg font-medium">Loading heatmap...</p>
            </div>
          </div>
        </div>
      </main>
    );
  }

  if (error) {
    return (
      <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center space-y-4 max-w-md">
              <AlertCircle className="h-12 w-12 mx-auto text-destructive" />
              <h2 className="text-2xl font-bold">Error Loading Heatmap</h2>
              <p className="text-muted-foreground">{error}</p>
              <Button onClick={() => router.back()} variant="outline">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Go Back
              </Button>
            </div>
          </div>
        </div>
      </main>
    );
  }

  if (!data || data.cells.length === 0) {
    return (
      <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
          <Button
            onClick={() =>
              router.push(`/strategies/${strategyId}/results/${resultId}/trades`)
            }
            variant="ghost"
            className="mb-4"
          >
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Trades
          </Button>

          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center space-y-4">
              <AlertCircle className="h-12 w-12 mx-auto text-muted-foreground" />
              <h2 className="text-2xl font-bold">No Data Available</h2>
              <p className="text-muted-foreground">
                Not enough trades to generate heatmap
              </p>
            </div>
          </div>
        </div>
      </main>
    );
  }

  // Calculate min and max values for color scaling
  const values = data.cells.map((cell) => cell.value);
  const minValue = Math.min(...values);
  const maxValue = Math.max(...values);
  const avgValue = values.reduce((sum, val) => sum + val, 0) / values.length;

  return (
    <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
      <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8">
          <Button
            onClick={() =>
              router.push(`/strategies/${strategyId}/results/${resultId}/trades`)
            }
            variant="ghost"
            className="mb-4"
          >
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Trades
          </Button>

          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-3xl font-bold mb-2">Performance Heatmap</h1>
              <p className="text-muted-foreground">
                Visualize trade performance across {data.label}
              </p>
            </div>

            {/* Dimension Selector */}
            <Select
              value={selectedDimension}
              onValueChange={handleDimensionChange}
            >
              <SelectTrigger className="w-[200px]">
                <SelectValue placeholder="Select dimension" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="hour">Hour of Day</SelectItem>
                <SelectItem value="day">Day of Week</SelectItem>
                <SelectItem value="condition">Market Condition</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Summary Stats */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium">Best {data.dimension}</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-profit">
                {data.cells.reduce((best, cell) =>
                  cell.value > best.value ? cell : best
                ).label}
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                Avg: {formatCurrency(
                  data.cells.reduce((best, cell) =>
                    cell.value > best.value ? cell : best
                  ).value
                )}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium">Worst {data.dimension}</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-loss">
                {data.cells.reduce((worst, cell) =>
                  cell.value < worst.value ? cell : worst
                ).label}
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                Avg: {formatCurrency(
                  data.cells.reduce((worst, cell) =>
                    cell.value < worst.value ? cell : worst
                  ).value
                )}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium">Average Performance</CardTitle>
            </CardHeader>
            <CardContent>
              <div
                className={`text-2xl font-bold ${
                  avgValue >= 0 ? "text-profit" : "text-loss"
                }`}
              >
                {formatCurrency(avgValue)}
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                Across all {data.dimension}s
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium">Total Categories</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{data.cells.length}</div>
              <p className="text-xs text-muted-foreground mt-1">
                {data.dimension} categories
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Heatmap Grid */}
        <Card>
          <CardHeader>
            <CardTitle>{data.label}</CardTitle>
            <p className="text-sm text-muted-foreground">
              Color intensity represents average P&L per trade
            </p>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
              {data.cells.map((cell) => {
                // Calculate color intensity based on value
                const isPositive = cell.value >= 0;
                const normalizedValue = isPositive
                  ? (cell.value / maxValue) * 100
                  : (Math.abs(cell.value) / Math.abs(minValue)) * 100;

                const opacity = Math.min(Math.max(normalizedValue / 100, 0.2), 1);

                return (
                  <div
                    key={cell.label}
                    className="relative p-6 rounded-lg border transition-all hover:scale-105 cursor-pointer"
                    style={{
                      backgroundColor: isPositive
                        ? `rgba(34, 197, 94, ${opacity * 0.3})`
                        : `rgba(239, 68, 68, ${opacity * 0.3})`,
                      borderColor: isPositive
                        ? `rgba(34, 197, 94, ${opacity})`
                        : `rgba(239, 68, 68, ${opacity})`,
                    }}
                    title={cell.tooltip || undefined}
                  >
                    {/* Label */}
                    <div className="text-center mb-3">
                      <p className="text-lg font-bold">{cell.label}</p>
                      <p className="text-xs text-muted-foreground">
                        {cell.count} trade{cell.count !== 1 ? "s" : ""}
                      </p>
                    </div>

                    {/* Value */}
                    <div className="text-center">
                      <p
                        className={`text-xl font-bold ${
                          isPositive ? "text-profit" : "text-loss"
                        }`}
                      >
                        {formatCurrency(cell.value)}
                      </p>
                      <p className="text-xs text-muted-foreground mt-1">
                        Avg P&L
                      </p>
                    </div>

                    {/* Performance Indicator */}
                    {cell.value > avgValue && (
                      <div className="absolute top-2 right-2">
                        <Badge variant="profit" className="text-xs">
                          <TrendingUp className="mr-1 h-3 w-3" />
                          Above Avg
                        </Badge>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>

            {/* Legend */}
            <div className="mt-8 pt-6 border-t">
              <p className="text-sm font-medium mb-4">Color Legend</p>
              <div className="flex items-center gap-8">
                <div className="flex items-center gap-3">
                  <div className="w-12 h-12 rounded border-2 border-loss bg-loss/30" />
                  <div>
                    <p className="text-sm font-medium">Negative</p>
                    <p className="text-xs text-muted-foreground">
                      Below average performance
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <div className="w-12 h-12 rounded border-2 border-muted bg-muted/30" />
                  <div>
                    <p className="text-sm font-medium">Neutral</p>
                    <p className="text-xs text-muted-foreground">
                      Around average performance
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <div className="w-12 h-12 rounded border-2 border-profit bg-profit/30" />
                  <div>
                    <p className="text-sm font-medium">Positive</p>
                    <p className="text-xs text-muted-foreground">
                      Above average performance
                    </p>
                  </div>
                </div>
              </div>
              <p className="text-xs text-muted-foreground mt-4">
                * Darker/more opaque colors indicate stronger performance (positive
                or negative)
              </p>
            </div>
          </CardContent>
        </Card>

        {loading && (
          <div className="fixed bottom-4 right-4 bg-card border rounded-lg p-4 shadow-lg">
            <div className="flex items-center gap-3">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span className="text-sm">Loading...</span>
            </div>
          </div>
        )}
      </div>
    </main>
  );
}
