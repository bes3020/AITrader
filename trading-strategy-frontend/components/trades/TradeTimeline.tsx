"use client";

import { format } from "date-fns";
import { TrendingUp, TrendingDown, AlertCircle, Target } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import type { BarData } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";

interface TradeTimelineProps {
  /**
   * Trade bars data (from entry to exit)
   */
  tradeBars: BarData[];

  /**
   * Entry price
   */
  entryPrice: number;

  /**
   * Trade direction
   */
  direction: "long" | "short";

  /**
   * Optional stop price
   */
  stopPrice?: number;

  /**
   * Optional target price
   */
  targetPrice?: number;

  /**
   * Optional className for styling
   */
  className?: string;
}

/**
 * Timeline view showing bar-by-bar progression of a trade
 */
export function TradeTimeline({
  tradeBars,
  entryPrice,
  direction,
  stopPrice,
  targetPrice,
  className,
}: TradeTimelineProps) {
  if (!tradeBars || tradeBars.length === 0) {
    return (
      <Card className={className}>
        <CardContent className="p-6">
          <div className="flex items-center justify-center py-8">
            <p className="text-sm text-muted-foreground">
              No trade bar data available
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  // Calculate unrealized P&L for each bar
  const barsWithPnl = tradeBars.map((bar, idx) => {
    const unrealizedPnl =
      direction === "long"
        ? bar.close - entryPrice
        : entryPrice - bar.close;

    const highPnl =
      direction === "long" ? bar.high - entryPrice : entryPrice - bar.low;

    const lowPnl =
      direction === "long" ? bar.low - entryPrice : entryPrice - bar.high;

    const priceChange = ((bar.close - bar.open) / bar.open) * 100;

    let status = "neutral";
    if (stopPrice) {
      if (direction === "long" && bar.low <= stopPrice) status = "stopped";
      if (direction === "short" && bar.high >= stopPrice) status = "stopped";
    }
    if (targetPrice) {
      if (direction === "long" && bar.high >= targetPrice) status = "target";
      if (direction === "short" && bar.low <= targetPrice) status = "target";
    }

    return {
      ...bar,
      barNumber: idx + 1,
      unrealizedPnl,
      highPnl,
      lowPnl,
      priceChange,
      status,
    };
  });

  const maxPnl = Math.max(...barsWithPnl.map((b) => b.highPnl));
  const minPnl = Math.min(...barsWithPnl.map((b) => b.lowPnl));

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle className="text-lg">Trade Timeline</CardTitle>
        <p className="text-sm text-muted-foreground">
          Bar-by-bar progression ({tradeBars.length} bars)
        </p>
      </CardHeader>
      <CardContent>
        <div className="space-y-3">
          {barsWithPnl.map((bar) => {
            const isPositive = bar.unrealizedPnl > 0;
            const isNegative = bar.unrealizedPnl < 0;

            return (
              <div
                key={bar.barNumber}
                className={`p-4 border rounded-lg transition-all ${
                  bar.status === "stopped"
                    ? "border-destructive bg-destructive/5"
                    : bar.status === "target"
                    ? "border-profit bg-profit/5"
                    : isPositive
                    ? "border-profit/30 bg-profit/5"
                    : isNegative
                    ? "border-destructive/30 bg-destructive/5"
                    : "border-border"
                }`}
              >
                <div className="flex items-start justify-between gap-4">
                  {/* Bar Info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-2">
                      <Badge variant="outline" className="text-xs">
                        Bar #{bar.barNumber}
                      </Badge>
                      <span className="text-xs text-muted-foreground">
                        {format(new Date(bar.timestamp), "MMM dd, HH:mm")}
                      </span>
                      {bar.status === "stopped" && (
                        <Badge variant="destructive" className="text-xs">
                          <AlertCircle className="mr-1 h-3 w-3" />
                          Stop Hit
                        </Badge>
                      )}
                      {bar.status === "target" && (
                        <Badge variant="profit" className="text-xs">
                          <Target className="mr-1 h-3 w-3" />
                          Target Hit
                        </Badge>
                      )}
                    </div>

                    {/* OHLC */}
                    <div className="grid grid-cols-4 gap-3 text-sm mb-2">
                      <div>
                        <p className="text-xs text-muted-foreground">Open</p>
                        <p className="font-mono font-medium">
                          {bar.open.toFixed(2)}
                        </p>
                      </div>
                      <div>
                        <p className="text-xs text-muted-foreground">High</p>
                        <p className="font-mono font-medium text-profit">
                          {bar.high.toFixed(2)}
                        </p>
                      </div>
                      <div>
                        <p className="text-xs text-muted-foreground">Low</p>
                        <p className="font-mono font-medium text-destructive">
                          {bar.low.toFixed(2)}
                        </p>
                      </div>
                      <div>
                        <p className="text-xs text-muted-foreground">Close</p>
                        <p className="font-mono font-medium">
                          {bar.close.toFixed(2)}
                        </p>
                      </div>
                    </div>

                    {/* Price Change */}
                    <div className="flex items-center gap-2 text-xs">
                      {bar.priceChange > 0 ? (
                        <TrendingUp className="h-3 w-3 text-profit" />
                      ) : bar.priceChange < 0 ? (
                        <TrendingDown className="h-3 w-3 text-destructive" />
                      ) : null}
                      <span
                        className={
                          bar.priceChange > 0
                            ? "text-profit"
                            : bar.priceChange < 0
                            ? "text-destructive"
                            : "text-muted-foreground"
                        }
                      >
                        {bar.priceChange > 0 ? "+" : ""}
                        {bar.priceChange.toFixed(2)}%
                      </span>
                    </div>
                  </div>

                  {/* Unrealized P&L */}
                  <div className="text-right">
                    <p className="text-xs text-muted-foreground mb-1">
                      Unrealized P&L
                    </p>
                    <p
                      className={`text-xl font-bold ${
                        isPositive
                          ? "text-profit"
                          : isNegative
                          ? "text-destructive"
                          : "text-muted-foreground"
                      }`}
                    >
                      {isPositive ? "+" : ""}
                      {bar.unrealizedPnl.toFixed(2)}
                    </p>
                    <p className="text-xs text-muted-foreground mt-1">
                      High: {bar.highPnl.toFixed(2)} / Low:{" "}
                      {bar.lowPnl.toFixed(2)}
                    </p>
                  </div>
                </div>

                {/* P&L Progress Bar */}
                <div className="mt-3">
                  <div className="h-2 bg-muted rounded-full overflow-hidden relative">
                    {/* Zero line marker */}
                    {minPnl < 0 && maxPnl > 0 && (
                      <div
                        className="absolute top-0 bottom-0 w-0.5 bg-border z-10"
                        style={{
                          left: `${((0 - minPnl) / (maxPnl - minPnl)) * 100}%`,
                        }}
                      />
                    )}
                    {/* P&L bar */}
                    <div
                      className={`h-full transition-all ${
                        bar.unrealizedPnl > 0 ? "bg-profit" : "bg-destructive"
                      }`}
                      style={{
                        width: `${
                          Math.abs(
                            ((bar.unrealizedPnl - minPnl) / (maxPnl - minPnl)) *
                              100
                          ) || 0
                        }%`,
                        marginLeft:
                          bar.unrealizedPnl < 0
                            ? `${((minPnl - bar.unrealizedPnl) / (maxPnl - minPnl)) * 100}%`
                            : `${((0 - minPnl) / (maxPnl - minPnl)) * 100}%`,
                      }}
                    />
                  </div>
                  <div className="flex justify-between text-xs text-muted-foreground mt-1">
                    <span>Min: {minPnl.toFixed(2)}</span>
                    <span>Max: {maxPnl.toFixed(2)}</span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        {/* Summary */}
        <div className="mt-6 p-4 bg-muted/50 rounded-lg">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div>
              <p className="text-muted-foreground">Total Bars</p>
              <p className="font-semibold">{tradeBars.length}</p>
            </div>
            <div>
              <p className="text-muted-foreground">Max Profit</p>
              <p className="font-semibold text-profit">
                +{maxPnl.toFixed(2)}
              </p>
            </div>
            <div>
              <p className="text-muted-foreground">Max Loss</p>
              <p className="font-semibold text-destructive">
                {minPnl.toFixed(2)}
              </p>
            </div>
            <div>
              <p className="text-muted-foreground">Entry Price</p>
              <p className="font-semibold font-mono">
                {entryPrice.toFixed(2)}
              </p>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
