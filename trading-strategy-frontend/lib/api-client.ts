import axios, { AxiosError, AxiosInstance, AxiosResponse } from "axios";
import type {
  AnalyzeStrategyRequest,
  AnalyzeStrategyResponse,
  RefineStrategyRequest,
  Strategy,
  SymbolInfo,
  TradeListResponse,
  TradeDetailResponse,
  TradePattern,
  HeatmapData,
  TradeFilters,
} from "./types";

/**
 * API error response structure
 */
interface ApiError {
  title: string;
  detail: string;
  status: number;
}

/**
 * Custom error class for API errors
 */
export class ApiClientError extends Error {
  public status: number;
  public detail: string;

  constructor(message: string, status: number, detail: string) {
    super(message);
    this.name = "ApiClientError";
    this.status = status;
    this.detail = detail;
  }
}

/**
 * Trading Strategy API Client
 * Type-safe client for interacting with the Trading Strategy API
 */
class TradingStrategyApiClient {
  private client: AxiosInstance;
  private authToken: string | null = null;

  constructor() {
    const baseURL =
      process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

    this.client = axios.create({
      baseURL,
      headers: {
        "Content-Type": "application/json",
      },
      timeout: 120000, // 2 minutes for long-running strategy analysis
    });

    // Request interceptor for authentication
    this.client.interceptors.request.use(
      (config) => {
        if (this.authToken) {
          config.headers.Authorization = `Bearer ${this.authToken}`;
        }
        return config;
      },
      (error) => {
        console.error("[API Client] Request interceptor error:", error);
        return Promise.reject(error);
      }
    );

    // Response interceptor for error handling
    this.client.interceptors.response.use(
      (response: AxiosResponse) => {
        return response;
      },
      (error: AxiosError<ApiError>) => {
        if (error.response) {
          // Server responded with error status
          const { status, data } = error.response;
          const errorMessage =
            data?.title || error.message || "An error occurred";
          const errorDetail =
            data?.detail || "Please try again or contact support";

          console.error("[API Client] HTTP Error:", {
            status,
            message: errorMessage,
            detail: errorDetail,
            url: error.config?.url,
          });

          throw new ApiClientError(errorMessage, status, errorDetail);
        } else if (error.request) {
          // Request made but no response received
          console.error("[API Client] Network Error:", {
            message: "No response received from server",
            error: error.message,
          });

          throw new ApiClientError(
            "Network error",
            0,
            "Unable to reach the server. Please check your connection."
          );
        } else {
          // Error setting up the request
          console.error("[API Client] Request Setup Error:", error.message);

          throw new ApiClientError(
            "Request error",
            0,
            "An error occurred while preparing the request"
          );
        }
      }
    );
  }

  /**
   * Sets the authentication token for API requests
   * @param token - JWT or Bearer token
   */
  public setAuthToken(token: string | null): void {
    this.authToken = token;
  }

  /**
   * Analyzes a trading strategy from natural language description
   * @param request - Strategy description, symbol, and backtest date range
   * @returns Strategy analysis with performance metrics and AI insights
   */
  public async analyzeStrategy(
    request: AnalyzeStrategyRequest
  ): Promise<AnalyzeStrategyResponse> {
    try {
      console.log("[API Client] Analyzing strategy:", {
        symbol: request.symbol,
        startDate: request.startDate,
        endDate: request.endDate,
        descriptionLength: request.description.length,
      });

      const response = await this.client.post<AnalyzeStrategyResponse>(
        "/api/strategy/analyze",
        request
      );

      console.log("[API Client] Strategy analysis completed:", {
        totalTrades: response.data.result.totalTrades,
        winRate: response.data.result.winRate,
        elapsed: response.data.elapsedMilliseconds,
        aiProvider: response.data.aiProvider,
      });

      return response.data;
    } catch (error) {
      console.error("[API Client] analyzeStrategy failed:", error);
      throw error;
    }
  }

  /**
   * Retrieves a strategy by ID
   * @param id - Strategy ID
   * @returns Strategy details with conditions, stop loss, and take profit
   */
  public async getStrategy(id: number): Promise<Strategy> {
    try {
      console.log("[API Client] Fetching strategy:", id);

      const response = await this.client.get<Strategy>(
        `/api/strategy/${id}`
      );

      console.log("[API Client] Strategy retrieved:", {
        id: response.data.id,
        name: response.data.name,
        symbol: response.data.symbol,
      });

      return response.data;
    } catch (error) {
      console.error("[API Client] getStrategy failed:", error);
      throw error;
    }
  }

