# Environment Variables & Configuration Guide

Complete reference for all environment variables and configuration options in the AITrader platform.

## Table of Contents

- [Overview](#overview)
- [Backend Configuration (.NET)](#backend-configuration-net)
- [Frontend Configuration (Next.js)](#frontend-configuration-nextjs)
- [Database Configuration](#database-configuration)
- [AI Provider Configuration](#ai-provider-configuration)
- [Redis Configuration](#redis-configuration)
- [Environment-Specific Settings](#environment-specific-settings)
- [Security Best Practices](#security-best-practices)

## Overview

AITrader uses two configuration systems:

1. **Backend**: `appsettings.json` + `appsettings.{Environment}.json` (.NET Configuration)
2. **Frontend**: `.env.local` (Next.js Environment Variables)

**Never commit sensitive data** to version control. Use `.gitignore` to exclude:
- `appsettings.Development.json`
- `appsettings.Production.json`
- `.env.local`
- `.env.production.local`

## Backend Configuration (.NET)

### Configuration File Locations

```
TradingStrategyAPI/TradingStrategyAPI/
├── appsettings.json                    # Base configuration (committed)
├── appsettings.Development.json        # Development overrides (gitignored)
└── appsettings.Production.json         # Production overrides (gitignored)
```

### Complete appsettings.json Structure

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
    "PostgreSQL": "Host=localhost;Database=trading_strategy;Username=postgres;Password=your_password;Pooling=true;MinPoolSize=0;MaxPoolSize=100",
    "Redis": "localhost:6379,abortConnect=false,connectRetry=3,connectTimeout=5000"
  },
  "AI": {
    "Provider": "gemini",
    "Gemini": {
      "ApiKey": "your-gemini-api-key-here",
      "Model": "gemini-1.5-flash",
      "MaxRetries": 3,
      "TimeoutSeconds": 60
    },
    "Claude": {
      "ApiKey": "your-claude-api-key-here",
      "Model": "claude-3-5-sonnet-20241022",
      "MaxRetries": 3,
      "TimeoutSeconds": 60
    }
  },
  "Cache": {
    "StrategyResultTTLDays": 30,
    "MarketDataTTLHours": 24,
    "IndicatorCalculationTTLHours": 24
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"],
    "AllowCredentials": true
  },
  "Performance": {
    "MaxBarsPerQuery": 100000,
    "MaxTradesPerResult": 10000,
    "QueryTimeoutSeconds": 120
  }
}
```

### Configuration Sections

#### 1. Logging

Controls application logging levels.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Logging:LogLevel:Default` | string | `"Information"` | Default log level |
| `Logging:LogLevel:Microsoft.AspNetCore` | string | `"Warning"` | ASP.NET Core framework logs |
| `Logging:LogLevel:Microsoft.EntityFrameworkCore` | string | `"Warning"` | EF Core database logs |

**Available Log Levels**: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`

**Production Recommendation**: Set `Default` to `"Warning"` to reduce log noise.

#### 2. AllowedHosts

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `AllowedHosts` | string | `"*"` | Allowed host headers (semicolon-separated) |

**Production**: Set to your actual domain(s), e.g., `"api.yourdomain.com;www.yourdomain.com"`

#### 3. ConnectionStrings

##### PostgreSQL

**Format**: Npgsql connection string

```
Host={server};Port={port};Database={db};Username={user};Password={pass};{options}
```

**Parameters**:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Host` | string | `localhost` | PostgreSQL server hostname |
| `Port` | int | `5432` | PostgreSQL server port |
| `Database` | string | `trading_strategy` | Database name |
| `Username` | string | `postgres` | Database user |
| `Password` | string | - | Database password (**required**) |
| `Pooling` | bool | `true` | Enable connection pooling |
| `MinPoolSize` | int | `0` | Minimum connections in pool |
| `MaxPoolSize` | int | `100` | Maximum connections in pool |
| `Timeout` | int | `30` | Connection timeout (seconds) |
| `CommandTimeout` | int | `30` | Command timeout (seconds) |
| `SslMode` | string | `Prefer` | SSL mode: `Disable`, `Prefer`, `Require` |

**Example (Local Development)**:
```json
"PostgreSQL": "Host=localhost;Database=trading_strategy;Username=postgres;Password=devpass123"
```

**Example (Production with SSL)**:
```json
"PostgreSQL": "Host=prod-db.example.com;Port=5432;Database=trading_strategy_prod;Username=app_user;Password=SecurePass123!;SslMode=Require;Pooling=true;MaxPoolSize=50"
```

**Example (Cloud - AWS RDS)**:
```json
"PostgreSQL": "Host=mydb.abc123.us-east-1.rds.amazonaws.com;Port=5432;Database=trading_strategy;Username=admin;Password=YourPassword;SslMode=Require"
```

##### Redis

**Format**: StackExchange.Redis connection string

```
{host}:{port},{option}={value},{option}={value}
```

**Parameters**:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `{host}:{port}` | string | `localhost:6379` | Redis server and port |
| `password` | string | - | Redis password (if AUTH enabled) |
| `ssl` | bool | `false` | Use SSL/TLS connection |
| `abortConnect` | bool | `true` | Abort on initial connection failure |
| `connectRetry` | int | `3` | Number of connection retry attempts |
| `connectTimeout` | int | `5000` | Connection timeout (milliseconds) |
| `syncTimeout` | int | `5000` | Synchronous operation timeout (ms) |
| `allowAdmin` | bool | `false` | Allow admin commands (FLUSHDB, etc.) |

**Example (Local Development)**:
```json
"Redis": "localhost:6379,abortConnect=false"
```

**Example (Production with Password)**:
```json
"Redis": "redis.example.com:6379,password=YourRedisPass,ssl=true,abortConnect=false,connectRetry=3"
```

**Example (Cloud - Azure Redis)**:
```json
"Redis": "mycache.redis.cache.windows.net:6380,password=PrimaryKey,ssl=true,abortConnect=false"
```

#### 4. AI Configuration

##### AI:Provider

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `AI:Provider` | string | `"gemini"` | AI provider to use: `"gemini"` or `"claude"` |

**Recommendation**: Use `"gemini"` for development (free tier), `"claude"` for production (higher quality).

##### AI:Gemini

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `AI:Gemini:ApiKey` | string | - | Google Gemini API key (**required if using Gemini**) |
| `AI:Gemini:Model` | string | `"gemini-1.5-flash"` | Model to use |
| `AI:Gemini:MaxRetries` | int | `3` | Retry attempts on failure |
| `AI:Gemini:TimeoutSeconds` | int | `60` | Request timeout |

**Get API Key**: https://aistudio.google.com/app/apikey

**Available Models**:
- `gemini-1.5-flash` - Fast, free tier (recommended for dev)
- `gemini-1.5-pro` - Higher quality, paid

##### AI:Claude

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `AI:Claude:ApiKey` | string | - | Anthropic Claude API key (**required if using Claude**) |
| `AI:Claude:Model` | string | `"claude-3-5-sonnet-20241022"` | Model to use |
| `AI:Claude:MaxRetries` | int | `3` | Retry attempts on failure |
| `AI:Claude:TimeoutSeconds` | int | `60` | Request timeout |

**Get API Key**: https://console.anthropic.com/

**Available Models**:
- `claude-3-5-sonnet-20241022` - Highest quality (recommended)
- `claude-3-opus-20240229` - Most capable (expensive)
- `claude-3-haiku-20240307` - Fast and cheap

#### 5. Cache Configuration

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Cache:StrategyResultTTLDays` | int | `30` | Days to cache strategy backtest results |
| `Cache:MarketDataTTLHours` | int | `24` | Hours to cache market data queries |
| `Cache:IndicatorCalculationTTLHours` | int | `24` | Hours to cache indicator calculations |

**Recommendation**: Keep defaults unless you have specific caching needs.

#### 6. CORS Configuration

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Cors:AllowedOrigins` | array | `["http://localhost:3000"]` | Allowed frontend origins |
| `Cors:AllowCredentials` | bool | `true` | Allow cookies/auth headers |

**Production Example**:
```json
"Cors": {
  "AllowedOrigins": [
    "https://app.yourdomain.com",
    "https://www.yourdomain.com"
  ],
  "AllowCredentials": true
}
```

#### 7. Performance Configuration

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Performance:MaxBarsPerQuery` | int | `100000` | Maximum bars to fetch in single query |
| `Performance:MaxTradesPerResult` | int | `10000` | Maximum trades per backtest result |
| `Performance:QueryTimeoutSeconds` | int | `120` | Database query timeout |

**Tuning**: Increase `QueryTimeoutSeconds` if backtests are timing out on large datasets.

---

## Frontend Configuration (Next.js)

### Configuration File Location

```
trading-strategy-frontend/
├── .env.example           # Template (committed)
├── .env.local             # Local development (gitignored)
└── .env.production.local  # Production build (gitignored)
```

### Environment Variables

Next.js environment variables must be prefixed with `NEXT_PUBLIC_` to be accessible in the browser.

#### Complete .env.local Structure

```bash
# API Configuration
NEXT_PUBLIC_API_URL=http://localhost:5000

# Optional: API Timeout (milliseconds)
NEXT_PUBLIC_API_TIMEOUT=120000

# Optional: Enable Debug Logging
NEXT_PUBLIC_DEBUG=false

# Optional: Default Theme
NEXT_PUBLIC_DEFAULT_THEME=default

# Optional: Analytics (when implemented)
# NEXT_PUBLIC_GOOGLE_ANALYTICS_ID=G-XXXXXXXXXX
# NEXT_PUBLIC_POSTHOG_KEY=your-posthog-key
```

### Variable Descriptions

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `NEXT_PUBLIC_API_URL` | string | `http://localhost:5000` | Backend API base URL (**required**) |
| `NEXT_PUBLIC_API_TIMEOUT` | number | `120000` | API request timeout (ms) |
| `NEXT_PUBLIC_DEBUG` | bool | `false` | Enable console debug logging |
| `NEXT_PUBLIC_DEFAULT_THEME` | string | `"default"` | Default theme: `default`, `ocean`, `forest`, `sunset`, `midnight`, `terminal` |

### Production Frontend Configuration

**Example (.env.production.local)**:
```bash
NEXT_PUBLIC_API_URL=https://api.yourdomain.com
NEXT_PUBLIC_API_TIMEOUT=120000
NEXT_PUBLIC_DEBUG=false
NEXT_PUBLIC_DEFAULT_THEME=ocean
```

---

## Database Configuration

### PostgreSQL Setup

#### Local Development (Docker)

```bash
docker run -d \
  --name trading-postgres \
  -e POSTGRES_DB=trading_strategy \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=devpass123 \
  -p 5432:5432 \
  -v postgres_data:/var/lib/postgresql/data \
  postgres:16
```

**Connection String**:
```
Host=localhost;Database=trading_strategy;Username=postgres;Password=devpass123
```

#### Production (Managed Database)

**AWS RDS PostgreSQL**:
1. Create RDS PostgreSQL 16 instance
2. Enable SSL
3. Configure security group to allow API server access
4. Use connection endpoint:
   ```
   Host=mydb.abc123.us-east-1.rds.amazonaws.com;Port=5432;Database=trading_strategy;Username=admin;Password=YourSecurePassword;SslMode=Require
   ```

**Azure Database for PostgreSQL**:
```
Host=myserver.postgres.database.azure.com;Port=5432;Database=trading_strategy;Username=admin@myserver;Password=YourPassword;SslMode=Require
```

**Google Cloud SQL**:
```
Host=/cloudsql/project:region:instance;Database=trading_strategy;Username=postgres;Password=YourPassword
```

### Redis Setup

#### Local Development (Docker)

```bash
docker run -d \
  --name trading-redis \
  -p 6379:6379 \
  redis:7-alpine
```

**Connection String**:
```
localhost:6379,abortConnect=false
```

#### Production (Managed Redis)

**AWS ElastiCache**:
```
mycache.abc123.0001.use1.cache.amazonaws.com:6379,ssl=true,abortConnect=false
```

**Azure Cache for Redis**:
```
mycache.redis.cache.windows.net:6380,password=PrimaryKey,ssl=true,abortConnect=false
```

**Redis Cloud**:
```
redis-12345.c123.us-east-1-2.ec2.cloud.redislabs.com:12345,password=YourPassword,ssl=true
```

---

## AI Provider Configuration

### Google Gemini

#### Getting an API Key

1. Go to [Google AI Studio](https://aistudio.google.com/app/apikey)
2. Sign in with Google account
3. Click "Create API Key"
4. Copy the key

#### Free Tier Limits

- **Requests**: 60 requests/minute
- **Tokens**: 1 million tokens/minute
- **Cost**: Free

#### Configuration

```json
"AI": {
  "Provider": "gemini",
  "Gemini": {
    "ApiKey": "AIzaSy...",
    "Model": "gemini-1.5-flash"
  }
}
```

### Anthropic Claude

#### Getting an API Key

1. Go to [Anthropic Console](https://console.anthropic.com/)
2. Sign up/sign in
3. Navigate to API Keys
4. Create new key
5. Add billing information

#### Pricing (as of 2024)

- **Claude 3.5 Sonnet**: $3/million input tokens, $15/million output tokens
- **Claude 3 Opus**: $15/million input tokens, $75/million output tokens
- **Claude 3 Haiku**: $0.25/million input tokens, $1.25/million output tokens

#### Configuration

```json
"AI": {
  "Provider": "claude",
  "Claude": {
    "ApiKey": "sk-ant-...",
    "Model": "claude-3-5-sonnet-20241022"
  }
}
```

#### Cost Estimation

Average strategy analysis:
- Input: ~2,000 tokens (strategy parsing prompt)
- Output: ~500 tokens (JSON response)

**Cost per analysis (Claude 3.5 Sonnet)**:
- Input: 2,000 × $3/1M = $0.006
- Output: 500 × $15/1M = $0.0075
- **Total**: ~$0.014 per strategy

**Monthly estimate (100 analyses/day)**:
- Daily: 100 × $0.014 = $1.40
- Monthly: $42

---

## Environment-Specific Settings

### Development Environment

**appsettings.Development.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=trading_strategy_dev;Username=postgres;Password=devpass",
    "Redis": "localhost:6379"
  },
  "AI": {
    "Provider": "gemini",
    "Gemini": {
      "ApiKey": "your-dev-key"
    }
  }
}
```

**.env.local** (Frontend):
```bash
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_DEBUG=true
```

### Production Environment

**appsettings.Production.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "PostgreSQL": "Host=prod-db.example.com;Database=trading_strategy;Username=app_user;Password=${DB_PASSWORD};SslMode=Require;Pooling=true",
    "Redis": "redis.example.com:6379,password=${REDIS_PASSWORD},ssl=true"
  },
  "AI": {
    "Provider": "claude",
    "Claude": {
      "ApiKey": "${CLAUDE_API_KEY}"
    }
  },
  "Cors": {
    "AllowedOrigins": ["https://app.yourdomain.com"]
  }
}
```

**.env.production.local** (Frontend):
```bash
NEXT_PUBLIC_API_URL=https://api.yourdomain.com
NEXT_PUBLIC_DEBUG=false
```

### Using Environment Variables in Configuration

.NET supports environment variable substitution using `${VAR_NAME}` syntax.

**Example**:
```json
"ConnectionStrings": {
  "PostgreSQL": "Host=${DB_HOST};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"
}
```

**Set environment variables**:
```bash
export DB_HOST=localhost
export DB_NAME=trading_strategy
export DB_USER=postgres
export DB_PASSWORD=SecurePass123
```

---

## Security Best Practices

### 1. Never Commit Secrets

**Add to .gitignore**:
```
appsettings.Development.json
appsettings.Production.json
*.local.json
.env.local
.env.*.local
```

### 2. Use Strong Passwords

- **Database**: 16+ characters, mix of upper/lower/numbers/symbols
- **Redis**: Enable AUTH with strong password in production
- **API Keys**: Rotate periodically

### 3. Enable SSL/TLS

- **PostgreSQL**: Use `SslMode=Require` in production
- **Redis**: Use `ssl=true` for production connections
- **HTTPS**: Always use HTTPS for API and frontend in production

### 4. Limit Access

- **Database**: Restrict to API server IP only
- **Redis**: Use firewall rules to limit access
- **CORS**: Whitelist only your frontend domain(s)

### 5. Use Managed Services

Consider using managed database services:
- AWS RDS (PostgreSQL)
- Azure Database for PostgreSQL
- Google Cloud SQL
- AWS ElastiCache (Redis)
- Azure Cache for Redis

These provide:
- Automatic backups
- SSL/TLS encryption
- Firewall rules
- Monitoring and alerts

### 6. Environment Variable Management

**Development**: Use `.env` files (gitignored)

**Production**: Use secret management services:
- **AWS**: AWS Secrets Manager or Parameter Store
- **Azure**: Azure Key Vault
- **Google Cloud**: Secret Manager
- **Docker**: Docker Secrets
- **Kubernetes**: Kubernetes Secrets

---

## Troubleshooting

### Backend Can't Connect to Database

**Error**: `Npgsql.NpgsqlException: Connection refused`

**Solutions**:
1. Check PostgreSQL is running: `docker ps` or `sudo systemctl status postgresql`
2. Verify connection string host and port
3. Check firewall allows port 5432
4. Verify credentials are correct

### Backend Can't Connect to Redis

**Error**: `StackExchange.Redis.RedisConnectionException`

**Solutions**:
1. Check Redis is running: `docker ps` or `sudo systemctl status redis`
2. Verify connection string
3. If using password, ensure it's correct
4. Check firewall allows port 6379

### AI API Errors

**Error**: `AI API key is invalid`

**Solutions**:
1. Verify API key is correct (no extra spaces)
2. Check API key has not expired
3. Ensure billing is set up (for Claude)
4. Check rate limits haven't been exceeded

### Frontend Can't Reach API

**Error**: `Network Error` or `ERR_CONNECTION_REFUSED`

**Solutions**:
1. Verify `NEXT_PUBLIC_API_URL` is correct
2. Check backend is running: `http://localhost:5000/swagger`
3. Verify CORS is configured for frontend URL
4. Check firewall rules

### CORS Errors

**Error**: `Access-Control-Allow-Origin header is missing`

**Solutions**:
1. Add frontend URL to `Cors:AllowedOrigins` in backend config
2. Restart backend after config change
3. Clear browser cache
4. Verify URL matches exactly (http vs https, port number)

---

## Complete Configuration Checklist

### Development Setup

- [ ] PostgreSQL installed and running
- [ ] Redis installed and running
- [ ] `appsettings.Development.json` created with database credentials
- [ ] AI API key configured (Gemini or Claude)
- [ ] Frontend `.env.local` created with API URL
- [ ] Database migrations applied: `dotnet ef database update`
- [ ] Backend running: `dotnet run` (port 5000)
- [ ] Frontend running: `npm run dev` (port 3000)

### Production Setup

- [ ] Managed PostgreSQL database provisioned
- [ ] Managed Redis cache provisioned
- [ ] SSL/TLS enabled for all connections
- [ ] Strong passwords for database and Redis
- [ ] AI API key with billing configured
- [ ] `appsettings.Production.json` with production values
- [ ] Frontend `.env.production.local` with production API URL
- [ ] CORS configured for production frontend domain
- [ ] Firewall rules configured
- [ ] Monitoring and alerting set up
- [ ] Backup strategy in place

---

**Need help?** See [DEVELOPMENT.md](DEVELOPMENT.md) for detailed setup guides or open an issue on GitHub.
