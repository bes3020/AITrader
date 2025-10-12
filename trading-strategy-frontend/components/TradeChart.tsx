"use client";

import { useEffect, useRef, useState } from "react";
import {
  createChart,
  ColorType,
  IChartApi,
  ISeriesApi,
  CandlestickSeriesPartialOptions,
  Time,
} from "lightweight-charts";
import type { Bar } from "@/lib/types";
import { AlertCircle } from "lucide-react";

interface TradeChartProps {
  /**
   * Historical price data around the trade
   */
  bars: Bar[];

  /**
   * Trade entry time
   */
  entryTime: Date;

  /**
   * Trade exit time
   */
  exitTime: Date;

  /**
   * Entry price level
   */
  entryPrice: number;

  /**
   * Exit price level
   */
  exitPrice: number;

  /**
   * Stop loss price level
   */
  stopPrice: number;

  /**
   * Take profit price level
   */
  targetPrice: number;

  /**
   * Trade direction
   */
  direction: "long" | "short";

  /**
   * Optional className for styling
   */
  className?: string;
}

/**
 * Error Boundary wrapper component for chart
 */
function ChartErrorBoundary({ children }: { children: React.ReactNode }) {
  const [hasError, setHasError] = useState(false);

  useEffect(() => {
    const errorHandler = (error: ErrorEvent) => {
      console.error("[TradeChart] Error caught:", error);
      setHasError(true);
    };

    window.addEventListener("error", errorHandler);
    return () => window.removeEventListener("error", errorHandler);
  }, []);

  if (hasError) {
    return (
      <div className="flex items-center justify-center h-[300px] border rounded-lg bg-muted/50">
        <div className="text-center space-y-2">
          <AlertCircle className="h-8 w-8 text-destructive mx-auto" />
          <p className="text-sm font-medium text-destructive">
            Failed to load chart
          </p>
          <p className="text-xs text-muted-foreground">
            Please try refreshing the page
          </p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}

/**
 * Displays a candlestick chart for a trade using Lightweight Charts
 * Shows entry/exit points, stop/target levels, and trade direction
 */
export function TradeChart({
  bars,
  entryTime,
  exitTime,
  entryPrice,
  exitPrice,
  stopPrice,
  targetPrice,
  direction,
  className,
}: TradeChartProps) {
  const chartContainerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const candlestickSeriesRef = useRef<ISeriesApi<"Candlestick"> | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!chartContainerRef.current) return;

    try {
      // Create chart instance
      const chart = createChart(chartContainerRef.current, {
        width: chartContainerRef.current.clientWidth,
        height: 300,
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
      });

      chartRef.current = chart;

      // Determine if trade was profitable
      const isProfitable =
        direction === "long"
          ? exitPrice > entryPrice
          : exitPrice < entryPrice;

      // Create candlestick series with color based on trade result
      const candlestickOptions: CandlestickSeriesPartialOptions = {
        upColor: "#22C55E",
        downColor: "#EF4444",
        borderUpColor: isProfitable ? "#22C55E" : "#EF4444",
        borderDownColor: isProfitable ? "#22C55E" : "#EF4444",
        wickUpColor: "#22C55E",
        wickDownColor: "#EF4444",
      };

      const candlestickSeries = chart.addCandlestickSeries(candlestickOptions);
      candlestickSeriesRef.current = candlestickSeries;

      // Transform bars data to lightweight-charts format
      const chartData = bars.map((bar) => ({
        time: Math.floor(new Date(bar.timestamp).getTime() / 1000) as Time,
        open: bar.open,
        high: bar.high,
        low: bar.low,
        close: bar.close,
      }));

      candlestickSeries.setData(chartData);

      // Add entry and exit markers
      const entryTimeUnix = Math.floor(entryTime.getTime() / 1000) as Time;
      const exitTimeUnix = Math.floor(exitTime.getTime() / 1000) as Time;

      candlestickSeries.setMarkers([
        {
          time: entryTimeUnix,
          position: direction === "long" ? "belowBar" : "aboveBar",
          color: "#22C55E",
          shape: direction === "long" ? "arrowUp" : "arrowDown",
          text: `Entry: ${entryPrice.toFixed(2)}`,
          size: 1,
        },
        {
          time: exitTimeUnix,
          position: direction === "long" ? "aboveBar" : "belowBar",
          color: isProfitable ? "#22C55E" : "#EF4444",
          shape: direction === "long" ? "arrowDown" : "arrowUp",
          text: `Exit: ${exitPrice.toFixed(2)}`,
          size: 1,
        },
      ]);

      // Add horizontal lines for entry, stop, and target prices
      // Entry price line (blue)
      const entryLine = candlestickSeries.createPriceLine({
        price: entryPrice,
        color: "#3B82F6",
        lineWidth: 2,
        lineStyle: 0, // Solid
        axisLabelVisible: true,
        title: "Entry",
      });

      // Stop loss line (red)
      const stopLine = candlestickSeries.createPriceLine({
        price: stopPrice,
        color: "#EF4444",
        lineWidth: 2,
        lineStyle: 2, // Dashed
        axisLabelVisible: true,
        title: "Stop",
      });

      // Target price line (green)
      const targetLine = candlestickSeries.createPriceLine({
        price: targetPrice,
        color: "#22C55E",
        lineWidth: 2,
        lineStyle: 2, // Dashed
        axisLabelVisible: true,
        title: "Target",
      });

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

        if (candlestickSeriesRef.current) {
          // Remove price lines
          if (entryLine) candlestickSeriesRef.current.removePriceLine(entryLine);
          if (stopLine) candlestickSeriesRef.current.removePriceLine(stopLine);
          if (targetLine) candlestickSeriesRef.current.removePriceLine(targetLine);
        }

        if (chartRef.current) {
          chartRef.current.remove();
          chartRef.current = null;
          candlestickSeriesRef.current = null;
        }
      };
    } catch (err) {
      console.error("[TradeChart] Failed to create chart:", err);
      setError(err instanceof Error ? err.message : "Unknown error");
    }
  }, [
    bars,
    entryTime,
    exitTime,
    entryPrice,
    exitPrice,
    stopPrice,
    targetPrice,
    direction,
  ]);

  if (error) {
    return (
      <div className={className}>
        <div className="flex items-center justify-center h-[300px] border rounded-lg bg-destructive/5 border-destructive">
          <div className="text-center space-y-2 p-4">
            <AlertCircle className="h-8 w-8 text-destructive mx-auto" />
            <p className="text-sm font-medium text-destructive">
              Failed to load chart
            </p>
            <p className="text-xs text-muted-foreground">{error}</p>
          </div>
        </div>
      </div>
    );
  }

  if (!bars || bars.length === 0) {
    return (
      <div className={className}>
        <div className="flex items-center justify-center h-[300px] border rounded-lg bg-muted/50">
          <p className="text-sm text-muted-foreground">No chart data available</p>
        </div>
      </div>
    );
  }

  return (
    <ChartErrorBoundary>
      <div className={className}>
        <div className="border rounded-lg p-4 bg-card">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-medium">Trade Chart</h3>
            <div className="flex items-center gap-4 text-xs text-muted-foreground">
              <span className="flex items-center gap-1">
                <span className="w-3 h-0.5 bg-primary"></span>
                Entry
              </span>
              <span className="flex items-center gap-1">
                <span className="w-3 h-0.5 bg-destructive" style={{ borderTop: "2px dashed" }}></span>
                Stop
              </span>
              <span className="flex items-center gap-1">
                <span className="w-3 h-0.5 bg-profit" style={{ borderTop: "2px dashed" }}></span>
                Target
              </span>
            </div>
          </div>
          <div ref={chartContainerRef} className="w-full" />
          <div className="mt-2 flex items-center justify-between text-xs text-muted-foreground">
            <span>
              Direction:{" "}
              <span className={direction === "long" ? "text-profit" : "text-destructive"}>
                {direction.toUpperCase()}
              </span>
            </span>
            <span>{bars.length} bars</span>
          </div>
        </div>
      </div>
    </ChartErrorBoundary>
  );
}
