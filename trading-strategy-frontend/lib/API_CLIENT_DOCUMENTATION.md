# Trading Strategy API Client Documentation

Complete type-safe API client for the Trading Strategy backend.

## Overview

The API client provides a fully typed interface to interact with the Trading Strategy API, including:

- ✅ Type-safe methods for all endpoints
- ✅ Automatic error handling with custom error class
- ✅ Request/response interceptors
- ✅ Authentication support (Bearer token)
- ✅ Comprehensive logging
- ✅ 2-minute timeout for long-running operations
- ✅ Singleton pattern for easy imports

## Installation

The API client is already configured. Simply import it:

```typescript
import apiClient from "@/lib/api-client";
```

## Configuration

Set the API base URL in `.env.local`:

```bash
NEXT_PUBLIC_API_URL=http://localhost:5000
```

Default: `http://localhost:5000`

## API Methods

### 1. analyzeStrategy

Analyzes a trading strategy from natural language description.

```typescript
const response = await apiClient.analyzeStrategy({
  description: "Buy when price crosses above VWAP with stop at 10 points and target at 20 points",
  symbol: "ES",
  startDate: "2024-01-01T00:00:00Z",
  endDate: "2024-03-01T00:00:00Z",
});

// Returns: AnalyzeStrategyResponse
// - strategy: Strategy (parsed with conditions, stops, targets)
// - result: StrategyResult (metrics, trades, insights)
// - elapsedMilliseconds: number
// - aiProvider: string ("Claude" or "Gemini")
```

**Endpoint:** `POST /api/strategy/analyze`

**Timeout:** 120 seconds (AI parsing + backtesting)

### 2. getStrategy

Retrieves a strategy by ID with all related data.

```typescript
const strategy = await apiClient.getStrategy(123);

// Returns: Strategy
// Includes: entryConditions[], stopLoss, takeProfit, results[]
```

**Endpoint:** `GET /api/strategy/{id}`

**Errors:** 404 if strategy not found

### 3. saveStrategy

Saves a strategy to the database.

```typescript
const savedStrategy = await apiClient.saveStrategy({
  name: "VWAP Crossover",
  description: "Buy when price crosses above VWAP",
  direction: "long",
  symbol: "ES",
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
});

// Returns: Strategy with generated ID
```

**Endpoint:** `POST /api/strategy/save`

**Status:** 201 Created

### 4. listStrategies

Lists all strategies, optionally filtered by symbol.

```typescript
// Get all strategies
const allStrategies = await apiClient.listStrategies();

// Filter by symbol
const esStrategies = await apiClient.listStrategies("ES");

// Returns: Strategy[]
```

**Endpoint:** `GET /api/strategy/list?symbol={symbol}`

**Auth:** Required (Bearer token)

**Note:** Currently returns all strategies (user filter disabled in dev)

### 5. refineStrategy

Refines an existing strategy by adding conditions.

```typescript
const response = await apiClient.refineStrategy({
  strategyId: 123,
  additionalCondition: "and volume > 1.5x_average",
  symbol: "ES",
  startDate: "2024-01-01T00:00:00Z",
  endDate: "2024-03-01T00:00:00Z",
});

// Returns: AnalyzeStrategyResponse
// Strategy name will be "{OriginalName} (Refined)"
// Version will be incremented
```

**Endpoint:** `POST /api/strategy/refine`

**Errors:** 404 if original strategy not found

### 6. getSymbols

Gets all supported futures symbols with specifications.

```typescript
const symbols = await apiClient.getSymbols();

// Returns: SymbolInfo[]
// Each symbol includes:
// - symbol: "ES" | "NQ" | "YM" | "BTC" | "CL"
// - name: "E-mini S&P 500"
// - pointValue: 50
// - tickSize: 0.25
// - tickValue: 12.50
// - minDate: earliest data timestamp (ISO 8601)
// - maxDate: latest data timestamp (ISO 8601)
// - barCount: total bars available
```

**Endpoint:** `GET /api/strategy/symbols`

### 7. deleteStrategy

Deletes a strategy by ID.

```typescript
await apiClient.deleteStrategy(123);

// Returns: void
```

**Endpoint:** `DELETE /api/strategy/{id}`

**Errors:** 404 if strategy not found

## Error Handling

All errors are thrown as `ApiClientError`:

```typescript
import { ApiClientError } from "@/lib/api-client";

try {
  const response = await apiClient.analyzeStrategy(request);
} catch (error) {
  if (error instanceof ApiClientError) {
    console.error("Status:", error.status);      // HTTP status code
    console.error("Message:", error.message);    // Error title
    console.error("Detail:", error.detail);      // Detailed explanation
  } else {
    console.error("Unexpected error:", error);
  }
}
```