  /**
   * Saves a strategy to the database
   * @param strategy - Strategy to save
   * @returns Saved strategy with generated ID
   */
  public async saveStrategy(strategy: Strategy): Promise<Strategy> {
    try {
      console.log("[API Client] Saving strategy:", {
        name: strategy.name,
        direction: strategy.direction,
        symbol: strategy.symbol,
        conditionsCount: strategy.entryConditions.length,
      });

      const response = await this.client.post<Strategy>(
        "/api/strategy/save",
        strategy
      );

      console.log("[API Client] Strategy saved successfully:", {
        id: response.data.id,
        name: response.data.name,
      });

      return response.data;
    } catch (error) {
      console.error("[API Client] saveStrategy failed:", error);
      throw error;
    }
  }

  /**
   * Lists all strategies, optionally filtered by symbol
   * @param symbol - Optional symbol filter (ES, NQ, YM, BTC, CL)
   * @returns Array of user's strategies
   */
  public async listStrategies(symbol?: string): Promise<Strategy[]> {
    try {
      console.log("[API Client] Listing strategies:", {
        symbol: symbol || "all",
      });

      const params = symbol ? { symbol } : {};
      const response = await this.client.get<Strategy[]>(
        "/api/strategy/list",
        { params }
      );

      console.log("[API Client] Strategies retrieved:", {
        count: response.data.length,
        symbol: symbol || "all",
      });

      return response.data;
    } catch (error) {
      console.error("[API Client] listStrategies failed:", error);
      throw error;
    }
  }

  /**
   * Refines an existing strategy by adding new conditions
   * @param request - Original strategy ID, additional condition, symbol, and date range
   * @returns Refined strategy analysis with comparison to original
   */
  public async refineStrategy(
    request: RefineStrategyRequest
  ): Promise<AnalyzeStrategyResponse> {
    try {
      console.log("[API Client] Refining strategy:", {
        strategyId: request.strategyId,
        additionalCondition: request.additionalCondition,
        symbol: request.symbol,
      });

      const response = await this.client.post<AnalyzeStrategyResponse>(
        "/api/strategy/refine",
        request
      );

      console.log("[API Client] Strategy refinement completed:", {
        totalTrades: response.data.result.totalTrades,
        winRate: response.data.result.winRate,
        elapsed: response.data.elapsedMilliseconds,
      });

      return response.data;
    } catch (error) {
      console.error("[API Client] refineStrategy failed:", error);
      throw error;
    }
  }

  /**
   * Gets a list of all supported futures symbols with their specifications
   * @returns Array of symbol information with metadata and available data ranges
   */
  public async getSymbols(): Promise<SymbolInfo[]> {
    try {
      console.log("[API Client] Fetching supported symbols");

      const response = await this.client.get<SymbolInfo[]>(
        "/api/strategy/symbols"
      );

      console.log("[API Client] Symbols retrieved:", {
        count: response.data.length,
        symbols: response.data.map((s) => s.symbol).join(", "),
      });

      return response.data;
    } catch (error) {
      console.error("[API Client] getSymbols failed:", error);
      throw error;
    }
  }

  /**
   * Deletes a strategy by ID
   * @param id - Strategy ID to delete
   */
  public async deleteStrategy(id: number): Promise<void> {
    try {
      console.log("[API Client] Deleting strategy:", id);

      await this.client.delete(`/api/strategy/${id}`);

      console.log("[API Client] Strategy deleted successfully:", id);
    } catch (error) {
      console.error("[API Client] deleteStrategy failed:", error);
      throw error;
    }
  }

  /**
   * Gets detailed strategy information
   * @param id - Strategy ID
   * @returns Detailed strategy with versions and results
   */
  public async getStrategyDetail(id: number): Promise<any> {
    try {
      console.log("[API Client] Fetching strategy detail:", id);
      const response = await this.client.get(`/api/strategy/${id}/detail`);
      console.log("[API Client] Strategy detail retrieved:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] getStrategyDetail failed:", error);
      throw error;
    }
  }

  /**
   * Updates an existing strategy
   * @param id - Strategy ID
   * @param data - Update data
   * @returns Updated strategy
   */
  public async updateStrategy(id: number, data: any): Promise<any> {
    try {
      console.log("[API Client] Updating strategy:", id);
      const response = await this.client.put(`/api/strategy/${id}`, data);
      console.log("[API Client] Strategy updated:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] updateStrategy failed:", error);
      throw error;
    }
  }

