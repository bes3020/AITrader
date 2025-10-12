"use client";

import { useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { format, subMonths } from "date-fns";
import { Calendar as CalendarIcon, Loader2 } from "lucide-react";
import { DateRange } from "react-day-picker";

import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import apiClient, { ApiClientError } from "@/lib/api-client";
import type { AnalyzeStrategyRequest } from "@/lib/types";
import { SymbolSelector } from "./SymbolSelector";

interface StrategyFormProps {
  /**
   * Optional callback when analysis starts
   */
  onAnalysisStart?: () => void;

  /**
   * Optional callback when analysis completes successfully
   */
  onAnalysisComplete?: (strategyId: number) => void;
}

export function StrategyForm({ onAnalysisStart, onAnalysisComplete }: StrategyFormProps = {}) {
  const router = useRouter();

  // Form state
  const [description, setDescription] = useState("");
  const [symbol, setSymbol] = useState<string>("ES");
  const [dateRange, setDateRange] = useState<DateRange | undefined>({
    from: subMonths(new Date(), 6),
    to: new Date(),
  });

  // UI state
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  /**
   * Validates form inputs before submission
   */
  const validateForm = useCallback((): string | null => {
    // Validate description
    if (!description.trim()) {
      return "Please enter a strategy description";
    }

    if (description.trim().length < 10) {
      return "Strategy description must be at least 10 characters";
    }

    // Validate date range
    if (!dateRange?.from || !dateRange?.to) {
      return "Please select a valid date range";
    }

    if (dateRange.from >= dateRange.to) {
      return "End date must be after start date";
    }

    if (dateRange.to > new Date()) {
      return "End date cannot be in the future";
    }

    // Validate symbol
    if (!symbol) {
      return "Please select a symbol";
    }

    return null;
  }, [description, dateRange, symbol]);

  /**
   * Handles form submission
   */
  const handleSubmit = useCallback(
    async (e: React.FormEvent<HTMLFormElement>) => {
      e.preventDefault();

      // Clear previous state
      setError(null);
      setSuccess(false);

      // Validate inputs
      const validationError = validateForm();
      if (validationError) {
        setError(validationError);
        return;
      }

      try {
        setLoading(true);
        onAnalysisStart?.();

        // Prepare request
        const request: AnalyzeStrategyRequest = {
          description: description.trim(),
          symbol,
          startDate: dateRange!.from!.toISOString(),
          endDate: dateRange!.to!.toISOString(),
        };

        console.log("[StrategyForm] Submitting analysis request:", {
          symbol: request.symbol,
          descriptionLength: request.description.length,
          dateRange: `${format(dateRange!.from!, "yyyy-MM-dd")} to ${format(dateRange!.to!, "yyyy-MM-dd")}`,
        });

        // Call API
        const response = await apiClient.analyzeStrategy(request);

        console.log("[StrategyForm] Analysis completed:", {
          strategyId: response.strategy.id,
          totalTrades: response.result.totalTrades,
          winRate: response.result.winRate,
          elapsed: response.elapsedMilliseconds,
        });

        // Validate strategy ID before redirecting
        const strategyId = response.strategy.id;
        if (!strategyId || strategyId === 0) {
          throw new Error("Invalid strategy ID returned from API");
        }

        // Show success message
        setSuccess(true);
        onAnalysisComplete?.(strategyId);

        // Redirect to results page after short delay
        setTimeout(() => {
          router.push(`/results/${strategyId}`);
        }, 1500);
      } catch (err) {
        console.error("[StrategyForm] Analysis failed:", err);

        if (err instanceof ApiClientError) {
          setError(err.detail || err.message);
        } else if (err instanceof Error) {
          setError(err.message);
        } else {
          setError("An unexpected error occurred. Please try again.");
        }
      } finally {
        setLoading(false);
      }
    },
    [description, symbol, dateRange, validateForm, onAnalysisStart, onAnalysisComplete, router]
  );

  /**
   * Formats the selected date range for display
   */
  const formatDateRange = useCallback(() => {
    if (!dateRange?.from) {
      return <span>Pick a date range</span>;
    }

    if (dateRange.to) {
      return (
        <>
          {format(dateRange.from, "LLL dd, y")} -{" "}
          {format(dateRange.to, "LLL dd, y")}
        </>
      );
    }

    return format(dateRange.from, "LLL dd, y");
  }, [dateRange]);

  return (
    <Card className="w-full max-w-3xl mx-auto">
      <CardHeader>
        <CardTitle>Analyze Trading Strategy</CardTitle>
        <CardDescription>
          Describe your trading strategy in natural language and we'll analyze its historical performance
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Strategy Description */}
          <div className="space-y-2">
            <Label htmlFor="description" className="text-base font-medium">
              Strategy Description
              <span className="text-destructive ml-1">*</span>
            </Label>
            <Textarea
              id="description"
              placeholder="Example: Buy when price crosses above VWAP and volume is greater than 1.5x average, with stop at 10 points and target at 20 points"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              disabled={loading}
              rows={10}
              className="resize-y text-base"
              aria-required="true"
              aria-invalid={!!error && !description.trim()}
              aria-describedby="description-hint"
            />
            <p id="description-hint" className="text-sm text-muted-foreground">
              Be specific about entry conditions, stop loss, and take profit levels
            </p>
          </div>

          {/* Symbol Selection */}
          <SymbolSelector
            value={symbol}
            onChange={setSymbol}
            disabled={loading}
          />

          {/* Date Range Picker */}
          <div className="space-y-2">
            <Label className="text-base font-medium">
              Backtest Date Range
              <span className="text-destructive ml-1">*</span>
            </Label>
            <Popover>
              <PopoverTrigger asChild>
                <Button
                  id="date-range"
                  variant="outline"
                  className={cn(
                    "w-full justify-start text-left font-normal",
                    !dateRange && "text-muted-foreground"
                  )}
                  disabled={loading}
                  aria-required="true"
                >
                  <CalendarIcon className="mr-2 h-4 w-4" />
                  {formatDateRange()}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0" align="start">
                <Calendar
                  initialFocus
                  mode="range"
                  defaultMonth={dateRange?.from}
                  selected={dateRange}
                  onSelect={setDateRange}
                  numberOfMonths={2}
                  disabled={(date) => date > new Date() || date < new Date("2020-01-01")}
                />
              </PopoverContent>
            </Popover>
            <p className="text-sm text-muted-foreground">
              Select a date range for backtesting (default: last 6 months)
            </p>
          </div>

          {/* Error Message */}
          {error && (
            <div
              className="rounded-md bg-destructive/15 border border-destructive p-4"
              role="alert"
              aria-live="polite"
            >
              <div className="flex items-start gap-3">
                <div className="flex-shrink-0 mt-0.5">
                  <svg
                    className="h-5 w-5 text-destructive"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    aria-hidden="true"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z"
                      clipRule="evenodd"
                    />
                  </svg>
                </div>
                <div>
                  <h3 className="text-sm font-medium text-destructive">Error</h3>
                  <p className="text-sm text-destructive/90 mt-1">{error}</p>
                </div>
              </div>
            </div>
          )}

          {/* Success Message */}
          {success && (
            <div
              className="rounded-md bg-profit-light border border-profit p-4"
              role="status"
              aria-live="polite"
            >
              <div className="flex items-start gap-3">
                <div className="flex-shrink-0 mt-0.5">
                  <svg
                    className="h-5 w-5 text-profit"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    aria-hidden="true"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z"
                      clipRule="evenodd"
                    />
                  </svg>
                </div>
                <div>
                  <h3 className="text-sm font-medium text-profit-dark">Success</h3>
                  <p className="text-sm text-profit-dark/90 mt-1">
                    Strategy analyzed successfully! Redirecting to results...
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Submit Button */}
          <Button
            type="submit"
            size="lg"
            className="w-full"
            disabled={loading}
          >
            {loading ? (
              <>
                <Loader2 className="mr-2 h-5 w-5 animate-spin" />
                Analyzing Strategy...
              </>
            ) : (
              "Analyze Strategy"
            )}
          </Button>

          {loading && (
            <p className="text-sm text-center text-muted-foreground">
              This may take 1-2 minutes depending on the date range and complexity
            </p>
          )}
        </form>
      </CardContent>
    </Card>
  );
}
