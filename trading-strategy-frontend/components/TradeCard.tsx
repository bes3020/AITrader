"use client";

import { format } from "date-fns";
import {
  ArrowDownRight,
  ArrowUpRight,
  Clock,
  TrendingDown,
  TrendingUp,
} from "lucide-react";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import type { TradeResult } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";
import { TradeChart } from "./TradeChart";

interface TradeCardProps {
  /**
   * Trade result data to display
   */
  trade: TradeResult;

  /**
   * Optional index number for display
   */
  index?: number;

  /**
   * Whether to show the chart
   */
  showChart?: boolean;

  /**
   * Optional className for styling
   */
  className?: string;
}

/**
 * Displays detailed information for a single trade
 */
export function TradeCard({
  trade,
  index,
  showChart = false,
  className,
}: TradeCardProps) {
  const isProfitable = trade.pnl > 0;
  const isLoss = trade.pnl < 0;

  const getResultBadgeVariant = (result: string) => {
    switch (result.toLowerCase()) {
      case "win":
        return "profit";
      case "loss":
        return "loss";
      case "timeout":
        return "secondary";
      default:
        return "secondary";
    }
  };

  return (
    <Card className={className}>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="space-y-1">
            {index !== undefined && (
              <p className="text-sm font-medium text-muted-foreground">
                Trade #{index}
              </p>
            )}
            <div className="flex items-center gap-2">
              <Badge variant={getResultBadgeVariant(trade.result)}>
                {trade.result.toUpperCase()}
              </Badge>
              <span className="text-xs text-muted-foreground">
                {trade.barsHeld} bars held
              </span>
            </div>
          </div>
          <div className="text-right">
            <div
              className={`text-2xl font-bold ${
                isProfitable
                  ? "text-profit"
                  : isLoss
                  ? "text-loss"
                  : "text-muted-foreground"
              }`}
            >
              {formatCurrency(trade.pnl)}
            </div>
            <p className="text-xs text-muted-foreground">P&L</p>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Entry/Exit Details */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {/* Entry */}
          <div className="space-y-2">
            <div className="flex items-center gap-2 text-sm font-medium">
              <ArrowDownRight className="h-4 w-4 text-muted-foreground" />
              Entry
            </div>
            <div className="pl-6 space-y-1">
              <p className="text-sm font-mono">
                {formatCurrency(trade.entryPrice)}
              </p>
              <p className="text-xs text-muted-foreground flex items-center gap-1">
                <Clock className="h-3 w-3" />
                {format(new Date(trade.entryTime), "MMM dd, yyyy HH:mm")}
              </p>
            </div>
          </div>

          {/* Exit */}
          <div className="space-y-2">
            <div className="flex items-center gap-2 text-sm font-medium">
              <ArrowUpRight className="h-4 w-4 text-muted-foreground" />
              Exit
            </div>
            <div className="pl-6 space-y-1">
              <p className="text-sm font-mono">
                {trade.exitPrice
                  ? formatCurrency(trade.exitPrice)
                  : "Still Open"}
              </p>
              {trade.exitTime && (
                <p className="text-xs text-muted-foreground flex items-center gap-1">
                  <Clock className="h-3 w-3" />
                  {format(new Date(trade.exitTime), "MMM dd, yyyy HH:mm")}
                </p>
              )}
            </div>
          </div>
        </div>

        {/* MAE and MFE */}
        <div className="grid grid-cols-2 gap-4 pt-2 border-t">
          <div className="space-y-1">
            <div className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
              <TrendingDown className="h-3 w-3 text-loss" />
              Max Adverse Excursion
            </div>
            <p className="text-sm font-semibold text-loss pl-5">
              {formatCurrency(trade.maxAdverseExcursion)}
            </p>
            <p className="text-xs text-muted-foreground pl-5">
              Worst unrealized loss
            </p>
          </div>

          <div className="space-y-1">
            <div className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
              <TrendingUp className="h-3 w-3 text-profit" />
              Max Favorable Excursion
            </div>
            <p className="text-sm font-semibold text-profit pl-5">
              {formatCurrency(trade.maxFavorableExcursion)}
            </p>
            <p className="text-xs text-muted-foreground pl-5">
              Best unrealized profit
            </p>
          </div>
        </div>

        {/* Trade Efficiency */}
        {trade.maxFavorableExcursion > 0 && (
          <div className="pt-2 border-t">
            <div className="flex items-center justify-between text-xs">
              <span className="text-muted-foreground">Trade Efficiency</span>
              <span className="font-medium">
                {((trade.pnl / trade.maxFavorableExcursion) * 100).toFixed(1)}%
              </span>
            </div>
            <div className="mt-2 h-2 bg-muted rounded-full overflow-hidden">
              <div
                className={`h-full transition-all ${
                  trade.pnl > 0 ? "bg-profit" : "bg-loss"
                }`}
                style={{
                  width: `${Math.min(Math.abs((trade.pnl / trade.maxFavorableExcursion) * 100), 100)}%`,
                }}
              />
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              {trade.pnl < trade.maxFavorableExcursion && trade.maxFavorableExcursion > 0
                ? "Gave back profit - consider tighter exits"
                : "Captured most of the move"}
            </p>
          </div>
        )}

        {/* Trade Chart */}
        {/* TODO: TradeChart requires additional props (bars, stopPrice, targetPrice, direction)
            that are not currently available in TradeResult.
            Will need to update the backend API to include this data. */}
        {showChart && (
          <div className="pt-4 border-t">
            <p className="text-sm text-muted-foreground text-center py-8">
              Chart visualization coming soon - requires additional trade data
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