  /**
   * Duplicates a strategy
   * @param id - Strategy ID to duplicate
   * @param newName - Name for the duplicate
   * @returns Duplicated strategy
   */
  public async duplicateStrategy(id: number, newName: string): Promise<any> {
    try {
      console.log("[API Client] Duplicating strategy:", id, newName);
      const response = await this.client.post(`/api/strategy/${id}/duplicate`, { newName });
      console.log("[API Client] Strategy duplicated:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] duplicateStrategy failed:", error);
      throw error;
    }
  }

  /**
   * Creates a new version of a strategy
   * @param id - Parent strategy ID
   * @param data - Version data
   * @returns New version
   */
  public async createVersion(id: number, data: any): Promise<any> {
    try {
      console.log("[API Client] Creating version:", id);
      const response = await this.client.post(`/api/strategy/${id}/version`, data);
      console.log("[API Client] Version created:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] createVersion failed:", error);
      throw error;
    }
  }

  /**
   * Gets all versions of a strategy
   * @param id - Strategy ID
   * @returns List of versions
   */
  public async getVersions(id: number): Promise<any[]> {
    try {
      console.log("[API Client] Fetching versions:", id);
      const response = await this.client.get(`/api/strategy/${id}/versions`);
      console.log("[API Client] Versions retrieved:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] getVersions failed:", error);
      throw error;
    }
  }

  /**
   * Toggles favorite status
   * @param id - Strategy ID
   * @returns New favorite status
   */
  public async toggleFavorite(id: number): Promise<{ isFavorite: boolean }> {
    try {
      console.log("[API Client] Toggling favorite:", id);
      const response = await this.client.post(`/api/strategy/${id}/favorite`);
      console.log("[API Client] Favorite toggled:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] toggleFavorite failed:", error);
      throw error;
    }
  }

  /**
   * Archives or unarchives a strategy
   * @param id - Strategy ID
   * @param archive - True to archive, false to unarchive
   */
  public async archiveStrategy(id: number, archive: boolean): Promise<void> {
    try {
      console.log("[API Client] Archiving strategy:", id, archive);
      await this.client.post(`/api/strategy/${id}/archive`, { archive });
      console.log("[API Client] Strategy archived:", id);
    } catch (error) {
      console.error("[API Client] archiveStrategy failed:", error);
      throw error;
    }
  }

  /**
   * Exports a strategy to JSON
   * @param id - Strategy ID
   * @returns Export data
   */
  public async exportStrategy(id: number): Promise<any> {
    try {
      console.log("[API Client] Exporting strategy:", id);
      const response = await this.client.post(`/api/strategy/${id}/export`);
      console.log("[API Client] Strategy exported:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] exportStrategy failed:", error);
      throw error;
    }
  }

  /**
   * Imports a strategy from JSON
   * @param data - Import data
   * @returns Imported strategy
   */
  public async importStrategy(data: any): Promise<any> {
    try {
      console.log("[API Client] Importing strategy");
      const response = await this.client.post("/api/strategy/import", data);
      console.log("[API Client] Strategy imported:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] importStrategy failed:", error);
      throw error;
    }
  }

  /**
   * Searches strategies with filters
   * @param params - Search parameters
   * @returns Search results
   */
  public async searchStrategies(params: any): Promise<any> {
    try {
      console.log("[API Client] Searching strategies:", params);
      const response = await this.client.post("/api/strategy/search", params);
      console.log("[API Client] Search results:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] searchStrategies failed:", error);
      throw error;
    }
  }

  /**
   * Compares multiple strategies
   * @param strategyIds - Array of strategy IDs to compare
   * @returns Comparison data
   */
  public async compareStrategies(strategyIds: number[]): Promise<any> {
    try {
      console.log("[API Client] Comparing strategies:", strategyIds);
      const response = await this.client.post("/api/strategy/compare", { strategyIds });
      console.log("[API Client] Comparison data:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] compareStrategies failed:", error);
      throw error;
    }
  }

  // ==================== INDICATOR MANAGEMENT ====================

  /**
   * Gets all built-in indicator definitions
   * @returns List of built-in indicators
   */
  public async getBuiltInIndicators(): Promise<any[]> {
    try {
      console.log("[API Client] Fetching built-in indicators");
      const response = await this.client.get("/api/indicator/built-in");
      console.log("[API Client] Built-in indicators retrieved:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] getBuiltInIndicators failed:", error);
      throw error;
    }
  }

