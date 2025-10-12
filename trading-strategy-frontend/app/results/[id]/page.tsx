"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, Loader2, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { ResultsSummary } from "@/components/ResultsSummary";
import { WorstTradesSection } from "@/components/WorstTradesSection";
import apiClient, { ApiClientError } from "@/lib/api-client";
import type { Strategy, StrategyResult } from "@/lib/types";

interface ResultsPageProps {
  params: Promise<{ id: string }>;
}

/**
 * Results page for displaying strategy analysis
 */
export default function ResultsPage({ params }: ResultsPageProps) {
  const router = useRouter();
  const [strategy, setStrategy] = useState<Strategy | null>(null);
  const [result, setResult] = useState<StrategyResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [strategyId, setStrategyId] = useState<string>("");

  useEffect(() => {
    // Unwrap params Promise
    params.then((p) => {
      setStrategyId(p.id);
      loadStrategyResults(p.id);
    });
  }, [params]);

  const loadStrategyResults = async (id: string) => {
    try {
      setLoading(true);
      setError(null);

      console.log("[ResultsPage] Loading strategy:", id);

      // Validate and parse ID
      const strategyId = parseInt(id, 10);
      if (isNaN(strategyId) || strategyId <= 0) {
        throw new Error(`Invalid strategy ID: ${id}`);
      }

      // Load strategy details
      const strategyData = await apiClient.getStrategy(strategyId);
      setStrategy(strategyData);

      // Get the latest result for this strategy
      if (strategyData.results && strategyData.results.length > 0) {
        const latestResult = strategyData.results[0];
        setResult(latestResult);
      } else {
        setError("No results found for this strategy");
      }

      console.log("[ResultsPage] Strategy loaded successfully");
    } catch (err) {
      console.error("[ResultsPage] Error loading strategy:", err);

      if (err instanceof ApiClientError) {
        setError(err.detail || err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to load strategy results");
      }
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center space-y-4">
              <Loader2 className="h-12 w-12 animate-spin mx-auto text-primary" />
              <p className="text-lg font-medium">Loading strategy results...</p>
              <p className="text-sm text-muted-foreground">
                Please wait while we retrieve your analysis
              </p>
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
              <h2 className="text-2xl font-bold">Error Loading Results</h2>
              <p className="text-muted-foreground">{error}</p>
              <div className="flex gap-3 justify-center">
                <Button onClick={() => router.push("/")} variant="outline">
                  <ArrowLeft className="mr-2 h-4 w-4" />
                  Back to Home
                </Button>
                <Button onClick={() => loadStrategyResults(strategyId)}>
                  Try Again
                </Button>
              </div>
            </div>
          </div>
        </div>
      </main>
    );
  }

  if (!strategy || !result) {
    return (
      <main className="min-h-screen bg-gradient-to-b from-background to-muted/20">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center space-y-4">
              <AlertCircle className="h-12 w-12 mx-auto text-muted-foreground" />
              <h2 className="text-2xl font-bold">No Results Found</h2>
              <p className="text-muted-foreground">
                This strategy hasn't been analyzed yet
              </p>
              <Button onClick={() => router.push("/")} variant="outline">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back to Home
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
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Analyzer
          </Button>

          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-3xl font-bold mb-2">{strategy.name}</h1>
              <p className="text-muted-foreground">
                {strategy.description || "No description provided"}
              </p>
              <div className="flex gap-4 mt-3 text-sm">
                <span className="px-3 py-1 bg-primary/10 rounded-full">
                  {strategy.symbol || "Unknown Symbol"}
                </span>
                <span className="px-3 py-1 bg-muted rounded-full">
                  {strategy.direction?.toUpperCase() || "UNKNOWN"}
                </span>
                <span className="px-3 py-1 bg-muted rounded-full">
                  {result.totalTrades} trades
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Results Summary */}
        <div className="mb-12">
          <ResultsSummary result={result} loading={false} error={null} />
        </div>

        {/* Worst Trades Section */}
        {result.worstTrades && result.worstTrades.length > 0 && (
          <div className="mb-12">
            <WorstTradesSection
              trades={result.worstTrades}
              insights={result.insights}
              loading={false}
              error={null}
            />
          </div>
        )}

        {/* Strategy Details */}
        <div className="space-y-6">
          <h2 className="text-2xl font-bold">Strategy Configuration</h2>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Entry Conditions */}
            {strategy.entryConditions && strategy.entryConditions.length > 0 && (
              <div className="p-6 border rounded-lg bg-card">
                <h3 className="font-semibold mb-3">Entry Conditions</h3>
                <ul className="space-y-2">
                  {strategy.entryConditions.map((condition, idx) => (
                    <li
                      key={idx}
                      className="text-sm flex items-start gap-2"
                    >
                      <span className="text-primary">•</span>
                      <span>
                        {condition.indicator} {condition.operator}{" "}
                        {condition.value}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {/* Exit Rules */}
            <div className="p-6 border rounded-lg bg-card">
              <h3 className="font-semibold mb-3">Exit Rules</h3>
              <div className="space-y-3 text-sm">
                {strategy.stopLoss && (
                  <div>
                    <span className="font-medium text-loss">Stop Loss:</span>{" "}
                    {strategy.stopLoss.value} {strategy.stopLoss.type}
                  </div>
                )}
                {strategy.takeProfit && (
                  <div>
                    <span className="font-medium text-profit">
                      Take Profit:
                    </span>{" "}
                    {strategy.takeProfit.value} {strategy.takeProfit.type}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>
  );
}
