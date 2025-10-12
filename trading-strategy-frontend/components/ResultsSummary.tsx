"use client";

import {
  TrendingUp,
  TrendingDown,
  Target,
  AlertCircle,
  Activity,
  DollarSign,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import type { StrategyResult } from "@/lib/types";
import { formatCurrency, formatPercentage } from "@/lib/utils";

interface ResultsSummaryProps {
  /**
   * Strategy result data to display
   */
  result: StrategyResult;

  /**
   * Optional loading state
   */
  loading?: boolean;

  /**
   * Optional error message
   */
  error?: string | null;
}

/**
 * Displays key performance metrics for a strategy in a grid layout
 */
export function ResultsSummary({ result, loading, error }: ResultsSummaryProps) {
  if (loading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {[...Array(6)].map((_, i) => (
          <Card key={i} className="animate-pulse">
            <CardHeader className="pb-3">
              <div className="h-4 bg-muted rounded w-1/2" />
            </CardHeader>
            <CardContent>
              <div className="h-8 bg-muted rounded w-3/4 mb-2" />
              <div className="h-3 bg-muted rounded w-1/2" />
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <Card className="border-destructive">
        <CardContent className="pt-6">
          <div className="flex items-center gap-3 text-destructive">
            <AlertCircle className="h-5 w-5" />
            <p className="text-sm font-medium">{error}</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  const winningTrades = Math.round(result.totalTrades * result.winRate);
  const losingTrades = result.totalTrades - winningTrades;
  const isProfitable = result.totalPnl > 0;

  return (
    <div className="space-y-6">
      {/* Performance Badge */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Performance Summary</h2>
          <p className="text-muted-foreground">
            Backtest period: {new Date(result.backtestStart).toLocaleDateString()} -{" "}
            {new Date(result.backtestEnd).toLocaleDateString()}
          </p>
        </div>
        <Badge
          variant={isProfitable ? "profit" : "loss"}
          className="text-lg px-4 py-2"
        >
          {isProfitable ? "Profitable" : "Unprofitable"}
        </Badge>
      </div>

      {/* Metrics Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {/* Win Rate */}
        <Card className="border-2">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Win Rate
            </CardTitle>
            {result.winRate >= 0.5 ? (
              <TrendingUp className="h-4 w-4 text-profit" />
            ) : (
              <TrendingDown className="h-4 w-4 text-loss" />
            )}
          </CardHeader>
          <CardContent>
            <div className="text-4xl font-bold">
              {formatPercentage(result.winRate)}
            </div>
            <p className="text-xs text-muted-foreground mt-2">
              {winningTrades} wins, {losingTrades} losses
            </p>
            <div className="mt-3 h-2 bg-muted rounded-full overflow-hidden">
              <div
                className="h-full bg-profit transition-all"
                style={{ width: `${result.winRate * 100}%` }}
              />
            </div>
          </CardContent>
        </Card>

        {/* Total P&L */}
        <Card className="border-2">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Total P&L
            </CardTitle>
            <DollarSign
              className={`h-4 w-4 ${isProfitable ? "text-profit" : "text-loss"}`}
            />
          </CardHeader>
          <CardContent>
            <div
              className={`text-4xl font-bold ${
                isProfitable ? "text-profit" : "text-loss"
              }`}
            >
              {formatCurrency(result.totalPnl)}
            </div>
            <p className="text-xs text-muted-foreground mt-2">
              Across {result.totalTrades} trades
            </p>
            {result.profitFactor && (
              <p className="text-xs text-muted-foreground mt-1">
                Profit Factor: {result.profitFactor.toFixed(2)}
              </p>
            )}
          </CardContent>
        </Card>

        {/* Total Trades */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Total Trades
            </CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-4xl font-bold">{result.totalTrades}</div>
            <p className="text-xs text-muted-foreground mt-2">
              {winningTrades} winning, {losingTrades} losing
            </p>
            {result.sharpeRatio && (
              <p className="text-xs text-muted-foreground mt-1">
                Sharpe Ratio: {result.sharpeRatio.toFixed(2)}
              </p>
            )}
          </CardContent>
        </Card>

        {/* Average Win */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Average Win
            </CardTitle>
            <Target className="h-4 w-4 text-profit" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-profit">
              {formatCurrency(result.avgWin)}
            </div>
            <p className="text-xs text-muted-foreground mt-2">
              Per winning trade
            </p>
            {result.avgWin > 0 && result.avgLoss < 0 && (
              <p className="text-xs text-muted-foreground mt-1">
                Win/Loss Ratio: {(result.avgWin / Math.abs(result.avgLoss)).toFixed(2)}
              </p>
            )}
          </CardContent>
        </Card>

        {/* Average Loss */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Average Loss
            </CardTitle>
            <Target className="h-4 w-4 text-loss" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-loss">
              {formatCurrency(result.avgLoss)}
            </div>
            <p className="text-xs text-muted-foreground mt-2">
              Per losing trade
            </p>
            {result.avgWin > 0 && result.avgLoss < 0 && (
              <p className="text-xs text-muted-foreground mt-1">
                Risk/Reward: 1:{(result.avgWin / Math.abs(result.avgLoss)).toFixed(2)}
              </p>
            )}
          </CardContent>
        </Card>

        {/* Max Drawdown */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Max Drawdown
            </CardTitle>
            <AlertCircle className="h-4 w-4 text-loss" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-loss">
              {formatCurrency(result.maxDrawdown)}
            </div>
            <p className="text-xs text-muted-foreground mt-2">
              Largest peak-to-trough decline
            </p>
            {result.totalPnl !== 0 && (
              <p className="text-xs text-muted-foreground mt-1">
                {((result.maxDrawdown / result.totalPnl) * 100).toFixed(1)}% of total P&L
              </p>
            )}
          </CardContent>
        </Card>
      </div>

      {/* AI Insights */}
      {result.insights && (
        <Card className="border-primary/20 bg-primary/5">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <svg
                className="h-5 w-5"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"
                />
              </svg>
              AI Insights
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="prose prose-sm max-w-none">
              <p className="whitespace-pre-wrap text-sm leading-relaxed">
                {result.insights}
              </p>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
