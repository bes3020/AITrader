"use client";

import { AlertTriangle } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { TradeResult } from "@/lib/types";
import { TradeCard } from "./TradeCard";

interface WorstTradesSectionProps {
  /**
   * Array of worst performing trades (typically top 5)
   */
  trades: TradeResult[];

  /**
   * Optional AI-generated insights about the worst trades
   */
  insights?: string | null;

  /**
   * Optional loading state
   */
  loading?: boolean;

  /**
   * Optional error message
   */
  error?: string | null;

  /**
   * Optional className for styling
   */
  className?: string;
}

/**
 * Displays the worst performing trades with analysis and insights
 */
export function WorstTradesSection({
  trades,
  insights,
  loading,
  error,
  className,
}: WorstTradesSectionProps) {
  if (loading) {
    return (
      <div className={className}>
        <div className="space-y-6">
          <div className="flex items-center gap-3">
            <div className="h-8 w-8 bg-muted rounded animate-pulse" />
            <div className="h-6 bg-muted rounded w-64 animate-pulse" />
          </div>
          {[...Array(5)].map((_, i) => (
            <Card key={i} className="animate-pulse border-destructive/20">
              <CardHeader className="pb-3">
                <div className="h-4 bg-muted rounded w-1/3 mb-2" />
                <div className="h-6 bg-muted rounded w-1/4" />
              </CardHeader>
              <CardContent>
                <div className="h-24 bg-muted rounded" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <Card className={`border-destructive ${className}`}>
        <CardContent className="pt-6">
          <div className="flex items-center gap-3 text-destructive">
            <AlertTriangle className="h-5 w-5" />
            <p className="text-sm font-medium">{error}</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!trades || trades.length === 0) {
    return (
      <Card className={className}>
        <CardContent className="pt-6">
          <div className="flex items-center gap-3 text-muted-foreground">
            <AlertTriangle className="h-5 w-5" />
            <p className="text-sm">No trades to analyze</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className={className}>
      <div className="space-y-6">
        {/* Section Header */}
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 bg-destructive/10 rounded-lg flex items-center justify-center">
            <AlertTriangle className="h-6 w-6 text-destructive" />
          </div>
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-destructive">
              These {trades.length} Trades Killed Your P&L
            </h2>
            <p className="text-sm text-muted-foreground">
              Analyzing your worst performing trades to identify improvement areas
            </p>
          </div>
        </div>

        {/* Trade Cards */}
        <div className="space-y-4">
          {trades.map((trade, index) => (
            <div
              key={`trade-${index}-${trade.entryTime}`}
              className="relative"
            >
              {/* Rank Badge */}
              <div className="absolute -left-4 top-4 z-10 w-8 h-8 bg-destructive rounded-full flex items-center justify-center text-destructive-foreground font-bold text-sm shadow-md">
                #{index + 1}
              </div>

              <TradeCard
                trade={trade}
                index={index + 1}
                showChart={false}
                className="border-destructive/30 hover:border-destructive/50 transition-colors ml-6"
              />
            </div>
          ))}
        </div>

        {/* AI Insights */}
        {insights && (
          <Card className="border-destructive/30 bg-destructive/5">
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-destructive">
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
                AI Analysis - What Went Wrong
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="prose prose-sm max-w-none">
                <p className="whitespace-pre-wrap text-sm leading-relaxed text-foreground/90">
                  {insights}
                </p>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Summary Stats */}
        <Card className="border-destructive/20">
          <CardHeader>
            <CardTitle className="text-base">Impact Summary</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div>
                <p className="text-sm text-muted-foreground">Total Loss</p>
                <p className="text-2xl font-bold text-destructive">
                  ${Math.abs(trades.reduce((sum, t) => sum + t.pnl, 0)).toFixed(2)}
                </p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Average Loss</p>
                <p className="text-2xl font-bold text-destructive">
                  ${Math.abs(trades.reduce((sum, t) => sum + t.pnl, 0) / trades.length).toFixed(2)}
                </p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Average Duration</p>
                <p className="text-2xl font-bold text-foreground">
                  {Math.round(trades.reduce((sum, t) => sum + t.barsHeld, 0) / trades.length)} bars
                </p>
              </div>
            </div>
            <div className="mt-4 pt-4 border-t">
              <p className="text-xs text-muted-foreground">
                These {trades.length} trades represent your largest losses. Focus on the patterns in entry/exit timing,
                market conditions, and trade management to improve future performance.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
