import axios, { AxiosError, AxiosInstance, AxiosResponse } from "axios";
import type {
  AnalyzeStrategyRequest,
  AnalyzeStrategyResponse,
  RefineStrategyRequest,
  Strategy,
  SymbolInfo,
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
}

// Export a singleton instance
const apiClient = new TradingStrategyApiClient();

export default apiClient;

// Also export the class for testing purposes
export { TradingStrategyApiClient };
