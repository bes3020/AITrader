"use client";

import { useRouter } from "next/navigation";
import { format } from "date-fns";
import { TrendingUp, TrendingDown, ArrowRight } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import type { TradeResult } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";

interface SimilarTradesPanelProps {
  /**
   * Current trade for comparison
   */
  currentTrade: TradeResult;

  /**
   * All trades to search through
   */
  allTrades: TradeResult[];

  /**
   * Strategy ID for navigation
   */
  strategyId: number;

  /**
   * Result ID for navigation
   */
  resultId: number;

  /**
   * Maximum number of similar trades to show
   */
  maxResults?: number;

  /**
   * Optional className for styling
   */
  className?: string;
}

interface SimilarTrade {
  trade: TradeResult;
  similarity: number;
  reasons: string[];
}

/**
 * Finds and displays trades similar to the current trade
 */
export function SimilarTradesPanel({
  currentTrade,
  allTrades,
  strategyId,
  resultId,
  maxResults = 5,
  className,
}: SimilarTradesPanelProps) {
  const router = useRouter();

  // Find similar trades based on multiple criteria
  const findSimilarTrades = (): SimilarTrade[] => {
    const similarTrades: SimilarTrade[] = [];

    for (const trade of allTrades) {
      // Skip the current trade
      if (trade.id === currentTrade.id) continue;

      const reasons: string[] = [];
      let similarityScore = 0;

      // Same result type (win/loss/timeout)
      if (trade.result === currentTrade.result) {
        reasons.push(`Same outcome (${trade.result})`);
        similarityScore += 20;
      }

      // Similar P&L magnitude (within 20%)
      const pnlDiff = Math.abs(
        (trade.pnl - currentTrade.pnl) / currentTrade.pnl
      );
      if (pnlDiff < 0.2) {
        reasons.push("Similar P&L");
        similarityScore += 15;
      }

      // Similar duration (within 20%)
      const durationDiff = Math.abs(
        (trade.barsHeld - currentTrade.barsHeld) / currentTrade.barsHeld
      );
      if (durationDiff < 0.2) {
        reasons.push("Similar duration");
        similarityScore += 15;
      }

      // Similar entry quality score (within 10 points)
      if (
        trade.entryQualityScore !== null &&
        currentTrade.entryQualityScore !== null
      ) {
        const qualityDiff = Math.abs(
          trade.entryQualityScore - currentTrade.entryQualityScore
        );
        if (qualityDiff <= 10) {
          reasons.push("Similar entry quality");
          similarityScore += 15;
        }
      }

      // Similar exit quality score (within 10 points)
      if (
        trade.exitQualityScore !== null &&
        currentTrade.exitQualityScore !== null
      ) {
        const qualityDiff = Math.abs(
          trade.exitQualityScore - currentTrade.exitQualityScore
        );
        if (qualityDiff <= 10) {
          reasons.push("Similar exit quality");
          similarityScore += 15;
        }
      }

      // Same time of day (within 1 hour)
      const currentHour = new Date(currentTrade.entryTime).getHours();
      const tradeHour = new Date(trade.entryTime).getHours();
      if (Math.abs(tradeHour - currentHour) <= 1) {
        reasons.push("Same time of day");
        similarityScore += 10;
      }

      // Same day of week
      const currentDay = new Date(currentTrade.entryTime).getDay();
      const tradeDay = new Date(trade.entryTime).getDay();
      if (tradeDay === currentDay) {
        reasons.push("Same day of week");
        similarityScore += 10;
      }

      // Similar MAE (within 30%)
      const maeDiff = Math.abs(
        (trade.maxAdverseExcursion - currentTrade.maxAdverseExcursion) /
          (currentTrade.maxAdverseExcursion || 1)
      );
      if (maeDiff < 0.3) {
        reasons.push("Similar adverse excursion");
        similarityScore += 10;
      }

      // Similar MFE (within 30%)
      const mfeDiff = Math.abs(
        (trade.maxFavorableExcursion - currentTrade.maxFavorableExcursion) /
          (currentTrade.maxFavorableExcursion || 1)
      );
      if (mfeDiff < 0.3) {
        reasons.push("Similar favorable excursion");
        similarityScore += 10;
      }

      // Only include if similarity score is meaningful (> 30)
      if (similarityScore > 30 && reasons.length > 0) {
        similarTrades.push({
          trade,
          similarity: similarityScore,
          reasons,
        });
      }
    }

    // Sort by similarity score (descending)
    similarTrades.sort((a, b) => b.similarity - a.similarity);

    return similarTrades.slice(0, maxResults);
  };

  const similarTrades = findSimilarTrades();

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

  const handleViewTrade = (tradeId: number) => {
    router.push(
      `/strategies/${strategyId}/results/${resultId}/trades/${tradeId}`
    );
  };

  if (similarTrades.length === 0) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="text-lg">Similar Trades</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground text-center py-8">
            No similar trades found
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle className="text-lg">Similar Trades</CardTitle>
        <p className="text-sm text-muted-foreground">
          Found {similarTrades.length} trades with similar characteristics
        </p>
      </CardHeader>
      <CardContent className="space-y-4">
        {similarTrades.map(({ trade, similarity, reasons }) => {
          const isProfitable = trade.pnl > 0;
          const isLoss = trade.pnl < 0;

          return (
            <div
              key={trade.id}
              className="p-4 border rounded-lg hover:border-primary transition-colors cursor-pointer"
              onClick={() => handleViewTrade(trade.id!)}
            >
              <div className="flex items-start justify-between gap-4 mb-3">
                {/* Trade Info */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-2">
                    <Badge variant={getResultBadgeVariant(trade.result)}>
                      {trade.result.toUpperCase()}
                    </Badge>
                    <span className="text-xs text-muted-foreground">
                      {format(new Date(trade.entryTime), "MMM dd, HH:mm")}
                    </span>
                    <Badge variant="outline" className="text-xs">
                      {similarity}% match
                    </Badge>
                  </div>

                  <div className="flex items-center gap-4 text-sm">
                    <div>
                      <span className="text-muted-foreground">Entry: </span>
                      <span className="font-mono font-medium">
                        {formatCurrency(trade.entryPrice)}
                      </span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Exit: </span>
                      <span className="font-mono font-medium">
                        {trade.exitPrice
                          ? formatCurrency(trade.exitPrice)
                          : "N/A"}
                      </span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Duration: </span>
                      <span className="font-medium">{trade.barsHeld} bars</span>
                    </div>
                  </div>
                </div>

                {/* P&L */}
                <div className="text-right">
                  <div
                    className={`text-xl font-bold ${
                      isProfitable
                        ? "text-profit"
                        : isLoss
                        ? "text-loss"
                        : "text-muted-foreground"
                    }`}
                  >
                    {formatCurrency(trade.pnl)}
                  </div>
                  {isProfitable ? (
                    <TrendingUp className="h-4 w-4 text-profit ml-auto mt-1" />
                  ) : isLoss ? (
                    <TrendingDown className="h-4 w-4 text-loss ml-auto mt-1" />
                  ) : null}
                </div>
              </div>

              {/* Similarity Reasons */}
              <div className="flex flex-wrap gap-2 mb-3">
                {reasons.map((reason, idx) => (
                  <Badge key={idx} variant="secondary" className="text-xs">
                    {reason}
                  </Badge>
                ))}
              </div>

              {/* Quality Scores */}
              {(trade.entryQualityScore !== null ||
                trade.exitQualityScore !== null) && (
                <div className="flex items-center gap-4 text-xs text-muted-foreground pt-3 border-t">
                  {trade.entryQualityScore !== null && (
                    <div>
                      Entry Quality:{" "}
                      <span className="font-semibold">
                        {trade.entryQualityScore}/100
                      </span>
                    </div>
                  )}
                  {trade.exitQualityScore !== null && (
                    <div>
                      Exit Quality:{" "}
                      <span className="font-semibold">
                        {trade.exitQualityScore}/100
                      </span>
                    </div>
                  )}
                </div>
              )}

              {/* View Button */}
              <Button
                variant="ghost"
                size="sm"
                className="w-full mt-3"
                onClick={(e) => {
                  e.stopPropagation();
                  handleViewTrade(trade.id!);
                }}
              >
                View Details
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            </div>
          );
        })}
      </CardContent>
    </Card>
  );
}
