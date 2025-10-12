# Trading Strategy API - Complete Documentation

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Technology Stack](#technology-stack)
4. [Database Schema](#database-schema)
5. [API Endpoints](#api-endpoints)
6. [Core Components](#core-components)
7. [Data Flow](#data-flow)
8. [Setup & Installation](#setup--installation)
9. [Configuration](#configuration)
10. [Design Decisions](#design-decisions)
11. [Error Handling](#error-handling)
12. [Future Enhancements](#future-enhancements)

---

## Overview

The Trading Strategy API is a comprehensive backtesting system that allows users to describe trading strategies in natural language, which are then automatically parsed by AI, backtested against historical futures market data, and analyzed for performance.

### Key Features
- **AI-Powered Strategy Parsing**: Convert natural language descriptions into executable strategies using Google Gemini or Anthropic Claude
- **Multi-Symbol Support**: Backtest on ES, NQ, YM, BTC, and CL futures contracts
- **Historical Data Analysis**: 1-minute bar data with pre-calculated technical indicators
- **Comprehensive Results**: Detailed performance metrics, trade-by-trade analysis, and AI-generated insights
- **Error Tracking**: Advanced error logging with automatic fix suggestions
- **Caching**: Redis-based caching for improved performance

---

## Architecture

### High-Level Architecture

```
┌─────────────────┐
│   Frontend      │ (Next.js 15 + React 19)
│   Port 3000     │
└────────┬────────┘
         │ HTTP/REST
         ▼
┌─────────────────┐
│   API Layer     │ (ASP.NET Core 8.0)
│   Port 5000     │
└────────┬────────┘
         │
    ┌────┴────┬──────────┬──────────┐
    ▼         ▼          ▼          ▼
┌────────┐ ┌─────┐  ┌────────┐  ┌─────────┐
│ Postgre│ │Redis│  │ Gemini │  │ Claude  │
│  SQL   │ │Cache│  │   AI   │  │   AI    │
└────────┘ └─────┘  └────────┘  └─────────┘
```

### Layer Structure

1. **Controllers** - HTTP request handling and routing
2. **Services** - Business logic and orchestration
3. **Models** - Domain entities and data structures
4. **Database** - Entity Framework Core + PostgreSQL
5. **DTOs** - Data transfer objects for API contracts

---

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8.0 (C#)
- **Database**: PostgreSQL 16
- **ORM**: Entity Framework Core 8.0
- **Cache**: Redis 7.x
- **AI Services**:
  - Google Gemini 1.5 Flash (default)
  - Anthropic Claude 3.5 Sonnet
- **Logging**: ILogger (ASP.NET Core)

### Frontend
- **Framework**: Next.js 15 with App Router
- **UI Library**: React 19
- **Language**: TypeScript
- **Styling**: Tailwind CSS
- **UI Components**: shadcn/ui
- **Charts**: Recharts
- **HTTP Client**: Axios

### DevOps
- **Version Control**: Git
- **Package Manager**: NuGet (.NET), npm (Frontend)
- **Migration Tool**: EF Core Migrations

---

## Database Schema

### Entity Relationship Diagram

```
users (1) ────────┐
                  │
                  ▼ (many)
              strategies (1)
                  │
    ┌─────────────┼─────────────────┬──────────────┐
    │             │                 │              │
    ▼ (many)      ▼ (1)             ▼ (1)          ▼ (many)
conditions   stop_losses      take_profits    strategy_results (1)
                                                     │
                                                     ▼ (many)
                                                trade_results

futures_bars (multi-symbol market data)
strategy_errors (error tracking)
```

### Core Tables

#### **users**
Stores user accounts for authentication and strategy ownership.

| Column | Type | Description |
|--------|------|-------------|
| id | serial | Primary key |
| email | varchar(255) | Unique email address |
| password_hash | text | Hashed password |
| created_at | timestamp | Account creation time |

**Indexes**:
- `ix_users_email` (unique)

**Special Users**:
- User ID 1: Anonymous user for unauthenticated strategy creation

---

#### **strategies**
Core table storing trading strategy definitions.

| Column | Type | Description |
|--------|------|-------------|
| id | serial | Primary key |
| user_id | integer | Foreign key to users |
| name | varchar(200) | Strategy name |
| description | text | Natural language description |
| direction | varchar(10) | "long", "short", or "both" |
| symbol | varchar(20) | Trading symbol (ES, NQ, etc.) |
| timeframe | varchar(10) | Bar timeframe (1m, 5m, etc.) |
| created_at | timestamp | Creation timestamp |
| updated_at | timestamp | Last update timestamp |
| is_active | boolean | Active status flag |
| version | integer | Strategy version number |
| max_positions | integer | Max concurrent positions |
| position_size | integer | Position size (contracts) |

**Indexes**:
- `ix_strategies_user_id`
- `ix_strategies_created_at`
- `ix_strategies_is_active`

**Relationships**:
- Many-to-One with `users`
- One-to-Many with `conditions`, `strategy_results`
- One-to-One with `stop_losses`, `take_profits`

---

#### **conditions**
Entry conditions for strategies (AND logic between multiple conditions).

| Column | Type | Description |
|--------|------|-------------|
| id | serial | Primary key |
| strategy_id | integer | Foreign key to strategies |
| indicator | varchar(50) | Indicator name (price, volume, ema9, etc.) |
| operator | varchar(20) | Comparison operator (>, <, =, crosses_above, etc.) |
| value | varchar(100) | Compare value (number, indicator, or expression) |
| description | text | Optional description |

**Valid Indicators**:
- `price`, `volume`, `vwap`
- `ema9`, `ema20`, `ema50`
- `rsi`, `atr`, `macd`
- `prev_day_high`, `prev_day_low`
- `time` (minutes since midnight)

**Valid Operators**:
- Comparison: `>`, `<`, `>=`, `<=`, `=`
- Crossover: `crosses_above`, `crosses_below`

**Value Formats**:
- Number: `"100.50"`
- Indicator: `"vwap"`
- Multiplier: `"1.5x_average"` (1.5 times avgVolume20)
- Time: `"10:00"` (10:00 AM)

**Indexes**:
- `ix_conditions_strategy_id`
- `ix_conditions_indicator`

---

#### **stop_losses**
Stop loss configuration for risk management.

| Column | Type | Description |
|--------|------|-------------|
| id | serial | Primary key |
| strategy_id | integer | Foreign key to strategies (unique) |
| type | varchar(50) | "points", "percentage", or "atr" |
| value | decimal(18,4) | Stop loss value |
| description | text | Optional description |

**Types**:
- `points`: Fixed points/ticks from entry
- `percentage`: Percentage of entry price
- `atr`: Multiplier of Average True Range

**Indexes**:
- `ix_stop_losses_strategy_id` (unique)

---

#### **take_profits**
Take profit configuration for profit-taking.

| Column | Type | Description |
|--------|------|-------------|
| id | serial | Primary key |
| strategy_id | integer | Foreign key to strategies (unique) |
| type | varchar(50) | "points", "percentage", or "atr" |
| value | decimal(18,4) | Take profit value |
| description | text | Optional description |

**Types**: Same as stop_losses

**Indexes**:
- `ix_take_profits_strategy_id` (unique)

---

#### **strategy_results**
Aggregate results from strategy backtests.

| Column | Type | Description |
|--------|------|-------------|
| id | serial | Primary key |
| strategy_id | integer | Foreign key to strategies |
| total_trades | integer | Number of trades executed |
| win_rate | decimal(5,4) | Win rate (0.0 - 1.0) |
| total_pnl | decimal(18,2) | Total profit/loss |
| avg_win | decimal(18,2) | Average winning trade |
| avg_loss | decimal(18,2) | Average losing trade |
| max_drawdown | decimal(18,2) | Maximum drawdown |
| profit_factor | decimal(10,4) | Gross profit / gross loss |
| sharpe_ratio | decimal(10,4) | Risk-adjusted return |
| insights | text | AI-generated analysis |
| created_at | timestamp | Result creation time |
| backtest_start | timestamp | Backtest period start |
| backtest_end | timestamp | Backtest period end |

**Computed Properties** (not stored):
- `worstTrades`: Top 10 worst trades
- `bestTrades`: Top 10 best trades

**Indexes**:
- `ix_strategy_results_strategy_id`
- `ix_strategy_results_created_at`
- `ix_strategy_results_strategy_id_created_at`

---

#### **trade_results**
Individual trade execution details.

| Column | Type | Description |
|--------|------|-------------|
| id | serial | Primary key |
| strategy_result_id | integer | Foreign key to strategy_results |
| entry_time | timestamp | Trade entry timestamp |
| exit_time | timestamp | Trade exit timestamp |
| entry_price | decimal(18,2) | Entry price |
| exit_price | decimal(18,2) | Exit price |
| pnl | decimal(18,2) | Profit/loss |
| result | varchar(20) | "win", "loss", or "timeout" |
| bars_held | integer | Number of bars held |
| max_adverse_excursion | decimal(18,2) | Worst unrealized loss (MAE) |
| max_favorable_excursion | decimal(18,2) | Best unrealized profit (MFE) |

**Indexes**:
- `ix_trade_results_strategy_result_id`
- `ix_trade_results_entry_time`
- `ix_trade_results_result`
- `ix_trade_results_strategy_result_id_result_pnl`

---

#### **futures_bars**
1-minute OHLCV bars for futures contracts with pre-calculated indicators.

| Column | Type | Description |
|--------|------|-------------|
| symbol | varchar(10) | Futures symbol (ES, NQ, YM, BTC, CL) |
| timestamp | timestamp | Bar timestamp (UTC) |
| open | decimal(18,2) | Opening price |
| high | decimal(18,2) | High price |
| low | decimal(18,2) | Low price |
| close | decimal(18,2) | Closing price |
| volume | decimal(18,2) | Volume |
| vwap | decimal(18,4) | Volume-weighted average price |
| ema_9 | decimal(18,4) | 9-period EMA |
| ema_20 | decimal(18,4) | 20-period EMA |
| ema_50 | decimal(18,4) | 50-period EMA |
| avg_volume_20 | decimal(18,2) | 20-period average volume |

**Primary Key**: Composite `(symbol, timestamp)`

**Indexes**:
- `ix_bars_symbol`
- `ix_bars_timestamp`
- `ix_bars_symbol_timestamp`
- `ix_bars_symbol_timestamp_volume`

**Supported Symbols**:
- **ES**: E-mini S&P 500 (50 points per contract, $12.50 per tick)
- **NQ**: E-mini NASDAQ-100 (20 points per contract, $5.00 per tick)
- **YM**: E-mini Dow Jones (5 points per contract, $5.00 per tick)
- **BTC**: Bitcoin Futures (5 points per contract, $5.00 per tick)
- **CL**: Crude Oil Futures (1000 points per contract, $10.00 per tick)

---

#### **strategy_errors**
Error tracking for debugging and pattern analysis.

| Column | Type | Description |
|--------|------|-------------|
| id | serial | Primary key |
| strategy_id | integer | Foreign key to strategies (nullable) |
| error_type | varchar(100) | Error classification |
| severity | varchar(20) | "Info", "Warning", "Error", "Critical" |
| message | text | Error message |
| details | text | Additional error details |
| stack_trace | text | Exception stack trace |
| failed_expression | text | Expression that failed |
| suggested_fix | text | Auto-generated fix suggestion |
| timestamp | timestamp | Error occurrence time |
| context | text | JSON context data |
| is_resolved | boolean | Resolution status |

**Auto-Generated Fix Patterns**:
- `"X * Y"` → `"Xx_Y"` format
- `"average_volume"` → `"avgVolume20"` or `"1.5x_average"`
- Spaces in expressions → Use underscores
- Invalid indicators → List valid options

**Indexes**:
- `ix_strategy_errors_strategy_id`
- `ix_strategy_errors_timestamp`
- `ix_strategy_errors_error_type`
- `ix_strategy_errors_is_resolved`
- `ix_strategy_errors_error_type_timestamp`

---

## API Endpoints

### Base URL
```
http://localhost:5000/api/strategy
```

### Authentication
Currently, authentication is not enforced. All strategies are created under the anonymous user (ID: 1).

---

### **POST /api/strategy/analyze**
Analyzes a trading strategy from natural language description.

**Request Body**:
```json
{
  "description": "Buy when price crosses above VWAP and volume is greater than 1.5x average, with stop at 10 points and target at 20 points",
  "symbol": "ES",
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-06-30T23:59:59Z"
}
```

**Response** (200 OK):
```json
{
  "strategy": {
    "id": 1,
    "userId": 1,
    "name": "AI Generated Strategy",
    "description": "Buy when price crosses above VWAP...",
    "direction": "long",
    "symbol": "ES",
    "entryConditions": [
      {
        "id": 1,
        "indicator": "price",
        "operator": "crosses_above",
        "value": "vwap",
        "strategyId": 1
      },
      {
        "id": 2,
        "indicator": "volume",
        "operator": ">",
        "value": "1.5x_average",
        "strategyId": 1
      }
    ],
    "stopLoss": {
      "id": 1,
      "type": "points",
      "value": 10.0,
      "strategyId": 1
    },
    "takeProfit": {
      "id": 1,
      "type": "points",
      "value": 20.0,
      "strategyId": 1
    }
  },
  "result": {
    "id": 1,
    "strategyId": 1,
    "totalTrades": 156,
    "winRate": 0.6538,
    "totalPnl": 12450.00,
    "avgWin": 325.50,
    "avgLoss": -187.25,
    "maxDrawdown": -2340.00,
    "profitFactor": 2.34,
    "sharpeRatio": 1.87,
    "insights": "This strategy performs best during high volatility...",
    "backtestStart": "2024-01-01T00:00:00Z",
    "backtestEnd": "2024-06-30T23:59:59Z",
    "worstTrades": [...],
    "allTrades": [...]
  },
  "elapsedMilliseconds": 18543,
  "aiProvider": "Gemini"
}
```

**Error Responses**:
- 400 Bad Request: Invalid parameters
- 500 Internal Server Error: Processing failure

**Caching**: Results are cached in Redis for 30 days based on description + symbol + date range

---

### **GET /api/strategy/{id}**
Retrieves a specific strategy by ID.

**Parameters**:
- `id` (path): Strategy ID

**Response** (200 OK):
```json
{
  "id": 1,
  "userId": 1,
  "name": "AI Generated Strategy",
  "description": "...",
  "direction": "long",
  "symbol": "ES",
  "entryConditions": [...],
  "stopLoss": {...},
  "takeProfit": {...},
  "results": [...]
}
```

**Error Responses**:
- 404 Not Found: Strategy doesn't exist

---

### **POST /api/strategy/save**
Saves a strategy to the database.

**Request Body**:
```json
{
  "name": "My Custom Strategy",
  "description": "...",
  "direction": "long",
  "symbol": "ES",
  "entryConditions": [...],
  "stopLoss": {...},
  "takeProfit": {...}
}
```

**Response** (201 Created):
```json
{
  "id": 2,
  ...
}
```

**Location Header**: `/api/strategy/2`

---

### **GET /api/strategy/list**
Lists all strategies (requires authentication in production).

**Query Parameters**:
- `symbol` (optional): Filter by symbol

**Response** (200 OK):
```json
[
  {
    "id": 1,
    "name": "Strategy 1",
    ...
  },
  {
    "id": 2,
    "name": "Strategy 2",
    ...
  }
]
```

---

### **POST /api/strategy/refine**
Refines an existing strategy by adding conditions.

**Request Body**:
```json
{
  "strategyId": 1,
  "additionalCondition": "and RSI is below 30",
  "symbol": "ES",
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-06-30T23:59:59Z"
}
```

**Response**: Same as `/analyze`

---

### **GET /api/strategy/symbols**
Gets information about supported trading symbols.

**Response** (200 OK):
```json
[
  {
    "symbol": "ES",
    "name": "E-mini S&P 500",
    "pointValue": 50.0,
    "tickSize": 0.25,
    "tickValue": 12.50,
    "minDate": "2023-01-01T00:00:00Z",
    "maxDate": "2024-12-31T23:59:59Z",
    "barCount": 524160
  },
  ...
]
```

---

### **GET /api/strategy/errors**
Retrieves recent strategy evaluation errors.

**Query Parameters**:
- `count` (optional, default: 50, max: 200): Number of errors to retrieve

**Response** (200 OK):
```json
[
  {
    "id": 1,
    "strategyId": 5,
    "errorType": "Evaluation",
    "severity": "Warning",
    "message": "Error evaluating condition: volume > 1.5 * average_volume",
    "failedExpression": "1.5 * average_volume",
    "suggestedFix": "Change '1.5 * average_volume' to '1.5x_average'",
    "timestamp": "2024-10-12T15:30:45Z",
    "isResolved": false
  },
  ...
]
```

---

### **GET /api/strategy/errors/statistics**
Gets error statistics and patterns.

**Response** (200 OK):
```json
{
  "totalErrors": 47,
  "unresolvedErrors": 12,
  "errorsByType": [
    { "type": "Evaluation", "count": 32 },
    { "type": "Parsing", "count": 15 }
  ],
  "errorsBySeverity": [
    { "severity": "Warning", "count": 35 },
    { "severity": "Error", "count": 12 }
  ],
  "topFailedExpressions": [
    {
      "expression": "1.5 * average_volume",
      "count": 8,
      "suggestedFix": "Change '1.5 * average_volume' to '1.5x_average'"
    }
  ]
}
```

---

## Core Components

### Services

#### **IAIService / GeminiService / ClaudeService**
AI service abstraction for parsing natural language into strategies.

**Key Methods**:
- `ParseStrategyAsync(string description)`: Converts text to Strategy object
- `GenerateInsightsAsync(StrategyResult result)`: Creates performance insights

**Features**:
- Automatic retry with exponential backoff
- Rate limit handling
- Response caching (30 days)
- JSON extraction from markdown-wrapped responses

**Configuration**:
```json
{
  "AI": {
    "Provider": "gemini",
    "Gemini": {
      "ApiKey": "your-key-here",
      "Model": "gemini-1.5-flash"
    },
    "Claude": {
      "ApiKey": "your-key-here",
      "Model": "claude-3-5-sonnet-20241022"
    }
  }
}
```

**Updated Prompts** (fixes expression format issues):
- Includes example: `'volume' > '1.5x_average'`
- Explicit rules: Use `Xx_indicator` NOT `X * indicator`
- Valid indicators list
- Valid formats documentation

---

#### **IStrategyEvaluator / StrategyEvaluator**
Evaluates strategy conditions against market data.

**Key Methods**:
- `EvaluateEntry(Strategy, Bar, List<Bar>)`: Checks if entry conditions are met
- `CalculateIndicator(string, List<Bar>, int)`: Calculates technical indicators

**Supported Indicators**:
- Price: close, open, high, low
- Volume-based: volume, vwap, avgVolume20
- Moving averages: ema9, ema20, ema50
- Technical: rsi, atr
- Time-based: time, prev_day_high, prev_day_low

**Value Resolution**:
- Numbers: `"100.5"`
- Indicators: `"vwap"`
- Multipliers: `"1.5x_average"` → 1.5 * avgVolume20
- Time: `"10:30"` → 630 minutes

**Error Handling**:
- Injects `IErrorTracker` for logging
- Captures failed expressions
- Logs context (symbol, timestamp, condition count)

---

#### **IStrategyScanner / StrategyScanner**
Scans historical data to execute strategies.

**Key Methods**:
- `ScanAsync(Strategy, symbol, startDate, endDate)`: Runs backtest

**Process**:
1. Load bars from database (symbol + date range)
2. Build historical window (lookback for indicators)
3. Iterate through each bar
4. Check entry conditions
5. Track open positions
6. Check exit conditions (stop loss, take profit, timeout)
7. Calculate P&L and metrics (MAE, MFE)
8. Record trade results

**Features**:
- Multi-position support (configurable max_positions)
- Position sizing
- Session filtering (market hours only)
- Bar-by-bar execution simulation

---

#### **IResultsAnalyzer / ResultsAnalyzer**
Analyzes trade results and generates performance metrics.

**Key Methods**:
- `AnalyzeAsync(List<TradeResult>, Strategy)`: Generates StrategyResult

**Calculated Metrics**:
- Total trades, win rate
- Total P&L, average win/loss
- Maximum drawdown
- Profit factor (gross profit / gross loss)
- Sharpe ratio (risk-adjusted returns)
- Best/worst trades
- AI-generated insights

---

#### **IErrorTracker / ErrorTracker**
Tracks and analyzes strategy errors.

**Key Methods**:
- `LogErrorAsync(...)`: Logs error with auto-fix suggestions
- `GetStrategyErrorsAsync(strategyId)`: Gets errors for strategy
- `GetErrorStatisticsAsync()`: Aggregated error stats
- `AnalyzeErrorPatternsAsync()`: Detects common patterns

**Pattern Detection**:
- Multiply format: `"X * Y"` → `"Xx_Y"`
- Indicator names: Suggests valid alternatives
- Expression syntax: Spaces, underscores, etc.

---

#### **IDataService / DataService**
Data access layer for market data.

**Key Methods**:
- `GetBarsAsync(symbol, startDate, endDate)`: Fetches bars
- `GetSymbolsAsync()`: Lists available symbols
- `GetDataRangeAsync(symbol)`: Gets available date range

---

### Models

All models use:
- Data annotations for validation
- Entity Framework navigation properties
- `[JsonIgnore]` on back-references to prevent circular serialization
- Computed properties marked with `[NotMapped]`

**Key Design Pattern**:
```csharp
// Parent entity includes children
public ICollection<Child> Children { get; set; }

// Child entity has foreign key but ignores parent in JSON
[JsonIgnore]
public Parent? Parent { get; set; }
```

---

## Data Flow

### Strategy Analysis Flow

```
1. User Input (Frontend)
   ↓
2. POST /api/strategy/analyze
   ↓
3. Check Redis cache
   ├─ Hit → Return cached result
   └─ Miss → Continue
   ↓
4. IAIService.ParseStrategyAsync()
   - Send to Gemini/Claude
   - Extract JSON
   - Create Strategy object
   ↓
5. Save Strategy to database
   - Get auto-generated ID
   ↓
6. IStrategyScanner.ScanAsync()
   - Load historical bars
   - Evaluate conditions
   - Track positions
   - Execute trades
   - Generate TradeResults
   ↓
7. IResultsAnalyzer.AnalyzeAsync()
   - Calculate metrics
   - Generate insights
   - Create StrategyResult
   ↓
8. Save Results to database
   - Link to Strategy
   - Save all trades
   ↓
9. Cache result in Redis
   ↓
10. Return to frontend
   ↓
11. Display results page
```

---

## Setup & Installation

### Prerequisites
- .NET 8.0 SDK
- Node.js 20+
- PostgreSQL 16+
- Redis 7+
- Google Gemini API key (free tier available)

### Backend Setup

1. **Clone repository**:
```bash
cd /mnt/d/repos/AITrader/TradingStrategyAPI
```

2. **Install dependencies**:
```bash
dotnet restore
```

3. **Configure database**:
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=trading_strategy;Username=postgres;Password=your_password",
    "Redis": "localhost:6379"
  },
  "AI": {
    "Provider": "gemini",
    "Gemini": {
      "ApiKey": "your-gemini-api-key-here"
    }
  }
}
```

4. **Run migrations**:
```bash
dotnet ef database update
```

This creates:
- All tables with indexes
- Default anonymous user (ID: 1)

5. **Load market data** (if available):
```bash
# Use TradingStrategyAPI.DataLoader project
cd TradingStrategyAPI.DataLoader
dotnet run
```

6. **Run API**:
```bash
cd TradingStrategyAPI
dotnet run
```

API available at: `http://localhost:5000`
Swagger UI: `http://localhost:5000/swagger`

---

### Frontend Setup

1. **Navigate to frontend**:
```bash
cd /mnt/d/repos/AITrader/trading-strategy-frontend
```

2. **Install dependencies**:
```bash
npm install
```

3. **Configure API endpoint** (if needed):
Edit `lib/api-client.ts`:
```typescript
const API_BASE_URL = "http://localhost:5000/api";
```

4. **Run development server**:
```bash
npm run dev
```

Frontend available at: `http://localhost:3000`

---

### Docker Setup (Optional)

**PostgreSQL**:
```bash
docker run -d \
  --name trading-postgres \
  -e POSTGRES_DB=trading_strategy \
  -e POSTGRES_PASSWORD=your_password \
  -p 5432:5432 \
  postgres:16
```

**Redis**:
```bash
docker run -d \
  --name trading-redis \
  -p 6379:6379 \
  redis:7-alpine
```

---

## Configuration

### appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=trading_strategy;Username=postgres;Password=password",
    "Redis": "localhost:6379"
  },
  "AI": {
    "Provider": "gemini",
    "Gemini": {
      "ApiKey": "your-gemini-api-key",
      "Model": "gemini-1.5-flash"
    },
    "Claude": {
      "ApiKey": "your-claude-api-key",
      "Model": "claude-3-5-sonnet-20241022"
    }
  }
}
```

### Environment Variables (Alternative)

```bash
export ConnectionStrings__PostgreSQL="Host=localhost;..."
export ConnectionStrings__Redis="localhost:6379"
export AI__Provider="gemini"
export AI__Gemini__ApiKey="your-key"
```

---

## Design Decisions

### 1. **AI Provider Abstraction**
**Decision**: Use interface `IAIService` with multiple implementations

**Rationale**:
- Easy to switch between Gemini (free) and Claude (paid)
- Can add more providers (OpenAI, local models)
- Consistent API regardless of backend

**Implementation**:
```csharp
services.AddSingleton<IAIService>(sp => {
    var provider = config["AI:Provider"];
    return provider switch {
        "gemini" => new GeminiService(...),
        "claude" => new ClaudeService(...),
        _ => new GeminiService(...)
    };
});
```

---

### 2. **Composite Primary Key for Bars**
**Decision**: Use `(Symbol, Timestamp)` as primary key for `futures_bars`

**Rationale**:
- Natural uniqueness constraint
- Efficient queries by symbol + date range
- No need for surrogate key
- Better index performance

**Trade-offs**:
- More complex joins (but rare in this app)
- Larger index size (acceptable for query performance)

---

### 3. **Pre-calculated Indicators in Database**
**Decision**: Store EMA, VWAP, avgVolume20 in bar table

**Rationale**:
- **Performance**: Calculate once vs. every strategy run
- **Consistency**: Same values across all backtests
- **Simplicity**: No need to maintain historical windows

**Trade-offs**:
- Storage: ~40 bytes/bar extra (negligible)
- Flexibility: Adding new indicators requires data reload

**Future**: Could move to calculated columns or views

---

### 4. **Anonymous User Pattern**
**Decision**: Create default user (ID: 1) for unauthenticated strategies

**Rationale**:
- **Data Integrity**: Maintains foreign key constraints
- **Simple Migration**: Easy to add auth later
- **Audit Trail**: Can identify anonymous vs. real users
- **No Nulls**: Cleaner data model

**Alternative Considered**: Nullable UserId (rejected for data integrity)

---

### 5. **JsonIgnore on Navigation Properties**
**Decision**: Mark all back-references with `[JsonIgnore]`

**Rationale**:
- **Prevents Cycles**: No circular reference errors
- **Cleaner JSON**: Smaller responses
- **Frontend Logic**: Parent ID is sufficient
- **EF Still Works**: Only affects JSON serialization

**Example**:
```csharp
public class Condition {
    public int StrategyId { get; set; }  // ✅ Included in JSON

    [JsonIgnore]
    public Strategy? Strategy { get; set; }  // ❌ Excluded from JSON
}
```

---

### 6. **Redis Caching Strategy**
**Decision**: Cache parsed + analyzed strategies for 30 days

**Cache Key Format**:
```
result:{SYMBOL}:{HASH(description)}:{startDate}:{endDate}
```

**Rationale**:
- **Performance**: Backtest takes 15-30 seconds
- **Cost**: AI API calls cost money
- **Deterministic**: Same inputs = same outputs
- **Long TTL**: Historical data doesn't change

---

### 7. **Error Tracking System**
**Decision**: Build comprehensive error logging with auto-fix suggestions

**Rationale**:
- **User Experience**: Help users fix their strategies
- **Pattern Detection**: Identify common mistakes
- **Debugging**: Track production issues
- **Analytics**: Understand where users struggle

**Key Feature**: Pattern matching engine automatically suggests fixes:
```
Input:  "1.5 * average_volume"
Output: "Change '1.5 * average_volume' to '1.5x_average'"
```

---

### 8. **Expression Parser Design**
**Decision**: Support multiple value formats in conditions

**Formats**:
1. Numbers: `"100.5"`
2. Indicators: `"vwap"`
3. Multipliers: `"1.5x_average"`
4. Time: `"10:30"`

**Rationale**:
- **Flexibility**: Natural language variety
- **Clarity**: `"1.5x_average"` more readable than `"avgVolume20 * 1.5"`
- **Safety**: Regex validation prevents injection

**Parser Logic**:
```csharp
1. Try parse as decimal
2. Check multiplier format (regex)
3. Check time format (HH:MM)
4. Try resolve as indicator
5. Throw error with context
```

---

### 9. **Futures Contract Specifications**
**Decision**: Hard-code contract specs in `FuturesContractSpecs` class

**Rationale**:
- **Stability**: Specs rarely change
- **Performance**: No database lookups
- **Type Safety**: Compile-time validation
- **Simplicity**: No admin UI needed

**Future**: Could move to database table if needed

---

### 10. **Next.js App Router**
**Decision**: Use Next.js 15 App Router (not Pages Router)

**Rationale**:
- **Modern**: Latest patterns from Vercel
- **Performance**: Server components by default
- **DX**: Simpler routing, layouts
- **Future-proof**: Direction of Next.js

**Trade-off**: Async params require Promise handling

---

## Error Handling

### Error Types

1. **Validation Errors** (400 Bad Request)
   - Invalid date ranges
   - Unsupported symbols
   - Missing required fields

2. **Processing Errors** (500 Internal Server Error)
   - AI service failures
   - Database connection issues
   - Unexpected exceptions

3. **Strategy Evaluation Errors** (Logged to `strategy_errors`)
   - Expression parsing failures
   - Invalid indicator values
   - Runtime exceptions

### Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Invalid date range",
  "status": 400,
  "detail": "End date must be after start date"
}
```

### Error Logging Strategy

All errors are logged with:
- **Timestamp**: When it occurred
- **Context**: What was being processed
- **Stack Trace**: Full exception details
- **Suggested Fix**: Automatic recommendations

**Example Log**:
```
[2024-10-12 15:30:45] ERROR: Error evaluating condition: volume > 1.5 * average_volume
Failed Expression: "1.5 * average_volume"
Suggested Fix: Change '1.5 * average_volume' to '1.5x_average'
Context: {Symbol: ES, Timestamp: 2024-06-15T10:30:00Z, StrategyId: 5}
```

---

## Future Enhancements

### High Priority

1. **User Authentication**
   - JWT-based auth
   - OAuth (Google, GitHub)
   - User dashboard
   - Strategy ownership

2. **Real-time Trading**
   - Live data integration
   - Paper trading mode
   - Broker API connections
   - Position management

3. **Advanced Indicators**
   - Bollinger Bands
   - MACD
   - Stochastic
   - Custom indicators

4. **Strategy Optimizer**
   - Parameter optimization
   - Walk-forward analysis
   - Monte Carlo simulation
   - Genetic algorithms

### Medium Priority

5. **Portfolio Management**
   - Multiple strategies
   - Position sizing algorithms
   - Risk management
   - Correlation analysis

6. **Advanced Charting**
   - Interactive charts
   - Trade markers
   - Indicator overlays
   - Comparison views

7. **Backtesting Enhancements**
   - Slippage modeling
   - Commission/fees
   - Multiple timeframes
   - Market impact

8. **Collaboration Features**
   - Share strategies
   - Community ratings
   - Discussion forums
   - Strategy marketplace

### Low Priority

9. **Mobile App**
   - React Native
   - Push notifications
   - Mobile-optimized UI

10. **Email Notifications**
    - Strategy results
    - Error alerts
    - Performance summaries

11. **Export/Import**
    - CSV export
    - PDF reports
    - JSON strategy files
    - TradeStation integration

12. **Admin Dashboard**
    - User management
    - System metrics
    - Error monitoring
    - Database tools

---

## Performance Optimization

### Current Optimizations

1. **Database Indexes**
   - All foreign keys indexed
   - Composite indexes for common queries
   - Covering indexes for hot paths

2. **Redis Caching**
   - 30-day TTL for results
   - AI response caching
   - Reduces repeated backtests by 100x

3. **Query Optimization**
   - Eager loading with `.Include()`
   - AsNoTracking() for read-only queries
   - Pagination for large result sets

4. **AI Service Optimization**
   - Connection pooling
   - Retry with exponential backoff
   - Response compression

### Future Optimizations

1. **Database**
   - Partitioning `futures_bars` by symbol
   - Read replicas
   - Connection pooling

2. **Caching**
   - CDN for static assets
   - API response caching
   - Client-side caching

3. **Compute**
   - Async/parallel processing
   - Background jobs for heavy tasks
   - Load balancing

---

## Testing Strategy

### Unit Tests (TODO)
- Service logic
- Indicator calculations
- Expression parsing
- Error handling

### Integration Tests (TODO)
- API endpoints
- Database operations
- AI service mocks
- Cache behavior

### End-to-End Tests (TODO)
- Full strategy analysis flow
- Frontend user journeys
- Error scenarios

### Manual Testing Checklist
- [ ] Create strategy with all condition types
- [ ] Test all operators (>, <, crosses_above, etc.)
- [ ] Test all value formats (number, indicator, multiplier, time)
- [ ] Verify error messages are helpful
- [ ] Check results page displays correctly
- [ ] Test caching (same request returns fast)
- [ ] Verify database constraints work
- [ ] Test all supported symbols

---

## Troubleshooting

### Common Issues

**Issue**: "Cannot connect to database"
- **Solution**: Check PostgreSQL is running, verify connection string

**Issue**: "AI API key invalid"
- **Solution**: Get key from https://aistudio.google.com/app/apikey

**Issue**: "No market data available"
- **Solution**: Run DataLoader to populate `futures_bars` table

**Issue**: "Expression parsing error: 1.5 * average"
- **Solution**: Use `1.5x_average` format instead

**Issue**: "Circular reference detected"
- **Solution**: Ensure all navigation properties have `[JsonIgnore]`

**Issue**: "Strategy with ID 0 does not exist"
- **Solution**: Check that strategy is saved before redirect

---

## Maintenance

### Database Maintenance

**Backup**:
```bash
pg_dump -U postgres trading_strategy > backup.sql
```

**Restore**:
```bash
psql -U postgres trading_strategy < backup.sql
```

**Vacuum** (monthly):
```sql
VACUUM ANALYZE;
```

### Cache Maintenance

**Clear Redis cache**:
```bash
redis-cli FLUSHDB
```

**Monitor cache size**:
```bash
redis-cli INFO memory
```

### Log Rotation

Logs are written to console by default. For production:
- Use Serilog for structured logging
- Configure rolling file logs
- Set up log aggregation (ELK, Datadog)

---

## Contributing

### Code Style
- Follow Microsoft C# conventions
- Use XML documentation comments
- Keep methods under 50 lines
- Favor composition over inheritance

### Git Workflow
1. Create feature branch
2. Make changes
3. Write tests
4. Update documentation
5. Submit pull request

### Database Changes
1. Create migration: `dotnet ef migrations add MigrationName`
2. Review generated SQL
3. Test on dev database
4. Document in migration comments

---

## License

MIT License - see LICENSE file for details

---

## Support

For issues, questions, or contributions:
- GitHub Issues: [link]
- Documentation: This file
- API Docs: http://localhost:5000/swagger

---

**Last Updated**: October 12, 2024
**Version**: 1.0.0
**Maintainer**: Development Team
