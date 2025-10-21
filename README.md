# AITrader - AI-Powered Trading Strategy Analysis Platform

> A comprehensive backtesting system that transforms natural language trading strategies into executable code, analyzes performance against historical futures market data, and provides AI-generated insights.

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Next.js 15](https://img.shields.io/badge/Next.js-15-000000?logo=nextdotjs)](https://nextjs.org/)
[![React 19](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis)](https://redis.io/)

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Quick Start](#quick-start)
- [Documentation](#documentation)
- [Project Structure](#project-structure)
- [Development](#development)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [License](#license)

## Overview

AITrader is a sophisticated trading strategy analysis platform that combines:

- **AI-Powered Parsing**: Convert natural language descriptions into executable strategies using Claude or Gemini
- **Historical Backtesting**: Test strategies against real 1-minute futures market data (ES, NQ, YM, BTC, CL)
- **Advanced Analytics**: Comprehensive performance metrics, trade-by-trade analysis, and pattern detection
- **Custom Indicators**: Build and use custom technical indicators with a formula language
- **Visual Strategy Builder**: Create strategies visually using an indicator-based condition builder
- **Beautiful UI**: Six stunning themes with dark mode support and responsive design

### What Makes It Special?

1. **Natural Language → Code**: Describe your strategy in plain English, AI converts it to working code
2. **Deep Trade Analysis**: Every trade includes AI-generated narrative, quality scores, and similar trade patterns
3. **Performance Heatmaps**: Visualize strategy performance by hour, day, duration, and market conditions
4. **Custom Indicators**: Create reusable indicators with formulas and share them publicly
5. **Smart Error Tracking**: Automatic fix suggestions when strategies fail evaluation

## Features

### Core Features

- ✅ **Natural Language Strategy Parsing** - Describe strategies in plain English
- ✅ **Visual Condition Builder** - Drag-and-drop indicator-based strategy creation
- ✅ **Multi-Symbol Support** - ES, NQ, YM, BTC, CL futures contracts
- ✅ **Historical Backtesting** - 1-minute bar data with pre-calculated indicators
- ✅ **Comprehensive Metrics** - Win rate, profit factor, Sharpe ratio, max drawdown, MAE/MFE
- ✅ **AI-Generated Insights** - Strategy analysis and trade narratives powered by Claude/Gemini
- ✅ **Performance Heatmaps** - Identify best/worst times to trade
- ✅ **Trade Pattern Detection** - Automatically find similar trades and patterns

### Strategy Management

- ✅ **CRUD Operations** - Create, read, update, delete strategies
- ✅ **Versioning** - Track strategy evolution with version chains
- ✅ **Favorites & Archiving** - Organize strategies efficiently
- ✅ **Tagging System** - Custom tags with colors for organization
- ✅ **Import/Export** - JSON-based strategy portability
- ✅ **Comparison** - Side-by-side analysis of multiple strategies
- ✅ **Search & Filters** - Advanced search with multiple criteria

### Custom Indicators

- ✅ **Built-in Indicators** - EMA, SMA, RSI, MACD, Bollinger Bands, ATR, ADX, Stochastic, VWAP
- ✅ **Custom Formulas** - Create indicators with mathematical expressions
- ✅ **Public Sharing** - Share indicators with other users
- ✅ **Parameter Configuration** - Customizable indicator parameters
- ✅ **Live Calculation** - Preview indicator values for any date range

### User Experience

- ✅ **6 Beautiful Themes** - Default, Ocean Blue, Forest Green, Sunset Orange, Midnight Purple, Terminal
- ✅ **Dark Mode Support** - All themes with automatic dark variants
- ✅ **Responsive Design** - Mobile-first, works on all screen sizes
- ✅ **Interactive Charts** - TradingView-style candlestick charts with indicators
- ✅ **Pagination & Filtering** - Efficient handling of large datasets
- ✅ **Error Tracking** - Detailed error logs with auto-fix suggestions

## Architecture

### High-Level System Design

```
┌─────────────────────────────────────────────────────────────────┐
│                        USER BROWSER                              │
│                     (Next.js 15 Frontend)                        │
│                    http://localhost:3000                         │
└────────────────────────────┬────────────────────────────────────┘
                             │ HTTP/REST API
                             │
┌────────────────────────────▼────────────────────────────────────┐
│                    ASP.NET Core 8.0 API                         │
│                    http://localhost:5000                         │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │ Controllers  │──│  Services    │──│  Models      │         │
│  │              │  │              │  │              │         │
│  │ - Strategy   │  │ - AI         │  │ - Strategy   │         │
│  │ - Indicator  │  │ - Scanner    │  │ - Trade      │         │
│  │ - Tag        │  │ - Evaluator  │  │ - Indicator  │         │
│  └──────────────┘  └──────────────┘  └──────────────┘         │
└─────────────┬──────────────┬──────────────┬───────────────────┘
              │              │              │
    ┌─────────▼─────┐  ┌────▼────┐  ┌─────▼──────┐
    │  PostgreSQL   │  │  Redis  │  │   AI APIs  │
    │   (Database)  │  │ (Cache) │  │ Claude/    │
    │   Port 5432   │  │  6379   │  │ Gemini     │
    └───────────────┘  └─────────┘  └────────────┘
```

### Data Flow: Strategy Analysis

```
1. User Input
   └─> Natural language description OR Visual condition builder

2. Frontend Validation
   └─> Date range, symbol, required fields

3. API Call
   └─> POST /api/strategy/analyze

4. Redis Cache Check
   ├─> Cache Hit → Return cached result (instant)
   └─> Cache Miss → Continue

5. AI Parsing (Claude/Gemini)
   └─> Convert description to Strategy object with conditions

6. Database Save
   └─> Store strategy with ID

7. Historical Data Fetch
   └─> Load 1-minute bars for symbol + date range

8. Backtest Execution
   ├─> Evaluate entry conditions bar-by-bar
   ├─> Track positions with stop loss/take profit
   └─> Generate TradeResult objects

9. Results Analysis
   ├─> Calculate performance metrics
   ├─> AI-generated insights
   └─> Trade quality scoring

10. Save & Cache Results
    ├─> Store in database
    └─> Cache in Redis (30 days)

11. Return Response
    └─> Strategy + Results + Trades + AI insights
```

## Tech Stack

### Backend (.NET)

| Technology | Version | Purpose |
|------------|---------|---------|
| ASP.NET Core | 8.0 | Web API framework |
| Entity Framework Core | 8.0 | ORM for database access |
| PostgreSQL | 16+ | Primary database |
| Npgsql | 8.0 | PostgreSQL driver |
| Redis | 7+ | Caching layer |
| StackExchange.Redis | 2.7 | Redis client |
| Swashbuckle | 6.5 | Swagger/OpenAPI docs |

### Frontend (Next.js + React)

| Technology | Version | Purpose |
|------------|---------|---------|
| Next.js | 15.5 | React framework with App Router |
| React | 19.2 | UI library |
| TypeScript | 5.9 | Type safety |
| TailwindCSS | 4.1 | Utility-first CSS |
| shadcn/ui | Latest | Radix UI components |
| TanStack Query | 5.90 | Data fetching & caching |
| Axios | 1.12 | HTTP client |
| Lightweight Charts | 5.0 | TradingView-style charts |
| date-fns | 4.1 | Date utilities |
| Zod | 4.1 | Schema validation |

### AI Providers

- **Anthropic Claude** 3.5 Sonnet (Paid, higher quality)
- **Google Gemini** 1.5 Flash (Free tier available)

## Quick Start

### Prerequisites

Ensure you have the following installed:

- ✅ Node.js 20+
- ✅ .NET 8.0 SDK
- ✅ PostgreSQL 16+
- ✅ Redis 7+
- ✅ Git

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/AITrader.git
cd AITrader
```

### 2. Backend Setup

```bash
# Navigate to backend
cd TradingStrategyAPI/TradingStrategyAPI

# Restore dependencies
dotnet restore

# Configure database connection (create appsettings.Development.json)
cat > appsettings.Development.json << 'EOF'
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=trading_strategy;Username=postgres;Password=your_password",
    "Redis": "localhost:6379"
  },
  "AI": {
    "Provider": "gemini",
    "Gemini": {
      "ApiKey": "your-gemini-api-key-here",
      "Model": "gemini-1.5-flash"
    },
    "Claude": {
      "ApiKey": "your-claude-api-key-here",
      "Model": "claude-3-5-sonnet-20241022"
    }
  }
}
EOF

# Run database migrations
dotnet ef database update

# Start the API
dotnet run
```

API will be available at: `http://localhost:5000`
Swagger UI: `http://localhost:5000/swagger`

### 3. Frontend Setup

```bash
# Navigate to frontend (in a new terminal)
cd trading-strategy-frontend

# Install dependencies
npm install

# Create environment file
cp .env.example .env.local

# Start development server
npm run dev
```

Frontend will be available at: `http://localhost:3000`

### 4. Load Sample Data (Optional)

```bash
# Navigate to data loader
cd TradingStrategyAPI/TradingStrategyAPI.DataLoader

# Run data loader to populate market data
dotnet run
```

### 5. Test the Application

1. Open `http://localhost:3000` in your browser
2. Enter a strategy description: "Buy when price crosses above VWAP and volume is greater than 1.5x average, with stop at 10 points and target at 20 points"
3. Select symbol: ES
4. Choose date range
5. Click "Analyze Strategy"
6. View results with performance metrics and trade analysis

## Documentation

### Comprehensive Documentation

- **[Backend API Documentation](TradingStrategyAPI/DOCUMENTATION.md)** - Complete backend architecture, database schema, API endpoints (1470 lines)
- **[Frontend README](trading-strategy-frontend/README.md)** - Frontend setup and project structure
- **[API Client Documentation](trading-strategy-frontend/lib/API_CLIENT_DOCUMENTATION.md)** - Type-safe API client usage
- **[Themes Guide](trading-strategy-frontend/THEMES.md)** - Color themes and customization
- **[Environment Variables Guide](ENVIRONMENT_VARIABLES.md)** - Complete configuration reference
- **[Development Guide](DEVELOPMENT.md)** - Development workflow and best practices
- **[Deployment Guide](DEPLOYMENT.md)** - Production deployment instructions
- **[Custom Indicator Formula Language](CUSTOM_INDICATORS.md)** - Formula syntax and examples
- **[Database Migrations Guide](DATABASE_MIGRATIONS.md)** - Migration management

### Quick Reference

#### API Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/strategy/analyze` | POST | Analyze natural language strategy |
| `/api/strategy/list` | GET | List all strategies |
| `/api/strategy/{id}` | GET | Get strategy details |
| `/api/strategy/{id}` | PUT | Update strategy |
| `/api/strategy/{id}` | DELETE | Archive strategy |
| `/api/strategy/symbols` | GET | Get supported symbols |
| `/api/strategy/compare` | POST | Compare strategies |
| `/api/indicator/built-in` | GET | Get built-in indicators |
| `/api/indicator/my` | GET | Get custom indicators |
| `/api/indicator` | POST | Create custom indicator |

Full API documentation: `http://localhost:5000/swagger`

#### Supported Trading Symbols

| Symbol | Name | Contract Size | Tick Value |
|--------|------|---------------|------------|
| ES | E-mini S&P 500 | $50/point | $12.50 |
| NQ | E-mini NASDAQ-100 | $20/point | $5.00 |
| YM | E-mini Dow Jones | $5/point | $5.00 |
| BTC | Bitcoin Futures | $5/point | $25.00 |
| CL | Crude Oil | $1000/point | $10.00 |

#### Built-in Technical Indicators

- **Trend**: EMA (9/20/50), SMA, VWAP
- **Momentum**: RSI, MACD, Stochastic
- **Volatility**: Bollinger Bands, ATR, ADX
- **Volume**: Average Volume (20-period)
- **Price**: Previous Day High/Low, Open, High, Low, Close

## Project Structure

```
AITrader/
├── TradingStrategyAPI/              # Backend (.NET 8.0)
│   ├── TradingStrategyAPI/          # Main API project
│   │   ├── Controllers/             # API endpoints
│   │   ├── Services/                # Business logic
│   │   ├── Models/                  # Data entities
│   │   ├── DTOs/                    # Data transfer objects
│   │   ├── Database/                # EF Core context & migrations
│   │   ├── Program.cs               # Startup configuration
│   │   └── TradingStrategyAPI.csproj
│   │
│   ├── TradingStrategyAPI.DataLoader/  # Data loading utility
│   │   ├── Services/                # CSV parsing
│   │   ├── Models/                  # Data models
│   │   └── Program.cs
│   │
│   └── DOCUMENTATION.md             # Complete backend docs (1470 lines)
│
├── trading-strategy-frontend/       # Frontend (Next.js 15 + React 19)
│   ├── app/                         # Next.js App Router
│   │   ├── page.tsx                 # Home page
│   │   ├── layout.tsx               # Root layout
│   │   ├── results/[id]/            # Strategy results
│   │   ├── strategies/              # Strategy management
│   │   ├── indicators/              # Indicator management
│   │   └── learn/                   # Learning hub
│   │
│   ├── components/                  # React components (46 total)
│   │   ├── StrategyForm.tsx         # Main strategy input
│   │   ├── ResultsSummary.tsx       # Performance metrics
│   │   ├── indicators/              # Indicator components (11)
│   │   ├── trades/                  # Trade analysis components (5)
│   │   └── ui/                      # shadcn/ui components (17)
│   │
│   ├── lib/                         # Utilities
│   │   ├── api-client.ts            # Type-safe API client (873 lines)
│   │   ├── types.ts                 # TypeScript interfaces (695 lines)
│   │   ├── themes.ts                # Theme definitions (415 lines)
│   │   ├── hooks/                   # Custom React hooks (5 files)
│   │   └── indicator-definitions.ts # Indicator registry
│   │
│   ├── README.md                    # Frontend setup
│   ├── THEMES.md                    # Theme documentation
│   └── API_CLIENT_DOCUMENTATION.md  # API client guide
│
├── .git/                            # Git repository
├── .gitignore                       # Git ignore rules
├── AITrader.sln                     # .NET solution file
├── README.md                        # This file
├── ENVIRONMENT_VARIABLES.md         # Configuration guide
├── DEVELOPMENT.md                   # Development guide
├── DEPLOYMENT.md                    # Deployment guide
├── CUSTOM_INDICATORS.md             # Indicator formula guide
└── DATABASE_MIGRATIONS.md           # Migration guide
```

## Development

### Development Workflow

1. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make changes**
   - Backend changes in `TradingStrategyAPI/`
   - Frontend changes in `trading-strategy-frontend/`

3. **Test your changes**
   ```bash
   # Backend tests (when implemented)
   cd TradingStrategyAPI/TradingStrategyAPI
   dotnet test

   # Frontend tests (when implemented)
   cd trading-strategy-frontend
   npm test
   ```

4. **Create database migration** (if schema changed)
   ```bash
   cd TradingStrategyAPI/TradingStrategyAPI
   dotnet ef migrations add YourMigrationName
   dotnet ef database update
   ```

5. **Commit and push**
   ```bash
   git add .
   git commit -m "feat: your feature description"
   git push origin feature/your-feature-name
   ```

6. **Create pull request** on GitHub

### Code Style

- **Backend**: Follow Microsoft C# coding conventions
- **Frontend**: ESLint + Prettier (configured)
- **Commits**: Use conventional commits (feat, fix, docs, style, refactor, test, chore)

### Running in Development

**Concurrent Development** (recommended):

```bash
# Terminal 1: Backend
cd TradingStrategyAPI/TradingStrategyAPI
dotnet watch run

# Terminal 2: Frontend
cd trading-strategy-frontend
npm run dev

# Terminal 3: Redis (Docker)
docker run -d -p 6379:6379 redis:7-alpine

# Terminal 4: PostgreSQL (Docker)
docker run -d -p 5432:5432 \
  -e POSTGRES_DB=trading_strategy \
  -e POSTGRES_PASSWORD=your_password \
  postgres:16
```

### Debugging

**Backend (Visual Studio Code)**:
1. Open `TradingStrategyAPI/` folder
2. Press F5 to start debugging
3. Breakpoints will work in .cs files

**Frontend (Browser DevTools)**:
1. Open `http://localhost:3000`
2. Press F12 for DevTools
3. Use React DevTools extension

## Deployment

### Production Deployment

See **[DEPLOYMENT.md](DEPLOYMENT.md)** for complete instructions.

**Quick Deployment Checklist:**

- [ ] Set production environment variables
- [ ] Configure HTTPS/SSL
- [ ] Set up PostgreSQL with backups
- [ ] Set up Redis with persistence
- [ ] Build frontend for production (`npm run build`)
- [ ] Publish backend (`dotnet publish -c Release`)
- [ ] Configure reverse proxy (Nginx/Caddy)
- [ ] Set up monitoring and logging
- [ ] Configure CORS for production domain
- [ ] Test all functionality in staging first

### Docker Deployment (Coming Soon)

Docker Compose setup for easy deployment is planned for future release.

## Contributing

We welcome contributions! Please see **[CONTRIBUTING.md](CONTRIBUTING.md)** for guidelines.

### How to Contribute

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Write or update tests
5. Update documentation
6. Submit a pull request

### Areas for Contribution

- 🧪 **Testing**: Add unit tests, integration tests, E2E tests
- 📚 **Documentation**: Improve guides, add examples, fix typos
- 🐛 **Bug Fixes**: Fix issues listed in GitHub Issues
- ✨ **Features**: Implement features from the roadmap
- 🎨 **UI/UX**: Improve design, accessibility, responsiveness
- ⚡ **Performance**: Optimize queries, caching, algorithms

## Roadmap

### Phase 1: User Authentication (Next)
- [ ] JWT-based authentication
- [ ] User registration & login
- [ ] OAuth (Google, GitHub)
- [ ] User dashboard with personal strategies

### Phase 2: Live Trading
- [ ] Real-time market data integration
- [ ] Paper trading mode
- [ ] Broker API connections (IBKR, Alpaca)
- [ ] Position management

### Phase 3: Advanced Features
- [ ] Strategy optimizer (parameter tuning)
- [ ] Walk-forward analysis
- [ ] Monte Carlo simulation
- [ ] Portfolio management

### Phase 4: Community
- [ ] Strategy marketplace
- [ ] Public strategy sharing
- [ ] Discussion forums
- [ ] Rating & reviews

### Phase 5: Mobile & Notifications
- [ ] React Native mobile app
- [ ] Push notifications
- [ ] Email alerts
- [ ] SMS notifications

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

### Getting Help

- 📖 **Documentation**: Start with the docs in this repository
- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/yourusername/AITrader/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/yourusername/AITrader/discussions)
- 📧 **Email**: support@aitrader.com (if applicable)

### FAQ

**Q: Do I need a paid AI API key?**
A: No, Google Gemini has a free tier that works great. Claude is optional for higher quality parsing.

**Q: What market data is included?**
A: You need to load your own historical 1-minute OHLCV data for futures. The DataLoader project can help with this.

**Q: Can I use this for live trading?**
A: Not yet. Phase 2 will add live trading support. Currently, it's a backtesting-only platform.

**Q: How accurate are the backtests?**
A: Backtests use historical data without slippage or commission modeling. Results are indicative but not guaranteed for live trading.

**Q: Can I create my own indicators?**
A: Yes! The custom indicator system supports formula-based indicators. See [CUSTOM_INDICATORS.md](CUSTOM_INDICATORS.md).

## Acknowledgments

- **shadcn/ui** - Beautiful UI components
- **TradingView Lightweight Charts** - Charting library
- **Anthropic Claude** - AI strategy parsing
- **Google Gemini** - Free AI API tier
- **Next.js Team** - Amazing React framework
- **.NET Team** - Powerful backend framework

---

**Built with ❤️ by the AITrader Team**

*Star this repo if you find it useful!* ⭐
