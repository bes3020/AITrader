"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { format } from "date-fns";
import {
  TrendingUp,
  TrendingDown,
  Loader2,
  AlertCircle,
  Calendar,
  BarChart3,
  Home,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import apiClient, { ApiClientError } from "@/lib/api-client";
import { formatCurrency } from "@/lib/utils";
import type { Strategy } from "@/lib/types";

/**
 * Strategy history page showing all analyzed strategies
 */
export default function HistoryPage() {
  const router = useRouter();
  const [strategies, setStrategies] = useState<Strategy[]>([]);
  const [filteredStrategies, setFilteredStrategies] = useState<Strategy[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [symbolFilter, setSymbolFilter] = useState<string>("all");

  useEffect(() => {
    loadStrategies();
  }, []);

  useEffect(() => {
    // Apply symbol filter
    if (symbolFilter === "all") {
      setFilteredStrategies(strategies);
    } else {
      setFilteredStrategies(
        strategies.filter((s) => s.symbol === symbolFilter)
      );
    }
  }, [symbolFilter, strategies]);

  const loadStrategies = async () => {
    try {
      setLoading(true);
      setError(null);

      console.log("[HistoryPage] Loading all strategies");

      const data = await apiClient.listStrategies();

      // Sort by most recent first
      const sorted = data.sort((a, b) => {
        const aDate = a.results?.[0]?.createdAt || a.createdAt || "";
        const bDate = b.results?.[0]?.createdAt || b.createdAt || "";
        return new Date(bDate).getTime() - new Date(aDate).getTime();
      });

      setStrategies(sorted);
      setFilteredStrategies(sorted);

      console.log("[HistoryPage] Strategies loaded:", sorted.length);
    } catch (err) {
      console.error("[HistoryPage] Error loading strategies:", err);

      if (err instanceof ApiClientError) {
        setError(err.detail || err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to load strategies");
      }
    } finally {
      setLoading(false);
    }
  };

  const handleViewResults = (strategy: Strategy) => {
    // Navigate to results page if strategy has been analyzed
    // Otherwise, navigate to home to re-analyze
    if (strategy.results && strategy.results.length > 0) {
      router.push(`/results/${strategy.id}`);
    } else {
      // Strategy exists but hasn't been analyzed yet
      // Could redirect to analyze page or show a message
      console.log("[HistoryPage] Strategy has no results:", strategy.id);
      router.push(`/results/${strategy.id}`);
    }
  };

  const getSymbols = (): string[] => {
    const symbols = new Set(
      strategies
        .map((s) => s.symbol)
        .filter((symbol): symbol is string => Boolean(symbol))
    );
    return Array.from(symbols).sort();
  };

  if (loading) {
    return (
      <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center space-y-4">
              <Loader2 className="h-12 w-12 animate-spin mx-auto text-primary" />
              <p className="text-lg font-medium">Loading strategy history...</p>
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
              <h2 className="text-2xl font-bold">Error Loading History</h2>
              <p className="text-muted-foreground">{error}</p>
              <div className="flex gap-3 justify-center">
                <Button onClick={() => router.push("/")} variant="outline">
                  <Home className="mr-2 h-4 w-4" />
                  Back to Home
                </Button>
                <Button onClick={loadStrategies}>Try Again</Button>
              </div>
            </div>
          </div>
        </div>
      </main>
    );
  }

  if (strategies.length === 0) {
    return (
      <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
          <Button
            onClick={() => router.push("/")}
            variant="ghost"
            className="mb-4"
          >
            <Home className="mr-2 h-4 w-4" />
            Back to Analyzer
          </Button>

          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center space-y-4">
              <BarChart3 className="h-12 w-12 mx-auto text-muted-foreground" />
              <h2 className="text-2xl font-bold">No Strategies Yet</h2>
              <p className="text-muted-foreground">
                You haven't analyzed any strategies yet. Start by creating your
                first strategy!
              </p>
              <Button onClick={() => router.push("/")}>
                Analyze Strategy
              </Button>
            </div>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
      <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8">
          <Button
            onClick={() => router.push("/")}
            variant="ghost"
            className="mb-4"
          >
            <Home className="mr-2 h-4 w-4" />
            Back to Analyzer
          </Button>

          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-3xl font-bold mb-2">Strategy History</h1>
              <p className="text-muted-foreground">
                Browse and analyze your {strategies.length} previously tested
                strategies
              </p>
            </div>

            {/* Symbol Filter */}
            <Select value={symbolFilter} onValueChange={setSymbolFilter}>
              <SelectTrigger className="w-[200px]">
                <SelectValue placeholder="Filter by symbol" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Symbols</SelectItem>
                {getSymbols().map((symbol) => (
                  <SelectItem key={symbol} value={symbol}>
                    {symbol}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Summary Stats */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium">
                Total Strategies
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{strategies.length}</div>
              <p className="text-xs text-muted-foreground mt-1">
                Analyzed strategies
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium">Symbols</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{getSymbols().length}</div>
              <p className="text-xs text-muted-foreground mt-1">
                Different instruments
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium">
                Profitable Strategies
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-profit">
                {
                  strategies.filter(
                    (s) => s.results?.[0]?.totalPnl && s.results[0].totalPnl > 0
                  ).length
                }
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                With positive P&L
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium">
                Best Win Rate
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-profit">
                {Math.max(
                  0,
                  ...strategies.map((s) => s.results?.[0]?.winRate || 0)
                ).toFixed(1)}
                %
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                Highest achieved
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Strategies Grid */}
        {filteredStrategies.length === 0 ? (
          <div className="text-center py-12">
            <AlertCircle className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
            <h3 className="text-xl font-semibold mb-2">No strategies found</h3>
            <p className="text-muted-foreground">
              No strategies match the selected filter
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {filteredStrategies.map((strategy) => {
              const latestResult = strategy.results?.[0];
              const isProfitable = latestResult?.totalPnl
                ? latestResult.totalPnl > 0
                : false;
              const isLoss = latestResult?.totalPnl
                ? latestResult.totalPnl < 0
                : false;

              return (
                <Card
                  key={strategy.id}
                  className="hover:border-primary transition-colors cursor-pointer"
                  onClick={() => handleViewResults(strategy)}
                >
                  <CardHeader>
                    <div className="flex items-start justify-between">
                      <div className="flex-1 min-w-0">
                        <CardTitle className="text-lg mb-2 truncate">
                          {strategy.name}
                        </CardTitle>
                        <p className="text-sm text-muted-foreground line-clamp-2">
                          {strategy.description || "No description provided"}
                        </p>
                      </div>
                      {latestResult && (
                        <div className="ml-4">
                          {isProfitable ? (
                            <TrendingUp className="h-6 w-6 text-profit" />
                          ) : isLoss ? (
                            <TrendingDown className="h-6 w-6 text-loss" />
                          ) : null}
                        </div>
                      )}
                    </div>
                  </CardHeader>

                  <CardContent className="space-y-4">
                    {/* Strategy Info */}
                    <div className="flex items-center gap-2 flex-wrap">
                      <Badge variant="outline">{strategy.symbol}</Badge>
                      <Badge variant="secondary">
                        {strategy.direction?.toUpperCase()}
                      </Badge>
                      {latestResult && (
                        <Badge variant="outline">
                          {latestResult.totalTrades} trades
                        </Badge>
                      )}
                    </div>

                    {/* Performance Metrics */}
                    {latestResult && (
                      <div className="grid grid-cols-2 gap-4 pt-4 border-t">
                        <div>
                          <p className="text-xs text-muted-foreground mb-1">
                            Total P&L
                          </p>
                          <p
                            className={`text-lg font-bold ${
                              isProfitable
                                ? "text-profit"
                                : isLoss
                                ? "text-loss"
                                : "text-muted-foreground"
                            }`}
                          >
                            {formatCurrency(latestResult.totalPnl)}
                          </p>
                        </div>
                        <div>
                          <p className="text-xs text-muted-foreground mb-1">
                            Win Rate
                          </p>
                          <p className="text-lg font-bold">
                            {latestResult.winRate.toFixed(1)}%
                          </p>
                        </div>
                        <div>
                          <p className="text-xs text-muted-foreground mb-1">
                            Avg Win
                          </p>
                          <p
                            className={`text-sm font-semibold ${
                              latestResult.avgWin > 0
                                ? "text-profit"
                                : "text-muted-foreground"
                            }`}
                          >
                            {formatCurrency(latestResult.avgWin)}
                          </p>
                        </div>
                        <div>
                          <p className="text-xs text-muted-foreground mb-1">
                            Max Drawdown
                          </p>
                          <p className="text-sm font-semibold text-loss">
                            {formatCurrency(latestResult.maxDrawdown)}
                          </p>
                        </div>
                      </div>
                    )}

                    {/* Date */}
                    <div className="flex items-center gap-2 text-xs text-muted-foreground pt-3 border-t">
                      <Calendar className="h-3 w-3" />
                      <span>
                        Analyzed{" "}
                        {latestResult?.createdAt
                          ? format(
                              new Date(latestResult.createdAt),
                              "MMM dd, yyyy"
                            )
                          : strategy.createdAt
                          ? format(new Date(strategy.createdAt), "MMM dd, yyyy")
                          : "Unknown"}
                      </span>
                    </div>

                    {/* View Button */}
                    <Button
                      className="w-full"
                      variant="outline"
                      onClick={(e) => {
                        e.stopPropagation();
                        handleViewResults(strategy);
                      }}
                    >
                      View Results
                      <BarChart3 className="ml-2 h-4 w-4" />
                    </Button>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
      </div>
    </main>
  );
}
