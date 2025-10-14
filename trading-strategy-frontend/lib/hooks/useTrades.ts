"use client";

import { useEffect, useState, useCallback } from "react";
import apiClient, { ApiClientError } from "@/lib/api-client";
import type {
  TradeListResponse,
  TradeDetailResponse,
  TradePattern,
  HeatmapData,
  TradeFilters,
} from "@/lib/types";

/**
 * Hook for managing trade list data with pagination and filtering
 */
export function useTrades(
  strategyId: number,
  resultId: number,
  initialFilters?: TradeFilters
) {
  const [data, setData] = useState<TradeListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState<TradeFilters>(initialFilters || {});

  const loadTrades = useCallback(async () => {
    // Don't load if IDs are invalid
    if (!strategyId || !resultId || strategyId <= 0 || resultId <= 0) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);

      console.log("[useTrades] Loading trades:", {
        strategyId,
        resultId,
        filters,
      });

      const response = await apiClient.getTrades(strategyId, resultId, filters);
      setData(response);

      console.log("[useTrades] Trades loaded successfully:", {
        count: response.trades.length,
        totalCount: response.totalCount,
      });
    } catch (err) {
      console.error("[useTrades] Error loading trades:", err);

      if (err instanceof ApiClientError) {
        setError(err.detail || err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to load trades");
      }
    } finally {
      setLoading(false);
    }
  }, [strategyId, resultId, filters]);

  useEffect(() => {
    loadTrades();
  }, [loadTrades]);

  const updateFilters = useCallback((newFilters: Partial<TradeFilters>) => {
    setFilters((prev) => ({ ...prev, ...newFilters }));
  }, []);

  const nextPage = useCallback(() => {
    if (data && data.page < data.totalPages) {
      updateFilters({ page: data.page + 1 });
    }
  }, [data, updateFilters]);

  const previousPage = useCallback(() => {
    if (data && data.page > 1) {
      updateFilters({ page: data.page - 1 });
    }
  }, [data, updateFilters]);

  const goToPage = useCallback(
    (page: number) => {
      if (data && page >= 1 && page <= data.totalPages) {
        updateFilters({ page });
      }
    },
    [data, updateFilters]
  );

  return {
    data,
    loading,
    error,
    filters,
    updateFilters,
    nextPage,
    previousPage,
    goToPage,
    reload: loadTrades,
  };
}

/**
 * Hook for managing individual trade detail data
 */
export function useTradeDetail(
  strategyId: number,
  resultId: number,
  tradeId: number
) {
  const [data, setData] = useState<TradeDetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadTradeDetail = useCallback(async () => {
    // Don't load if IDs are invalid
    if (!strategyId || !resultId || !tradeId || strategyId <= 0 || resultId <= 0 || tradeId <= 0) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);

      console.log("[useTradeDetail] Loading trade detail:", {
        strategyId,
        resultId,
        tradeId,
      });

      const response = await apiClient.getTradeDetail(
        strategyId,
        resultId,
        tradeId
      );
      setData(response);

      console.log("[useTradeDetail] Trade detail loaded successfully");
    } catch (err) {
      console.error("[useTradeDetail] Error loading trade detail:", err);

      if (err instanceof ApiClientError) {
        setError(err.detail || err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to load trade detail");
      }
    } finally {
      setLoading(false);
    }
  }, [strategyId, resultId, tradeId]);

  useEffect(() => {
    loadTradeDetail();
  }, [loadTradeDetail]);

  return {
    data,
    loading,
    error,
    reload: loadTradeDetail,
  };
}

/**
 * Hook for managing trade patterns data
 */
export function useTradePatterns(strategyId: number, resultId: number) {
  const [data, setData] = useState<TradePattern[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadPatterns = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      console.log("[useTradePatterns] Loading patterns:", {
        strategyId,
        resultId,
      });

      const response = await apiClient.getTradePatterns(strategyId, resultId);
      setData(response);

      console.log("[useTradePatterns] Patterns loaded successfully:", {
        count: response.length,
      });
    } catch (err) {
      console.error("[useTradePatterns] Error loading patterns:", err);

      if (err instanceof ApiClientError) {
        setError(err.detail || err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to load trade patterns");
      }
    } finally {
      setLoading(false);
    }
  }, [strategyId, resultId]);

  useEffect(() => {
    loadPatterns();
  }, [loadPatterns]);

  return {
    data,
    loading,
    error,
    reload: loadPatterns,
  };
}

/**
 * Hook for managing heatmap data
 */
export function useHeatmap(
  strategyId: number,
  resultId: number,
  dimension: string = "hour"
) {
  const [data, setData] = useState<HeatmapData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentDimension, setCurrentDimension] = useState(dimension);

  const loadHeatmap = useCallback(async () => {
    // Don't load if IDs are invalid
    if (!strategyId || !resultId || strategyId <= 0 || resultId <= 0) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);

      console.log("[useHeatmap] Loading heatmap:", {
        strategyId,
        resultId,
        dimension: currentDimension,
      });

      const response = await apiClient.getHeatmap(
        strategyId,
        resultId,
        currentDimension
      );
      setData(response);

      console.log("[useHeatmap] Heatmap loaded successfully:", {
        dimension: response.dimension,
        cellCount: response.cells.length,
      });
    } catch (err) {
      console.error("[useHeatmap] Error loading heatmap:", err);

      if (err instanceof ApiClientError) {
        setError(err.detail || err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to load heatmap");
      }
    } finally {
      setLoading(false);
    }
  }, [strategyId, resultId, currentDimension]);

  useEffect(() => {
    loadHeatmap();
  }, [loadHeatmap]);

  const changeDimension = useCallback((newDimension: string) => {
    setCurrentDimension(newDimension);
  }, []);

  return {
    data,
    loading,
    error,
    dimension: currentDimension,
    changeDimension,
    reload: loadHeatmap,
  };
}