### Error Types

| Status | Type | Description |
|--------|------|-------------|
| 0 | Network Error | No response from server / connection failed |
| 400 | Bad Request | Invalid parameters or validation failed |
| 401 | Unauthorized | Missing or invalid authentication token |
| 404 | Not Found | Strategy or resource not found |
| 500 | Server Error | Internal server error during processing |

## Authentication

Set authentication token for protected endpoints:

```typescript
// Set token (after login)
apiClient.setAuthToken("your-jwt-token");

// All subsequent requests will include:
// Authorization: Bearer your-jwt-token

// Clear token (on logout)
apiClient.setAuthToken(null);
```

## Supported Symbols

| Symbol | Name | Point Value | Tick Size | Tick Value |
|--------|------|-------------|-----------|------------|
| ES | E-mini S&P 500 | $50 | 0.25 | $12.50 |
| NQ | E-mini Nasdaq 100 | $20 | 0.25 | $5.00 |
| YM | E-mini Dow | $5 | 1.00 | $5.00 |
| BTC | Bitcoin Futures | $5 | 5.00 | $25.00 |
| CL | Crude Oil | $1000 | 0.01 | $10.00 |

## Type Definitions

All types are defined in `@/lib/types.ts`:

- `Strategy` - Complete strategy with conditions, stops, targets
- `StrategyResult` - Backtest results with metrics and insights
- `TradeResult` - Individual trade with entry/exit, P&L, MAE, MFE
- `Condition` - Entry/exit condition (indicator, operator, value)
- `StopLoss` - Stop loss configuration
- `TakeProfit` - Take profit configuration
- `AnalyzeStrategyRequest` - Natural language analysis request
- `AnalyzeStrategyResponse` - Analysis results
- `RefineStrategyRequest` - Strategy refinement request
- `SymbolInfo` - Symbol metadata and specifications
- `Bar` - OHLCV bar with indicators

## Logging

All API calls are logged to the console with the prefix `[API Client]`:

```
[API Client] Analyzing strategy: { symbol: "ES", startDate: "...", ... }
[API Client] Strategy analysis completed: { totalTrades: 45, winRate: 0.65, ... }
```

Errors are also logged:

```
[API Client] HTTP Error: { status: 400, message: "Invalid symbol", ... }
```

## Example: React Component

```typescript
"use client";

import { useState } from "react";
import apiClient, { ApiClientError } from "@/lib/api-client";
import type { AnalyzeStrategyRequest, AnalyzeStrategyResponse } from "@/lib/types";

export function StrategyAnalyzer() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<AnalyzeStrategyResponse | null>(null);

  const analyzeStrategy = async (request: AnalyzeStrategyRequest) => {
    try {
      setLoading(true);
      setError(null);

      const response = await apiClient.analyzeStrategy(request);
      setResult(response);
    } catch (err) {
      if (err instanceof ApiClientError) {
        setError(err.detail);
      } else {
        setError("An unexpected error occurred");
      }
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div>Analyzing strategy...</div>;
  if (error) return <div>Error: {error}</div>;
  if (result) {
    return (
      <div>
        <h2>{result.strategy.name}</h2>
        <p>Total Trades: {result.result.totalTrades}</p>
        <p>Win Rate: {(result.result.winRate * 100).toFixed(2)}%</p>
        <p>Total P&L: ${result.result.totalPnl.toFixed(2)}</p>
      </div>
    );
  }

  return <div>Ready to analyze</div>;
}
```

## Best Practices

1. **Always handle errors** - Use try/catch with `ApiClientError`
2. **Check error status** - Different statuses require different handling
3. **Use TypeScript types** - Import types from `@/lib/types`
4. **Log important operations** - Helps with debugging
5. **Set auth token once** - On login/logout, not per request
6. **Use loading states** - API calls can take 1-2 minutes
7. **Validate inputs** - Before calling API methods
8. **Cache results** - Backend caches, but client-side helps UX

## Testing

The API client class is exported for testing:

```typescript
import { TradingStrategyApiClient } from "@/lib/api-client";

// Create a test instance
const testClient = new TradingStrategyApiClient();
testClient.setAuthToken("test-token");
```

## Additional Resources

- **Backend API Docs:** `http://localhost:5000/swagger` (when running)
- **Type Definitions:** `/lib/types.ts`
- **Usage Examples:** `/lib/api-client.example.ts`
