"use client";

import {
  TrendingUp,
  TrendingDown,
  Activity,
  Target,
  Clock,
  Award,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { TradeListSummary } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";

interface TradeSummaryPanelProps {
  /**
   * Trade list summary statistics
   */
  summary: TradeListSummary;

  /**
   * Optional className for styling
   */
  className?: string;
}

/**
 * Displays summary statistics for a list of trades
 */
export function TradeSummaryPanel({
  summary,
  className,
}: TradeSummaryPanelProps) {
  const winRatePercentage = (summary.winRate * 100).toFixed(1);
  const lossRate = (
    ((summary.losses + summary.timeouts) / summary.totalTrades) *
    100
  ).toFixed(1);

  return (
    <div className={className}>
      {/* Main Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {/* Total Trades */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Trades</CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{summary.totalTrades}</div>
            <p className="text-xs text-muted-foreground mt-1">
              {summary.wins}W / {summary.losses}L / {summary.timeouts}T
            </p>
          </CardContent>
        </Card>

        {/* Win Rate */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Win Rate</CardTitle>
            <Target className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-profit">
              {winRatePercentage}%
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              {summary.wins} winning trades
            </p>
          </CardContent>
        </Card>

        {/* Total P&L */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total P&L</CardTitle>
            {summary.totalPnl >= 0 ? (
              <TrendingUp className="h-4 w-4 text-profit" />
            ) : (
              <TrendingDown className="h-4 w-4 text-loss" />
            )}
          </CardHeader>
          <CardContent>
            <div
              className={`text-2xl font-bold ${
                summary.totalPnl >= 0 ? "text-profit" : "text-loss"
              }`}
            >
              {formatCurrency(summary.totalPnl)}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Avg: {formatCurrency(summary.avgPnl)}
            </p>
          </CardContent>
        </Card>

        {/* Average Win */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Avg Win/Loss</CardTitle>
            <Award className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-profit">
              {formatCurrency(summary.avgWin)}
            </div>
            <p className="text-xs text-loss mt-1">
              Loss: {formatCurrency(summary.avgLoss)}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Detailed Stats Card */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Performance Details</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {/* Win/Loss Breakdown */}
            <div className="space-y-3">
              <h4 className="text-sm font-medium text-muted-foreground">
                Trade Outcomes
              </h4>
              <div className="space-y-2">
                <div className="flex justify-between items-center">
                  <span className="text-sm">Wins</span>
                  <span className="text-sm font-semibold text-profit">
                    {summary.wins} ({winRatePercentage}%)
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm">Losses</span>
                  <span className="text-sm font-semibold text-loss">
                    {summary.losses} (
                    {((summary.losses / summary.totalTrades) * 100).toFixed(1)}
                    %)
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm">Timeouts</span>
                  <span className="text-sm font-semibold text-muted-foreground">
                    {summary.timeouts} (
                    {((summary.timeouts / summary.totalTrades) * 100).toFixed(
                      1
                    )}
                    %)
                  </span>
                </div>
              </div>
            </div>

            {/* P&L Stats */}
            <div className="space-y-3">
              <h4 className="text-sm font-medium text-muted-foreground">
                P&L Statistics
              </h4>
              <div className="space-y-2">
                <div className="flex justify-between items-center">
                  <span className="text-sm">Total P&L</span>
                  <span
                    className={`text-sm font-semibold ${
                      summary.totalPnl >= 0 ? "text-profit" : "text-loss"
                    }`}
                  >
                    {formatCurrency(summary.totalPnl)}
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm">Average P&L</span>
                  <span
                    className={`text-sm font-semibold ${
                      summary.avgPnl >= 0 ? "text-profit" : "text-loss"
                    }`}
                  >
                    {formatCurrency(summary.avgPnl)}
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm">Largest Win</span>
                  <span className="text-sm font-semibold text-profit">
                    {formatCurrency(summary.largestWin)}
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm">Largest Loss</span>
                  <span className="text-sm font-semibold text-loss">
                    {formatCurrency(summary.largestLoss)}
                  </span>
                </div>
              </div>
            </div>

            {/* Win/Loss Comparison */}
            <div className="space-y-3">
              <h4 className="text-sm font-medium text-muted-foreground">
                Win/Loss Analysis
              </h4>
              <div className="space-y-2">
                <div className="flex justify-between items-center">
                  <span className="text-sm">Average Win</span>
                  <span className="text-sm font-semibold text-profit">
                    {formatCurrency(summary.avgWin)}
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm">Average Loss</span>
                  <span className="text-sm font-semibold text-loss">
                    {formatCurrency(summary.avgLoss)}
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm">Win/Loss Ratio</span>
                  <span className="text-sm font-semibold">
                    {summary.avgLoss !== 0
                      ? Math.abs(summary.avgWin / summary.avgLoss).toFixed(2)
                      : "N/A"}
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm">Expectancy</span>
                  <span
                    className={`text-sm font-semibold ${
                      summary.avgPnl >= 0 ? "text-profit" : "text-loss"
                    }`}
                  >
                    {formatCurrency(summary.avgPnl)}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Visual Win Rate Bar */}
          <div className="mt-6 space-y-2">
            <div className="flex justify-between text-xs text-muted-foreground">
              <span>Win Rate Distribution</span>
              <span>{winRatePercentage}% wins</span>
            </div>
            <div className="h-4 bg-muted rounded-full overflow-hidden flex">
              <div
                className="bg-profit transition-all"
                style={{ width: `${summary.winRate * 100}%` }}
                title={`${summary.wins} wins`}
              />
              <div
                className="bg-loss transition-all"
                style={{
                  width: `${(summary.losses / summary.totalTrades) * 100}%`,
                }}
                title={`${summary.losses} losses`}
              />
              <div
                className="bg-muted-foreground transition-all"
                style={{
                  width: `${(summary.timeouts / summary.totalTrades) * 100}%`,
                }}
                title={`${summary.timeouts} timeouts`}
              />
            </div>
            <div className="flex justify-between text-xs">
              <span className="text-profit">{summary.wins} Wins</span>
              <span className="text-loss">{summary.losses} Losses</span>
              <span className="text-muted-foreground">
                {summary.timeouts} Timeouts
              </span>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
