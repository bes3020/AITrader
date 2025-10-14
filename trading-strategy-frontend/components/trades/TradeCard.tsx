"use client";

import { format } from "date-fns";
import { useRouter } from "next/navigation";
import {
  ArrowDownRight,
  ArrowUpRight,
  Clock,
  TrendingDown,
  TrendingUp,
  Star,
  Award,
  ChevronRight,
} from "lucide-react";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { TradeResult } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";

interface TradeCardProps {
  /**
   * Trade result data to display
   */
  trade: TradeResult;

  /**
   * Strategy ID for navigation
   */
  strategyId: number;

  /**
   * Strategy result ID for navigation
   */
  resultId: number;

  /**
   * Optional index number for display
   */
  index?: number;

  /**
   * Whether to show sparkline chart
   */
  showSparkline?: boolean;

  /**
   * Optional className for styling
   */
  className?: string;
}

/**
 * Enhanced trade card with quality scores and navigation to detail page
 */
export function TradeCard({
  trade,
  strategyId,
  resultId,
  index,
  showSparkline = false,
  className,
}: TradeCardProps) {
  const router = useRouter();
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

  const getQualityColor = (score: number) => {
    if (score >= 80) return "text-green-600";
    if (score >= 60) return "text-yellow-600";
    return "text-orange-600";
  };

  const handleViewDetail = () => {
    router.push(
      `/strategies/${strategyId}/results/${resultId}/trades/${trade.id}`
    );
  };

  return (
    <Card
      className={`${className} hover:shadow-lg transition-shadow cursor-pointer`}
      onClick={handleViewDetail}
    >
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="space-y-1">
            {index !== undefined && (
              <p className="text-sm font-medium text-muted-foreground">
                Trade #{index}
              </p>
            )}
            <div className="flex items-center gap-2 flex-wrap">
              <Badge variant={getResultBadgeVariant(trade.result)}>
                {trade.result.toUpperCase()}
              </Badge>
              <span className="text-xs text-muted-foreground">
                {trade.barsHeld} bars held
              </span>
              {trade.riskRewardRatio && (
                <span className="text-xs text-muted-foreground">
                  R/R: {trade.riskRewardRatio.toFixed(2)}
                </span>
              )}
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
        {/* Quality Scores */}
        {(trade.entryQualityScore !== null ||
          trade.exitQualityScore !== null) && (
          <div className="grid grid-cols-2 gap-3 pb-3 border-b">
            {trade.entryQualityScore !== null && (
              <div className="flex items-center gap-2">
                <Star className="h-4 w-4 text-primary" />
                <div className="flex-1">
                  <p className="text-xs text-muted-foreground">Entry Quality</p>
                  <p
                    className={`text-sm font-semibold ${getQualityColor(trade.entryQualityScore)}`}
                  >
                    {trade.entryQualityScore}/100
                  </p>
                </div>
              </div>
            )}
            {trade.exitQualityScore !== null && (
              <div className="flex items-center gap-2">
                <Award className="h-4 w-4 text-primary" />
                <div className="flex-1">
                  <p className="text-xs text-muted-foreground">Exit Quality</p>
                  <p
                    className={`text-sm font-semibold ${getQualityColor(trade.exitQualityScore)}`}
                  >
                    {trade.exitQualityScore}/100
                  </p>
                </div>
              </div>
            )}
          </div>
        )}

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
                {format(new Date(trade.entryTime), "MMM dd, HH:mm")}
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
                  {format(new Date(trade.exitTime), "MMM dd, HH:mm")}
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
              MAE
            </div>
            <p className="text-sm font-semibold text-loss pl-5">
              {formatCurrency(trade.maxAdverseExcursion)}
            </p>
          </div>

          <div className="space-y-1">
            <div className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
              <TrendingUp className="h-3 w-3 text-profit" />
              MFE
            </div>
            <p className="text-sm font-semibold text-profit pl-5">
              {formatCurrency(trade.maxFavorableExcursion)}
            </p>
          </div>
        </div>

        {/* Sparkline Chart Placeholder */}
        {showSparkline && trade.tradeBars && (
          <div className="pt-3 border-t">
            <div className="h-12 bg-muted/30 rounded flex items-center justify-center">
              <p className="text-xs text-muted-foreground">
                Mini chart (coming soon)
              </p>
            </div>
          </div>
        )}

        {/* View Details Button */}
        <Button
          variant="ghost"
          className="w-full mt-2"
          onClick={(e) => {
            e.stopPropagation();
            handleViewDetail();
          }}
        >
          View Full Analysis
          <ChevronRight className="ml-2 h-4 w-4" />
        </Button>
      </CardContent>
    </Card>
  );
}