  /**
   * Gets user's custom indicators
   * @returns List of user's indicators
   */
  public async getMyIndicators(): Promise<any[]> {
    try {
      console.log("[API Client] Fetching my indicators");
      const response = await this.client.get("/api/indicator/my");
      console.log("[API Client] My indicators retrieved:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] getMyIndicators failed:", error);
      throw error;
    }
  }

  /**
   * Gets public indicators
   * @returns List of public indicators
   */
  public async getPublicIndicators(): Promise<any[]> {
    try {
      console.log("[API Client] Fetching public indicators");
      const response = await this.client.get("/api/indicator/public");
      console.log("[API Client] Public indicators retrieved:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] getPublicIndicators failed:", error);
      throw error;
    }
  }

  /**
   * Creates a new custom indicator
   * @param data - Indicator creation data
   * @returns Created indicator
   */
  public async createIndicator(data: any): Promise<any> {
    try {
      console.log("[API Client] Creating indicator:", data);
      const response = await this.client.post("/api/indicator", data);
      console.log("[API Client] Indicator created:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] createIndicator failed:", error);
      throw error;
    }
  }

  /**
   * Updates an indicator
   * @param id - Indicator ID
   * @param data - Update data
   * @returns Updated indicator
   */
  public async updateIndicator(id: number, data: any): Promise<any> {
    try {
      console.log("[API Client] Updating indicator:", id, data);
      const response = await this.client.put(`/api/indicator/${id}`, data);
      console.log("[API Client] Indicator updated:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] updateIndicator failed:", error);
      throw error;
    }
  }

  /**
   * Deletes an indicator
   * @param id - Indicator ID
   */
  public async deleteIndicator(id: number): Promise<void> {
    try {
      console.log("[API Client] Deleting indicator:", id);
      await this.client.delete(`/api/indicator/${id}`);
      console.log("[API Client] Indicator deleted:", id);
    } catch (error) {
      console.error("[API Client] deleteIndicator failed:", error);
      throw error;
    }
  }

  /**
   * Calculates indicator values for a date range
   * @param id - Indicator ID
   * @param symbol - Trading symbol
   * @param startDate - Start date
   * @param endDate - End date
   * @returns Calculated values
   */
  public async calculateIndicator(
    id: number,
    symbol: string,
    startDate: string,
    endDate: string
  ): Promise<any> {
    try {
      console.log("[API Client] Calculating indicator:", id, symbol);
      const response = await this.client.get(`/api/indicator/${id}/calculate`, {
        params: { symbol, startDate, endDate },
      });
      console.log("[API Client] Indicator calculated:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] calculateIndicator failed:", error);
      throw error;
    }
  }

  // ==================== TAG MANAGEMENT ====================

  /**
   * Gets all tags
   * @returns List of tags
   */
  public async getTags(): Promise<any[]> {
    try {
      console.log("[API Client] Fetching tags");
      const response = await this.client.get("/api/tag");
      console.log("[API Client] Tags retrieved:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] getTags failed:", error);
      throw error;
    }
  }

  /**
   * Creates a new tag
   * @param name - Tag name
   * @param color - Tag color (hex)
   * @returns Created tag
   */
  public async createTag(name: string, color: string): Promise<any> {
    try {
      console.log("[API Client] Creating tag:", name, color);
      const response = await this.client.post("/api/tag", { name, color });
      console.log("[API Client] Tag created:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] createTag failed:", error);
      throw error;
    }
  }

  /**
   * Updates a tag
   * @param id - Tag ID
   * @param data - Update data
   * @returns Updated tag
   */
  public async updateTag(id: number, data: { name?: string; color?: string }): Promise<any> {
    try {
      console.log("[API Client] Updating tag:", id, data);
      const response = await this.client.put(`/api/tag/${id}`, data);
      console.log("[API Client] Tag updated:", response.data);
      return response.data;
    } catch (error) {
      console.error("[API Client] updateTag failed:", error);
      throw error;
    }
  }

  /**
   * Deletes a tag
   * @param id - Tag ID
   */
  public async deleteTag(id: number): Promise<void> {
    try {
      console.log("[API Client] Deleting tag:", id);
      await this.client.delete(`/api/tag/${id}`);
      console.log("[API Client] Tag deleted:", id);
    } catch (error) {
      console.error("[API Client] deleteTag failed:", error);
      throw error;
    }
  }

