using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.DTOs;

/// <summary>
/// Complete trade detail response including chart data and analysis.
/// </summary>
public class TradeDetailResponse
{
    /// <summary>
    /// The trade result with all metrics.
    /// </summary>
    public required TradeResult Trade { get; set; }

    /// <summary>
    /// Detailed analysis of the trade.
    /// </summary>
    public TradeAnalysis? Analysis { get; set; }

    /// <summary>
    /// Chart bars for visualization (setup + trade bars combined).
    /// </summary>
    public List<BarData>? ChartData { get; set; }

    /// <summary>
    /// Indicator values over time for chart overlay.
    /// Key: indicator name, Value: list of values matching ChartData timestamps.
    /// </summary>
    public Dictionary<string, List<decimal>>? IndicatorSeries { get; set; }
}

/// <summary>
/// Simplified bar data for API responses (smaller than full Bar model).
/// </summary>
public class BarData
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

/// <summary>
/// Identified pattern in trade results.
/// </summary>
public class TradePattern
{
    /// <summary>
    /// Pattern identifier (e.g., "afternoon_losses", "high_vix_stops").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Human-readable description of the pattern.
    /// Example: "Losses cluster when entries occur after 2:00 PM"
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// How many trades exhibit this pattern.
    /// </summary>
    public int Frequency { get; set; }

    /// <summary>
    /// Average P&L impact of this pattern.
    /// Negative for losing patterns, positive for winning patterns.
    /// </summary>
    public decimal AvgImpact { get; set; }

    /// <summary>
    /// Pattern type: "positive" (winning pattern) or "negative" (losing pattern).
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Confidence level (0-100) that this pattern is meaningful.
    /// </summary>
    public int Confidence { get; set; }
}

/// <summary>
/// Heatmap data for visualizing performance by dimension.
/// </summary>
public class HeatmapData
{
    /// <summary>
    /// Dimension being analyzed: "hour", "day", "condition", "vix_level".
    /// </summary>
    public required string Dimension { get; set; }

    /// <summary>
    /// Display label for this dimension.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Individual cells in the heatmap.
    /// </summary>
    public required List<HeatmapCell> Cells { get; set; }
}

/// <summary>
/// Single cell in a heatmap.
/// </summary>
public class HeatmapCell
{
    /// <summary>
    /// Label for this cell (e.g., "10:00 AM", "Monday", "Trending").
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Numeric value (P&L, win rate, count, etc.).
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Number of trades in this cell.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Suggested color for visualization: "green", "red", "yellow", "gray".
    /// </summary>
    public required string Color { get; set; }

    /// <summary>
    /// Additional context for tooltip.
    /// Example: "15 trades, 60% win rate, +$750 avg"
    /// </summary>
    public string? Tooltip { get; set; }
}

/// <summary>
/// Paginated trade list response.
/// </summary>
public class TradeListResponse
{
    /// <summary>
    /// List of trades in this page.
    /// </summary>
    public required List<TradeResult> Trades { get; set; }

    /// <summary>
    /// Total number of trades matching the filter.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Summary statistics for the filtered trades.
    /// </summary>
    public TradeListSummary? Summary { get; set; }
}

/// <summary>
/// Summary statistics for a filtered trade list.
/// </summary>
public class TradeListSummary
{
    public int TotalTrades { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Timeouts { get; set; }
    public decimal TotalPnl { get; set; }
    public decimal AvgPnl { get; set; }
    public decimal WinRate { get; set; }
    public decimal AvgWin { get; set; }
    public decimal AvgLoss { get; set; }
    public decimal LargestWin { get; set; }
    public decimal LargestLoss { get; set; }
}

/// <summary>
/// Statistics grouped by a specific dimension.
/// </summary>
public class DimensionStats
{
    public required string Dimension { get; set; }
    public required Dictionary<string, TradeListSummary> Stats { get; set; }
}
