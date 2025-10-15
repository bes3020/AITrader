using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Database;

/// <summary>
/// Entity Framework Core database context for the Trading Strategy API.
/// Manages all database entities and their relationships.
/// </summary>
public class TradingDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the TradingDbContext.
    /// </summary>
    /// <param name="options">Database context options</param>
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Users in the system.
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Trading strategies created by users.
    /// </summary>
    public DbSet<Strategy> Strategies { get; set; }

    /// <summary>
    /// Futures 1-minute market data bars (ES, NQ, YM, BTC, CL).
    /// </summary>
    public DbSet<Bar> Bars { get; set; }

    /// <summary>
    /// Entry/exit conditions for strategies.
    /// </summary>
    public DbSet<Condition> Conditions { get; set; }

    /// <summary>
    /// Stop loss configurations.
    /// </summary>
    public DbSet<StopLoss> StopLosses { get; set; }

    /// <summary>
    /// Take profit configurations.
    /// </summary>
    public DbSet<TakeProfit> TakeProfits { get; set; }

    /// <summary>
    /// Aggregate strategy backtest results.
    /// </summary>
    public DbSet<StrategyResult> StrategyResults { get; set; }

    /// <summary>
    /// Individual trade execution results.
    /// </summary>
    public DbSet<TradeResult> TradeResults { get; set; }

    /// <summary>
    /// Detailed analyses of individual trades with AI insights.
    /// </summary>
    public DbSet<TradeAnalysis> TradeAnalyses { get; set; }

    /// <summary>
    /// Strategy evaluation errors for debugging and analysis.
    /// </summary>
    public DbSet<StrategyError> StrategyErrors { get; set; }

    /// <summary>
    /// User-defined tags for organizing strategies.
    /// </summary>
    public DbSet<StrategyTag> StrategyTags { get; set; }

    /// <summary>
    /// Saved strategy comparisons for side-by-side analysis.
    /// </summary>
    public DbSet<StrategyComparison> StrategyComparisons { get; set; }

    /// <summary>
    /// Custom indicators created by users.
    /// </summary>
    public DbSet<CustomIndicator> CustomIndicators { get; set; }

    /// <summary>
    /// Configures entity models and their relationships using Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureStrategy(modelBuilder);
        ConfigureBar(modelBuilder);
        ConfigureCondition(modelBuilder);
        ConfigureStopLoss(modelBuilder);
        ConfigureTakeProfit(modelBuilder);
        ConfigureStrategyResult(modelBuilder);
        ConfigureTradeResult(modelBuilder);
        ConfigureTradeAnalysis(modelBuilder);
        ConfigureStrategyError(modelBuilder);
        ConfigureStrategyTag(modelBuilder);
        ConfigureStrategyComparison(modelBuilder);
        ConfigureCustomIndicator(modelBuilder);
    }

    /// <summary>
    /// Configures the User entity.
    /// </summary>
    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Unique index on email
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("ix_users_email");

            // Default value for CreatedAt
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // One-to-many relationship with Strategies
            entity.HasMany(e => e.Strategies)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the Strategy entity.
    /// </summary>
    private void ConfigureStrategy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Strategy>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_strategies_user_id");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_strategies_created_at");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("ix_strategies_is_active");

            // Default values
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.Version)
                .HasDefaultValue(1);

            entity.Property(e => e.MaxPositions)
                .HasDefaultValue(1);

            entity.Property(e => e.PositionSize)
                .HasDefaultValue(1);

            entity.Property(e => e.VersionNumber)
                .HasDefaultValue(1);

            entity.Property(e => e.IsFavorite)
                .HasDefaultValue(false);

            entity.Property(e => e.IsArchived)
                .HasDefaultValue(false);

            // Additional indexes for new fields
            entity.HasIndex(e => e.ParentStrategyId)
                .HasDatabaseName("ix_strategies_parent_strategy_id");

            entity.HasIndex(e => e.IsFavorite)
                .HasDatabaseName("ix_strategies_is_favorite");

            entity.HasIndex(e => e.IsArchived)
                .HasDatabaseName("ix_strategies_is_archived");

            entity.HasIndex(e => e.LastBacktestedAt)
                .HasDatabaseName("ix_strategies_last_backtested_at");

            // Self-referencing relationship for versioning
            entity.HasOne(e => e.ParentStrategy)
                .WithMany(e => e.Versions)
                .HasForeignKey(e => e.ParentStrategyId)
                .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete versions

            // One-to-many relationship with Conditions
            entity.HasMany(e => e.EntryConditions)
                .WithOne(c => c.Strategy)
                .HasForeignKey(c => c.StrategyId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-one relationship with StopLoss
            entity.HasOne(e => e.StopLoss)
                .WithOne(s => s.Strategy)
                .HasForeignKey<StopLoss>(s => s.StrategyId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-one relationship with TakeProfit
            entity.HasOne(e => e.TakeProfit)
                .WithOne(t => t.Strategy)
                .HasForeignKey<TakeProfit>(t => t.StrategyId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many relationship with StrategyResults
            entity.HasMany(e => e.Results)
                .WithOne(r => r.Strategy)
                .HasForeignKey(r => r.StrategyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the Bar entity (multi-symbol futures market data).
    /// Uses composite primary key: (Symbol, Timestamp).
    /// </summary>
    private void ConfigureBar(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bar>(entity =>
        {
            // Table name
            entity.ToTable("futures_bars");

            // Composite primary key: (Symbol, Timestamp)
            entity.HasKey(e => new { e.Symbol, e.Timestamp })
                .HasName("pk_futures_bars");

            // Index on Symbol alone for symbol-specific queries
            entity.HasIndex(e => e.Symbol)
                .HasDatabaseName("ix_bars_symbol");

            // Index on Timestamp alone for time-based queries across symbols
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("ix_bars_timestamp");

            // Composite index for efficient symbol + date range queries
            entity.HasIndex(e => new { e.Symbol, e.Timestamp })
                .HasDatabaseName("ix_bars_symbol_timestamp");

            // Additional index for volume filtering
            entity.HasIndex(e => new { e.Symbol, e.Timestamp, e.Volume })
                .HasDatabaseName("ix_bars_symbol_timestamp_volume");
        });
    }

    /// <summary>
    /// Configures the Condition entity.
    /// </summary>
    private void ConfigureCondition(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Condition>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Index on StrategyId for efficient lookups
            entity.HasIndex(e => e.StrategyId)
                .HasDatabaseName("ix_conditions_strategy_id");

            // Index on Indicator for analysis queries
            entity.HasIndex(e => e.Indicator)
                .HasDatabaseName("ix_conditions_indicator");

            // Index on CustomIndicatorId
            entity.HasIndex(e => e.CustomIndicatorId)
                .HasDatabaseName("ix_conditions_custom_indicator_id");

            // Relationship with CustomIndicator
            entity.HasOne(e => e.CustomIndicator)
                .WithMany()
                .HasForeignKey(e => e.CustomIndicatorId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Configures the StopLoss entity.
    /// </summary>
    private void ConfigureStopLoss(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StopLoss>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Unique index on StrategyId (one-to-one relationship)
            entity.HasIndex(e => e.StrategyId)
                .IsUnique()
                .HasDatabaseName("ix_stop_losses_strategy_id");
        });
    }

    /// <summary>
    /// Configures the TakeProfit entity.
    /// </summary>
    private void ConfigureTakeProfit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TakeProfit>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Unique index on StrategyId (one-to-one relationship)
            entity.HasIndex(e => e.StrategyId)
                .IsUnique()
                .HasDatabaseName("ix_take_profits_strategy_id");
        });
    }

    /// <summary>
    /// Configures the StrategyResult entity.
    /// </summary>
    private void ConfigureStrategyResult(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StrategyResult>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.StrategyId)
                .HasDatabaseName("ix_strategy_results_strategy_id");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_strategy_results_created_at");

            entity.HasIndex(e => new { e.StrategyId, e.CreatedAt })
                .HasDatabaseName("ix_strategy_results_strategy_id_created_at");

            // Default value for CreatedAt
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // One-to-many relationship with TradeResults
            entity.HasMany(e => e.AllTrades)
                .WithOne(t => t.StrategyResult)
                .HasForeignKey(t => t.StrategyResultId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore computed properties
            entity.Ignore(e => e.WorstTrades);
            entity.Ignore(e => e.BestTrades);
        });
    }

    /// <summary>
    /// Configures the TradeResult entity.
    /// </summary>
    private void ConfigureTradeResult(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TradeResult>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.StrategyResultId)
                .HasDatabaseName("ix_trade_results_strategy_result_id");

            entity.HasIndex(e => e.EntryTime)
                .HasDatabaseName("ix_trade_results_entry_time");

            entity.HasIndex(e => e.Result)
                .HasDatabaseName("ix_trade_results_result");

            entity.HasIndex(e => new { e.StrategyResultId, e.Result, e.Pnl })
                .HasDatabaseName("ix_trade_results_strategy_result_id_result_pnl");
        });
    }

    /// <summary>
    /// Configures the TradeAnalysis entity.
    /// </summary>
    private void ConfigureTradeAnalysis(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TradeAnalysis>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Unique index on TradeResultId (one-to-one relationship)
            entity.HasIndex(e => e.TradeResultId)
                .IsUnique()
                .HasDatabaseName("ix_trade_analyses_trade_result_id");

            // Index on market condition for analysis queries
            entity.HasIndex(e => e.MarketCondition)
                .HasDatabaseName("ix_trade_analyses_market_condition");

            // Index on time of day for pattern analysis
            entity.HasIndex(e => e.TimeOfDay)
                .HasDatabaseName("ix_trade_analyses_time_of_day");

            // Index on day of week
            entity.HasIndex(e => e.DayOfWeek)
                .HasDatabaseName("ix_trade_analyses_day_of_week");

            // Composite index for multi-dimensional analysis
            entity.HasIndex(e => new { e.MarketCondition, e.TimeOfDay })
                .HasDatabaseName("ix_trade_analyses_condition_time");

            // Default value for CreatedAt
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // One-to-one relationship with TradeResult
            entity.HasOne(e => e.TradeResult)
                .WithOne(t => t.Analysis)
                .HasForeignKey<TradeAnalysis>(e => e.TradeResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the StrategyError entity.
    /// </summary>
    private void ConfigureStrategyError(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StrategyError>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.StrategyId)
                .HasDatabaseName("ix_strategy_errors_strategy_id");

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("ix_strategy_errors_timestamp");

            entity.HasIndex(e => e.ErrorType)
                .HasDatabaseName("ix_strategy_errors_error_type");

            entity.HasIndex(e => e.IsResolved)
                .HasDatabaseName("ix_strategy_errors_is_resolved");

            entity.HasIndex(e => new { e.ErrorType, e.Timestamp })
                .HasDatabaseName("ix_strategy_errors_error_type_timestamp");

            // Default value for Timestamp
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("NOW()");

            // Default value for IsResolved
            entity.Property(e => e.IsResolved)
                .HasDefaultValue(false);

            // Optional relationship with Strategy
            entity.HasOne(e => e.Strategy)
                .WithMany()
                .HasForeignKey(e => e.StrategyId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Saves changes and automatically updates the UpdatedAt timestamp for strategies.
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Saves changes asynchronously and automatically updates the UpdatedAt timestamp for strategies.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates the UpdatedAt timestamp for modified Strategy entities.
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Strategy && e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            ((Strategy)entry.Entity).UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Configures the StrategyTag entity.
    /// </summary>
    private void ConfigureStrategyTag(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StrategyTag>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_strategy_tags_user_id");

            entity.HasIndex(e => new { e.UserId, e.Name })
                .IsUnique()
                .HasDatabaseName("ix_strategy_tags_user_id_name");

            // Default value for CreatedAt
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // Relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the StrategyComparison entity.
    /// </summary>
    private void ConfigureStrategyComparison(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StrategyComparison>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_strategy_comparisons_user_id");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_strategy_comparisons_created_at");

            // Default value for CreatedAt
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            // Relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the CustomIndicator entity.
    /// </summary>
    private void ConfigureCustomIndicator(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomIndicator>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_custom_indicators_user_id");

            entity.HasIndex(e => new { e.UserId, e.Name })
                .IsUnique()
                .HasDatabaseName("ix_custom_indicators_user_id_name");

            entity.HasIndex(e => e.Type)
                .HasDatabaseName("ix_custom_indicators_type");

            entity.HasIndex(e => e.IsPublic)
                .HasDatabaseName("ix_custom_indicators_is_public");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_custom_indicators_created_at");

            // Default values
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.IsPublic)
                .HasDefaultValue(false);

            entity.Property(e => e.UserId)
                .HasDefaultValue(1);

            // Relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
