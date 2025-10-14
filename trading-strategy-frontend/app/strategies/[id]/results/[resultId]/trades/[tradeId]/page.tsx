"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { format } from "date-fns";
import {
  ArrowLeft,
  Loader2,
  AlertCircle,
  TrendingUp,
  TrendingDown,
  Clock,
  Award,
  Star,
  Download,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { TradeDetailChart } from "@/components/trades/TradeDetailChart";
import { TradeAnalysisPanel } from "@/components/trades/TradeAnalysisPanel";
import { TradeTimeline } from "@/components/trades/TradeTimeline";
import { useTradeDetail } from "@/lib/hooks/useTrades";
import { formatCurrency } from "@/lib/utils";
import type { BarData } from "@/lib/types";

interface TradeDetailPageProps {
  params: Promise<{ id: string; resultId: string; tradeId: string }>;
}

/**
 * Comprehensive trade detail page with charts, analysis, and timeline
 */
export default function TradeDetailPage({ params }: TradeDetailPageProps) {
  const router = useRouter();
  const [strategyId, setStrategyId] = useState<number>(0);
  const [resultId, setResultId] = useState<number>(0);
  const [tradeId, setTradeId] = useState<number>(0);

  useEffect(() => {
    // Unwrap params Promise
    params.then((p) => {
      setStrategyId(parseInt(p.id, 10));
      setResultId(parseInt(p.resultId, 10));
      setTradeId(parseInt(p.tradeId, 10));
    });
  }, [params]);

  const { data, loading, error } = useTradeDetail(strategyId, resultId, tradeId);

  if (strategyId === 0 || resultId === 0 || tradeId === 0) {
    return null; // Loading params
  }

  if (loading) {
    return (
      <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center space-y-4">
              <Loader2 className="h-12 w-12 animate-spin mx-auto text-primary" />
              <p className="text-lg font-medium">Loading trade details...</p>
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
              <h2 className="text-2xl font-bold">Error Loading Trade</h2>
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

  if (!data) {
    return null;
  }

  const { trade, analysis, chartData, indicatorSeries } = data;
  const isProfitable = trade.pnl > 0;
  const isLoss = trade.pnl < 0;

  // Parse trade bars from JSONB if available
  let tradeBars: BarData[] = [];
  if (trade.tradeBars) {
    try {
      const parsed = JSON.parse(trade.tradeBars);
      tradeBars = parsed.map((b: any) => ({
        timestamp: b.t,
        open: b.o,
        high: b.h,
        low: b.l,
        close: b.c,
        volume: b.v,
      }));
    } catch (e) {
      console.error("Failed to parse trade bars:", e);
    }
  }

  // Determine trade direction (would need to be passed from backend or strategy)
  // For now, assume based on P&L and price movement
  const direction: "long" | "short" =
    (trade.exitPrice ?? 0) > trade.entryPrice ? "long" : "short";

  const getQualityColor = (score: number) => {
    if (score >= 80) return "text-green-600";
    if (score >= 60) return "text-yellow-600";
    return "text-orange-600";
  };

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
              <div className="flex items-center gap-3 mb-2">
                <h1 className="text-3xl font-bold">Trade #{trade.id}</h1>
                <Badge variant={getResultBadgeVariant(trade.result)}>
                  {trade.result.toUpperCase()}
                </Badge>
                <Badge variant="outline">{direction.toUpperCase()}</Badge>
              </div>
              <p className="text-muted-foreground">
                Entered {format(new Date(trade.entryTime), "MMM dd, yyyy 'at' HH:mm")}
                {trade.exitTime &&
                  ` • Exited ${format(new Date(trade.exitTime), "MMM dd, yyyy 'at' HH:mm")}`}
              </p>
            </div>
            <Button variant="outline" size="sm">
              <Download className="mr-2 h-4 w-4" />
              Export
            </Button>
          </div>
        </div>

        {/* Key Metrics Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          {/* P&L */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">P&L</CardTitle>
              {isProfitable ? (
                <TrendingUp className="h-4 w-4 text-profit" />
              ) : (
                <TrendingDown className="h-4 w-4 text-loss" />
              )}
            </CardHeader>
            <CardContent>
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
              <p className="text-xs text-muted-foreground mt-1">
                {trade.result === "win"
                  ? "Profitable trade"
                  : trade.result === "loss"
                  ? "Loss taken"
                  : "Timed out"}
              </p>
            </CardContent>
          </Card>

          {/* Duration */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Duration</CardTitle>
              <Clock className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{trade.barsHeld}</div>
              <p className="text-xs text-muted-foreground mt-1">Bars held</p>
            </CardContent>
          </Card>

          {/* Entry Quality */}
          {trade.entryQualityScore !== null && (
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">
                  Entry Quality
                </CardTitle>
                <Star className="h-4 w-4 text-primary" />
              </CardHeader>
              <CardContent>
                <div
                  className={`text-2xl font-bold ${getQualityColor(trade.entryQualityScore)}`}
                >
                  {trade.entryQualityScore}/100
                </div>
                <p className="text-xs text-muted-foreground mt-1">
                  {trade.entryQualityScore >= 80
                    ? "Excellent setup"
                    : trade.entryQualityScore >= 60
                    ? "Good setup"
                    : "Needs improvement"}
                </p>
              </CardContent>
            </Card>
          )}

          {/* Exit Quality */}
          {trade.exitQualityScore !== null && (
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">
                  Exit Quality
                </CardTitle>
                <Award className="h-4 w-4 text-primary" />
              </CardHeader>
              <CardContent>
                <div
                  className={`text-2xl font-bold ${getQualityColor(trade.exitQualityScore)}`}
                >
                  {trade.exitQualityScore}/100
                </div>
                <p className="text-xs text-muted-foreground mt-1">
                  {trade.exitQualityScore >= 80
                    ? "Well executed"
                    : trade.exitQualityScore >= 60
                    ? "Good execution"
                    : "Could be better"}
                </p>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Trade Details Card */}
        <Card className="mb-8">
          <CardHeader>
            <CardTitle>Trade Details</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {/* Entry Info */}
              <div className="space-y-3">
                <h4 className="text-sm font-medium text-muted-foreground">
                  Entry Information
                </h4>
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm">Price</span>
                    <span className="text-sm font-mono font-semibold">
                      {formatCurrency(trade.entryPrice)}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm">Time</span>
                    <span className="text-sm font-semibold">
                      {format(new Date(trade.entryTime), "HH:mm:ss")}
                    </span>
                  </div>
                </div>
              </div>

              {/* Exit Info */}
              <div className="space-y-3">
                <h4 className="text-sm font-medium text-muted-foreground">
                  Exit Information
                </h4>
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm">Price</span>
                    <span className="text-sm font-mono font-semibold">
                      {trade.exitPrice
                        ? formatCurrency(trade.exitPrice)
                        : "N/A"}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm">Time</span>
                    <span className="text-sm font-semibold">
                      {trade.exitTime
                        ? format(new Date(trade.exitTime), "HH:mm:ss")
                        : "N/A"}
                    </span>
                  </div>
                </div>
              </div>

              {/* Excursion Info */}
              <div className="space-y-3">
                <h4 className="text-sm font-medium text-muted-foreground">
                  Excursion Analysis
                </h4>
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm">MAE</span>
                    <span className="text-sm font-semibold text-loss">
                      {formatCurrency(trade.maxAdverseExcursion)}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm">MFE</span>
                    <span className="text-sm font-semibold text-profit">
                      {formatCurrency(trade.maxFavorableExcursion)}
                    </span>
                  </div>
                  {trade.riskRewardRatio && (
                    <div className="flex justify-between">
                      <span className="text-sm">R/R Ratio</span>
                      <span className="text-sm font-semibold">
                        {trade.riskRewardRatio.toFixed(2)}
                      </span>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Tabbed Content */}
        <Tabs defaultValue="chart" className="space-y-6">
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="chart">Price Chart</TabsTrigger>
            <TabsTrigger value="analysis">AI Analysis</TabsTrigger>
            <TabsTrigger value="timeline">Timeline</TabsTrigger>
          </TabsList>

          {/* Chart Tab */}
          <TabsContent value="chart" className="space-y-6">
            {chartData && chartData.length > 0 ? (
              <TradeDetailChart
                chartData={chartData}
                trade={trade}
                direction={direction}
                indicatorSeries={indicatorSeries || undefined}
              />
            ) : (
              <Card>
                <CardContent className="p-12 text-center">
                  <AlertCircle className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
                  <p className="text-muted-foreground">
                    Chart data not available for this trade
                  </p>
                </CardContent>
              </Card>
            )}
          </TabsContent>

          {/* Analysis Tab */}
          <TabsContent value="analysis">
            {analysis ? (
              <TradeAnalysisPanel analysis={analysis} />
            ) : (
              <Card>
                <CardContent className="p-12 text-center">
                  <AlertCircle className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
                  <p className="text-muted-foreground">
                    AI analysis not available for this trade
                  </p>
                </CardContent>
              </Card>
            )}
          </TabsContent>

          {/* Timeline Tab */}
          <TabsContent value="timeline">
            {tradeBars.length > 0 ? (
              <TradeTimeline
                tradeBars={tradeBars}
                entryPrice={trade.entryPrice}
                direction={direction}
              />
            ) : (
              <Card>
                <CardContent className="p-12 text-center">
                  <AlertCircle className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
                  <p className="text-muted-foreground">
                    Timeline data not available for this trade
                  </p>
                </CardContent>
              </Card>
            )}
          </TabsContent>
        </Tabs>

        {/* Trade Notes */}
        {trade.tradeNotes && (
          <Card className="mt-8">
            <CardHeader>
              <CardTitle>Notes</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm whitespace-pre-wrap">{trade.tradeNotes}</p>
            </CardContent>
          </Card>
        )}
      </div>
    </main>
  );
}
