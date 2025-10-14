"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, Loader2, AlertCircle, Filter } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { TradeCard } from "@/components/trades/TradeCard";
import { TradeSummaryPanel } from "@/components/trades/TradeSummaryPanel";
import { useTrades } from "@/lib/hooks/useTrades";
import type { TradeFilters } from "@/lib/types";

interface TradesPageProps {
  params: Promise<{ id: string; resultId: string }>;
}

/**
 * Trades list page with filtering and pagination
 */
export default function TradesPage({ params }: TradesPageProps) {
  const router = useRouter();
  const [strategyId, setStrategyId] = useState<number | null>(null);
  const [resultId, setResultId] = useState<number | null>(null);
  const [filters, setFilters] = useState<TradeFilters>({
    page: 1,
    pageSize: 20,
    sortBy: "entryTime",
  });

  useEffect(() => {
    // Unwrap params Promise
    params.then((p) => {
      const sid = parseInt(p.id, 10);
      const rid = parseInt(p.resultId, 10);
      setStrategyId(sid);
      setResultId(rid);
    });
  }, [params]);

  const { data, loading, error, updateFilters, nextPage, previousPage, goToPage } =
    useTrades(strategyId || 0, resultId || 0, filters);

  const handleFilterChange = (key: keyof TradeFilters, value: any) => {
    updateFilters({ [key]: value, page: 1 }); // Reset to page 1 when filter changes
  };

  if (strategyId === 0 || resultId === 0) {
    return null; // Loading params
  }

  if (loading && !data) {
    return (
      <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center space-y-4">
              <Loader2 className="h-12 w-12 animate-spin mx-auto text-primary" />
              <p className="text-lg font-medium">Loading trades...</p>
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
              <h2 className="text-2xl font-bold">Error Loading Trades</h2>
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

  if (!data || data.trades.length === 0) {
    return (
      <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
          <Button
            onClick={() => router.push(`/strategies/${strategyId}/results/${resultId}`)}
            variant="ghost"
            className="mb-4"
          >
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Results
          </Button>

          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center space-y-4">
              <AlertCircle className="h-12 w-12 mx-auto text-muted-foreground" />
              <h2 className="text-2xl font-bold">No Trades Found</h2>
              <p className="text-muted-foreground">
                No trades match your current filters
              </p>
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
            onClick={() => router.push(`/strategies/${strategyId}/results/${resultId}`)}
            variant="ghost"
            className="mb-4"
          >
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Results
          </Button>

          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-3xl font-bold mb-2">Trade Analysis</h1>
              <p className="text-muted-foreground">
                Detailed view of all {data.totalCount} trades
              </p>
            </div>
            <Button
              onClick={() =>
                router.push(`/strategies/${strategyId}/results/${resultId}/heatmap`)
              }
              variant="outline"
            >
              View Heatmap
            </Button>
          </div>
        </div>

        {/* Summary Panel */}
        {data.summary && (
          <div className="mb-8">
            <TradeSummaryPanel summary={data.summary} />
          </div>
        )}

        {/* Filters */}
        <div className="mb-6 flex flex-wrap items-center gap-4">
          <div className="flex items-center gap-2">
            <Filter className="h-4 w-4 text-muted-foreground" />
            <span className="text-sm font-medium">Filters:</span>
          </div>

          {/* Result Filter */}
          <Select
            value={filters.result || "all"}
            onValueChange={(value) =>
              handleFilterChange("result", value === "all" ? undefined : value)
            }
          >
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="All Results" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Results</SelectItem>
              <SelectItem value="win">Wins Only</SelectItem>
              <SelectItem value="loss">Losses Only</SelectItem>
              <SelectItem value="timeout">Timeouts Only</SelectItem>
            </SelectContent>
          </Select>

          {/* Sort By */}
          <Select
            value={filters.sortBy || "entryTime"}
            onValueChange={(value) => handleFilterChange("sortBy", value)}
          >
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Sort By" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="entryTime">Entry Time</SelectItem>
              <SelectItem value="pnl">P&L (Highest)</SelectItem>
              <SelectItem value="duration">Duration</SelectItem>
            </SelectContent>
          </Select>

          {/* Page Size */}
          <Select
            value={String(filters.pageSize || 20)}
            onValueChange={(value) =>
              handleFilterChange("pageSize", parseInt(value, 10))
            }
          >
            <SelectTrigger className="w-[140px]">
              <SelectValue placeholder="Per Page" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="10">10 per page</SelectItem>
              <SelectItem value="20">20 per page</SelectItem>
              <SelectItem value="50">50 per page</SelectItem>
              <SelectItem value="100">100 per page</SelectItem>
            </SelectContent>
          </Select>

          {filters.result && (
            <Badge variant="secondary">
              {filters.result}: {data.totalCount} trades
              <button
                onClick={() => handleFilterChange("result", undefined)}
                className="ml-2 hover:text-destructive"
              >
                ×
              </button>
            </Badge>
          )}
        </div>

        {/* Trades Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
          {data.trades.map((trade, idx) => (
            <TradeCard
              key={trade.id}
              trade={trade}
              strategyId={strategyId}
              resultId={resultId}
              index={(data.page - 1) * data.pageSize + idx + 1}
              showSparkline={false}
            />
          ))}
        </div>

        {/* Pagination */}
        {data.totalPages > 1 && (
          <div className="flex items-center justify-between border-t pt-6">
            <div className="text-sm text-muted-foreground">
              Showing {(data.page - 1) * data.pageSize + 1} to{" "}
              {Math.min(data.page * data.pageSize, data.totalCount)} of{" "}
              {data.totalCount} trades
            </div>

            <div className="flex items-center gap-2">
              <Button
                onClick={previousPage}
                disabled={data.page === 1 || loading}
                variant="outline"
                size="sm"
              >
                Previous
              </Button>

              <div className="flex items-center gap-1">
                {Array.from({ length: Math.min(data.totalPages, 5) }, (_, i) => {
                  let pageNumber;
                  if (data.totalPages <= 5) {
                    pageNumber = i + 1;
                  } else if (data.page <= 3) {
                    pageNumber = i + 1;
                  } else if (data.page >= data.totalPages - 2) {
                    pageNumber = data.totalPages - 4 + i;
                  } else {
                    pageNumber = data.page - 2 + i;
                  }

                  return (
                    <Button
                      key={pageNumber}
                      onClick={() => goToPage(pageNumber)}
                      disabled={loading}
                      variant={pageNumber === data.page ? "default" : "outline"}
                      size="sm"
                      className="w-10"
                    >
                      {pageNumber}
                    </Button>
                  );
                })}
              </div>

              <Button
                onClick={nextPage}
                disabled={data.page >= data.totalPages || loading}
                variant="outline"
                size="sm"
              >
                Next
              </Button>
            </div>
          </div>
        )}

        {loading && (
          <div className="fixed bottom-4 right-4 bg-card border rounded-lg p-4 shadow-lg">
            <div className="flex items-center gap-3">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span className="text-sm">Loading...</span>
            </div>
          </div>
        )}
      </div>
    </main>
  );
}