  /**
   * Gets a paginated list of trades for a strategy result
   * @param strategyId - Strategy ID
   * @param resultId - Strategy result ID
   * @param filters - Optional filters for result, pagination, and sorting
   * @returns Paginated trade list with summary statistics
   */
  public async getTrades(
    strategyId: number,
    resultId: number,
    filters?: TradeFilters
  ): Promise<TradeListResponse> {
    try {
      console.log("[API Client] Fetching trades:", {
        strategyId,
        resultId,
        filters,
      });

      const params: Record<string, string | number> = {
        page: filters?.page || 1,
        pageSize: filters?.pageSize || 20,
        sortBy: filters?.sortBy || "entryTime",
      };

      if (filters?.result) {
        params.result = filters.result;
      }

      const response = await this.client.get<TradeListResponse>(
        `/api/strategy/${strategyId}/results/${resultId}/trades`,
        { params }
      );

      console.log("[API Client] Trades retrieved:", {
        count: response.data.trades.length,
        totalCount: response.data.totalCount,
        page: response.data.page,
        totalPages: response.data.totalPages,
      });

      return response.data;
    } catch (error) {
      console.error("[API Client] getTrades failed:", error);
      throw error;
    }
  }

  /**
   * Gets detailed information about a specific trade
   * @param strategyId - Strategy ID
   * @param resultId - Strategy result ID
   * @param tradeId - Trade ID
   * @returns Trade detail with analysis, chart data, and indicator series
   */
  public async getTradeDetail(
    strategyId: number,
    resultId: number,
    tradeId: number
  ): Promise<TradeDetailResponse> {
    try {
      console.log("[API Client] Fetching trade detail:", {
        strategyId,
        resultId,
        tradeId,
      });

      const response = await this.client.get<TradeDetailResponse>(
        `/api/strategy/${strategyId}/results/${resultId}/trades/${tradeId}`
      );

      console.log("[API Client] Trade detail retrieved:", {
        tradeId,
        entryTime: response.data.trade.entryTime,
        pnl: response.data.trade.pnl,
        hasAnalysis: !!response.data.analysis,
        chartDataPoints: response.data.chartData?.length || 0,
      });

      return response.data;
    } catch (error) {
      console.error("[API Client] getTradeDetail failed:", error);
      throw error;
    }
  }

  /**
   * Gets identified patterns across all trades in a result
   * @param strategyId - Strategy ID
   * @param resultId - Strategy result ID
   * @returns List of trade patterns with frequency and impact
   */
  public async getTradePatterns(
    strategyId: number,
    resultId: number
  ): Promise<TradePattern[]> {
    try {
      console.log("[API Client] Fetching trade patterns:", {
        strategyId,
        resultId,
      });

      const response = await this.client.get<TradePattern[]>(
        `/api/strategy/${strategyId}/results/${resultId}/patterns`
      );

      console.log("[API Client] Trade patterns retrieved:", {
        count: response.data.length,
        patterns: response.data.map((p) => p.name).join(", "),
      });

      return response.data;
    } catch (error) {
      console.error("[API Client] getTradePatterns failed:", error);
      throw error;
    }
  }

  /**
   * Gets heatmap data for trade performance by dimension
   * @param strategyId - Strategy ID
   * @param resultId - Strategy result ID
   * @param dimension - Dimension to analyze (hour, dayOfWeek, duration, marketCondition)
   * @returns Heatmap data with cells colored by performance
   */
  public async getHeatmap(
    strategyId: number,
    resultId: number,
    dimension: string = "hour"
  ): Promise<HeatmapData> {
    try {
      console.log("[API Client] Fetching heatmap:", {
        strategyId,
        resultId,
        dimension,
      });

      const response = await this.client.get<HeatmapData>(
        `/api/strategy/${strategyId}/results/${resultId}/heatmap`,
        { params: { dimension } }
      );

      console.log("[API Client] Heatmap retrieved:", {
        dimension: response.data.dimension,
        label: response.data.label,
        cellCount: response.data.cells.length,
      });

      return response.data;
    } catch (error) {
      console.error("[API Client] getHeatmap failed:", error);
      throw error;
    }
  }
}

// Export a singleton instance
const apiClient = new TradingStrategyApiClient();

export default apiClient;
export { apiClient };

// Also export the class for testing purposes
export { TradingStrategyApiClient };
