"use client";

import { useEffect, useRef, useState } from "react";
import {
  createChart,
  ColorType,
  IChartApi,
  ISeriesApi,
  CandlestickSeriesPartialOptions,
  LineSeriesPartialOptions,
  Time,
} from "lightweight-charts";
import { AlertCircle, Eye, EyeOff } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import type { BarData, TradeResult } from "@/lib/types";

interface TradeDetailChartProps {
  /**
   * Historical price data (setup + trade bars)
   */
  chartData: BarData[];

  /**
   * Trade result data
   */
  trade: TradeResult;

  /**
   * Trade direction
   */
  direction: "long" | "short";

  /**
   * Stop loss price
   */
  stopPrice?: number;

  /**
   * Take profit price
   */
  targetPrice?: number;

  /**
   * Optional indicator series data
   */
  indicatorSeries?: Record<string, number[]>;

  /**
   * Optional className for styling
   */
  className?: string;
}

/**
 * Interactive chart component for trade detail page with indicators
 */
export function TradeDetailChart({
  chartData,
  trade,
  direction,
  stopPrice,
  targetPrice,
  indicatorSeries,
  className,
}: TradeDetailChartProps) {
  const chartContainerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const candlestickSeriesRef = useRef<ISeriesApi<"Candlestick"> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [visibleIndicators, setVisibleIndicators] = useState<Set<string>>(
    new Set()
  );

  // Toggle indicator visibility
  const toggleIndicator = (indicator: string) => {
    setVisibleIndicators((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(indicator)) {
        newSet.delete(indicator);
      } else {
        newSet.add(indicator);
      }
      return newSet;
    });
  };

  useEffect(() => {
    if (!chartContainerRef.current || !chartData || chartData.length === 0)
      return;

    try {
      // Create chart instance
      const chart = createChart(chartContainerRef.current, {
        width: chartContainerRef.current.clientWidth,
        height: 500,
        layout: {
          background: { type: ColorType.Solid, color: "transparent" },
          textColor: "#6B7280",
        },
        grid: {
          vertLines: { color: "#E5E7EB" },
          horzLines: { color: "#E5E7EB" },
        },
        timeScale: {
          borderColor: "#E5E7EB",
          timeVisible: true,
          secondsVisible: false,
        },
        rightPriceScale: {
          borderColor: "#E5E7EB",
        },
        crosshair: {
          mode: 1, // Normal crosshair
          vertLine: {
            width: 1,
            color: "#9CA3AF",
            style: 3, // Dashed
          },
          horzLine: {
            width: 1,
            color: "#9CA3AF",
            style: 3, // Dashed
          },
        },
      });

      chartRef.current = chart;

      // Determine if trade was profitable
      const isProfitable =
        direction === "long"
          ? (trade.exitPrice ?? 0) > trade.entryPrice
          : (trade.exitPrice ?? 0) < trade.entryPrice;

      // Create candlestick series
      const candlestickOptions: CandlestickSeriesPartialOptions = {
        upColor: "#22C55E",
        downColor: "#EF4444",
        borderUpColor: "#22C55E",
        borderDownColor: "#EF4444",
        wickUpColor: "#22C55E",
        wickDownColor: "#EF4444",
      };

      const candlestickSeries = chart.addCandlestickSeries(candlestickOptions);
      candlestickSeriesRef.current = candlestickSeries;

      // Transform bars data to lightweight-charts format
      const transformedData = chartData.map((bar) => ({
        time: Math.floor(new Date(bar.timestamp).getTime() / 1000) as Time,
        open: bar.open,
        high: bar.high,
        low: bar.low,
        close: bar.close,
      }));

      candlestickSeries.setData(transformedData);

      // Add entry and exit markers
      const entryTimeUnix = Math.floor(
        new Date(trade.entryTime).getTime() / 1000
      ) as Time;

      const markers: any[] = [
        {
          time: entryTimeUnix,
          position: direction === "long" ? "belowBar" : "aboveBar",
          color: "#3B82F6",
          shape: direction === "long" ? "arrowUp" : "arrowDown",
          text: `Entry: ${trade.entryPrice.toFixed(2)}`,
          size: 2,
        },
      ];

      if (trade.exitTime && trade.exitPrice) {
        const exitTimeUnix = Math.floor(
          new Date(trade.exitTime).getTime() / 1000
        ) as Time;
        markers.push({
          time: exitTimeUnix,
          position: direction === "long" ? "aboveBar" : "belowBar",
          color: isProfitable ? "#22C55E" : "#EF4444",
          shape: direction === "long" ? "arrowDown" : "arrowUp",
          text: `Exit: ${trade.exitPrice.toFixed(2)}`,
          size: 2,
        });
      }

      candlestickSeries.setMarkers(markers);

      // Add horizontal price lines
      const priceLines: any[] = [];

      // Entry price line (blue)
      priceLines.push(
        candlestickSeries.createPriceLine({
          price: trade.entryPrice,
          color: "#3B82F6",
          lineWidth: 2,
          lineStyle: 0, // Solid
          axisLabelVisible: true,
          title: "Entry",
        })
      );

      // Stop loss line (red)
      if (stopPrice) {
        priceLines.push(
          candlestickSeries.createPriceLine({
            price: stopPrice,
            color: "#EF4444",
            lineWidth: 2,
            lineStyle: 2, // Dashed
            axisLabelVisible: true,
            title: "Stop",
          })
        );
      }

      // Target price line (green)
      if (targetPrice) {
        priceLines.push(
          candlestickSeries.createPriceLine({
            price: targetPrice,
            color: "#22C55E",
            lineWidth: 2,
            lineStyle: 2, // Dashed
            axisLabelVisible: true,
            title: "Target",
          })
        );
      }

      // Add indicator series if provided
      const indicatorSeriesRefs: Map<string, ISeriesApi<"Line">> = new Map();

      if (indicatorSeries && transformedData.length > 0) {
        Object.entries(indicatorSeries).forEach(([name, values]) => {
          if (!visibleIndicators.has(name) || values.length === 0) return;

          const lineOptions: LineSeriesPartialOptions = {
            color: getIndicatorColor(name),
            lineWidth: 2,
            title: name.toUpperCase(),
          };

          const lineSeries = chart.addLineSeries(lineOptions);

          // Transform indicator data
          const indicatorData = values.map((value, idx) => ({
            time: transformedData[idx]?.time,
            value,
          })).filter((d) => d.time !== undefined);

          lineSeries.setData(indicatorData as any);
          indicatorSeriesRefs.set(name, lineSeries);
        });
      }

      // Fit content to show all bars
      chart.timeScale().fitContent();

      // Handle window resize
      const handleResize = () => {
        if (chartContainerRef.current && chartRef.current) {
          chartRef.current.applyOptions({
            width: chartContainerRef.current.clientWidth,
          });
        }
      };

      window.addEventListener("resize", handleResize);

      // Cleanup function
      return () => {
        window.removeEventListener("resize", handleResize);

        // Remove price lines
        priceLines.forEach((line) => {
          if (candlestickSeriesRef.current) {
            candlestickSeriesRef.current.removePriceLine(line);
          }
        });

        // Remove indicator series
        indicatorSeriesRefs.forEach((series) => {
          if (chartRef.current) {
            chartRef.current.removeSeries(series);
          }
        });

        if (chartRef.current) {
          chartRef.current.remove();
          chartRef.current = null;
          candlestickSeriesRef.current = null;
        }
      };
    } catch (err) {
      console.error("[TradeDetailChart] Failed to create chart:", err);
      setError(err instanceof Error ? err.message : "Unknown error");
    }
  }, [
    chartData,
    trade,
    direction,
    stopPrice,
    targetPrice,
    indicatorSeries,
    visibleIndicators,
  ]);

  if (error) {
    return (
      <Card className={className}>
        <CardContent className="p-6">
          <div className="flex items-center justify-center h-[500px] border rounded-lg bg-destructive/5 border-destructive">
            <div className="text-center space-y-2 p-4">
              <AlertCircle className="h-8 w-8 text-destructive mx-auto" />
              <p className="text-sm font-medium text-destructive">
                Failed to load chart
              </p>
              <p className="text-xs text-muted-foreground">{error}</p>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!chartData || chartData.length === 0) {
    return (
      <Card className={className}>
        <CardContent className="p-6">
          <div className="flex items-center justify-center h-[500px] border rounded-lg bg-muted/50">
            <p className="text-sm text-muted-foreground">
              No chart data available
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className={className}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="text-lg">Trade Price Chart</CardTitle>
          <div className="flex items-center gap-2">
            <Badge variant={direction === "long" ? "profit" : "loss"}>
              {direction.toUpperCase()}
            </Badge>
            <span className="text-sm text-muted-foreground">
              {chartData.length} bars
            </span>
          </div>
        </div>

        {/* Indicator Toggle Buttons */}
        {indicatorSeries && Object.keys(indicatorSeries).length > 0 && (
          <div className="flex flex-wrap gap-2 mt-3">
            {Object.keys(indicatorSeries).map((indicator) => {
              const isVisible = visibleIndicators.has(indicator);
              return (
                <Button
                  key={indicator}
                  variant={isVisible ? "default" : "outline"}
                  size="sm"
                  onClick={() => toggleIndicator(indicator)}
                  className="text-xs"
                >
                  {isVisible ? (
                    <Eye className="mr-1 h-3 w-3" />
                  ) : (
                    <EyeOff className="mr-1 h-3 w-3" />
                  )}
                  {indicator.toUpperCase()}
                </Button>
              );
            })}
          </div>
        )}
      </CardHeader>

      <CardContent>
        <div ref={chartContainerRef} className="w-full" />

        {/* Legend */}
        <div className="mt-4 flex flex-wrap items-center gap-4 text-xs text-muted-foreground">
          <span className="flex items-center gap-2">
            <span className="w-4 h-0.5 bg-primary"></span>
            Entry: {trade.entryPrice.toFixed(2)}
          </span>
          {stopPrice && (
            <span className="flex items-center gap-2">
              <span
                className="w-4 h-0.5 bg-destructive"
                style={{ borderTop: "2px dashed #EF4444" }}
              ></span>
              Stop: {stopPrice.toFixed(2)}
            </span>
          )}
          {targetPrice && (
            <span className="flex items-center gap-2">
              <span
                className="w-4 h-0.5 bg-profit"
                style={{ borderTop: "2px dashed #22C55E" }}
              ></span>
              Target: {targetPrice.toFixed(2)}
            </span>
          )}
          {trade.exitPrice && (
            <span className="flex items-center gap-2">
              <span
                className={`w-2 h-2 rounded-full ${
                  trade.pnl > 0 ? "bg-profit" : "bg-destructive"
                }`}
              ></span>
              Exit: {trade.exitPrice.toFixed(2)}
            </span>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

/**
 * Get color for indicator based on name
 */
function getIndicatorColor(indicatorName: string): string {
  const colors: Record<string, string> = {
    ema9: "#F59E0B",
    ema20: "#8B5CF6",
    ema50: "#EC4899",
    vwap: "#06B6D4",
    rsi: "#F97316",
    macd: "#10B981",
    bb_upper: "#6366F1",
    bb_lower: "#6366F1",
  };

  return colors[indicatorName.toLowerCase()] || "#64748B";
}
