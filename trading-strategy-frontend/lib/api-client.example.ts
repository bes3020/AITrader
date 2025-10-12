/**
 * Example usage of the Trading Strategy API Client
 *
 * This file demonstrates how to use the API client in your components.
 * DO NOT import this file - it's for reference only.
 */

import apiClient, { ApiClientError } from "./api-client";
import type { AnalyzeStrategyRequest } from "./types";

// ============================================================================
// Example 1: Analyze a Strategy
// ============================================================================
async function exampleAnalyzeStrategy() {
  try {
    const request: AnalyzeStrategyRequest = {
      description:
        "Buy when price crosses above VWAP with stop at 10 points and target at 20 points",
      symbol: "ES",
      startDate: "2024-01-01T00:00:00Z",
      endDate: "2024-03-01T00:00:00Z",
    };

    const response = await apiClient.analyzeStrategy(request);

    console.log("Strategy Name:", response.strategy.name);
    console.log("Total Trades:", response.result.totalTrades);
    console.log("Win Rate:", (response.result.winRate * 100).toFixed(2) + "%");
    console.log("Total P&L:", response.result.totalPnl);
    console.log("AI Provider:", response.aiProvider);
    console.log("AI Insights:", response.result.insights);
  } catch (error) {
    if (error instanceof ApiClientError) {
      console.error("API Error:", error.message);
      console.error("Status:", error.status);
      console.error("Detail:", error.detail);
    } else {
      console.error("Unexpected error:", error);
    }
  }
}

// ============================================================================
// Example 2: Get Strategy by ID
// ============================================================================
async function exampleGetStrategy(strategyId: number) {
  try {
    const strategy = await apiClient.getStrategy(strategyId);

    console.log("Strategy:", strategy.name);
    console.log("Direction:", strategy.direction);
    console.log("Entry Conditions:", strategy.entryConditions.length);
    console.log("Stop Loss:", strategy.stopLoss?.type, strategy.stopLoss?.value);
    console.log(
      "Take Profit:",
      strategy.takeProfit?.type,
      strategy.takeProfit?.value
    );
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 404) {
        console.error("Strategy not found");
      } else {
        console.error("Error fetching strategy:", error.message);
      }
    }
  }
}

// ============================================================================
// Example 3: Save a Strategy
// ============================================================================
async function exampleSaveStrategy() {
  try {
    const strategy = {
      name: "VWAP Crossover",
      description: "Buy when price crosses above VWAP",
      direction: "long",
      symbol: "ES",
      timeframe: "1m",
      entryConditions: [
        {
          indicator: "price",
          operator: "crosses_above",
          value: "vwap",
        },
      ],
      stopLoss: {
        type: "points",
        value: 10,
      },
      takeProfit: {
        type: "points",
        value: 20,
      },
    };

    const savedStrategy = await apiClient.saveStrategy(strategy);

    console.log("Strategy saved with ID:", savedStrategy.id);
  } catch (error) {
    if (error instanceof ApiClientError) {
      console.error("Failed to save strategy:", error.detail);
    }
  }
}

// ============================================================================
// Example 4: List Strategies
// ============================================================================
async function exampleListStrategies() {
  try {
    // List all strategies
    const allStrategies = await apiClient.listStrategies();
    console.log("Total strategies:", allStrategies.length);

    // List strategies for ES only
    const esStrategies = await apiClient.listStrategies("ES");
    console.log("ES strategies:", esStrategies.length);

    esStrategies.forEach((strategy) => {
      console.log(`- ${strategy.name} (${strategy.symbol})`);
    });
  } catch (error) {
    if (error instanceof ApiClientError) {
      console.error("Failed to list strategies:", error.message);
    }
  }
}

// ============================================================================
// Example 5: Refine a Strategy
// ============================================================================
async function exampleRefineStrategy() {
  try {
    const request = {
      strategyId: 123,
      additionalCondition: "and volume > 1.5x_average",
      symbol: "ES",
      startDate: "2024-01-01T00:00:00Z",
      endDate: "2024-03-01T00:00:00Z",
    };

    const response = await apiClient.refineStrategy(request);

    console.log("Refined Strategy:", response.strategy.name);
    console.log("New Win Rate:", (response.result.winRate * 100).toFixed(2) + "%");
    console.log("Total Trades:", response.result.totalTrades);
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 404) {
        console.error("Original strategy not found");
      } else {
        console.error("Failed to refine strategy:", error.detail);
      }
    }
  }
}

// ============================================================================
// Example 6: Get Supported Symbols
// ============================================================================
async function exampleGetSymbols() {
  try {
    const symbols = await apiClient.getSymbols();

    symbols.forEach((symbol) => {
      console.log(`${symbol.symbol} - ${symbol.name}`);
      console.log(`  Point Value: $${symbol.pointValue}`);
      console.log(`  Tick Size: ${symbol.tickSize}`);
      console.log(`  Bars Available: ${symbol.barCount.toLocaleString()}`);
      if (symbol.minDate && symbol.maxDate) {
        console.log(`  Date Range: ${symbol.minDate} to ${symbol.maxDate}`);
      }
    });
  } catch (error) {
    if (error instanceof ApiClientError) {
      console.error("Failed to get symbols:", error.message);
    }
  }
}

// ============================================================================
// Example 7: Delete a Strategy
// ============================================================================
async function exampleDeleteStrategy(strategyId: number) {
  try {
    await apiClient.deleteStrategy(strategyId);
    console.log("Strategy deleted successfully");
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 404) {
        console.error("Strategy not found");
      } else {
        console.error("Failed to delete strategy:", error.message);
      }
    }
  }
}

// ============================================================================
// Example 8: Using with Authentication
// ============================================================================
async function exampleWithAuth() {
  // Set auth token (e.g., after user login)
  const token = "your-jwt-token-here";
  apiClient.setAuthToken(token);

  // Now all requests will include the Authorization header
  const strategies = await apiClient.listStrategies();

  // Clear auth token (e.g., on logout)
  apiClient.setAuthToken(null);
}

// ============================================================================
// Example 9: React Component Usage
// ============================================================================
/*
import { useState, useEffect } from "react";
import apiClient, { ApiClientError } from "@/lib/api-client";
import type { Strategy } from "@/lib/types";

export function StrategyList() {
  const [strategies, setStrategies] = useState<Strategy[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadStrategies() {
      try {
        setLoading(true);
        setError(null);
        const data = await apiClient.listStrategies();
        setStrategies(data);
      } catch (err) {
        if (err instanceof ApiClientError) {
          setError(err.detail);
        } else {
          setError("An unexpected error occurred");
        }
      } finally {
        setLoading(false);
      }
    }

    loadStrategies();
  }, []);

  if (loading) return <div>Loading strategies...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      {strategies.map((strategy) => (
        <div key={strategy.id}>
          <h3>{strategy.name}</h3>
          <p>{strategy.description}</p>
        </div>
      ))}
    </div>
  );
}
*/
