using System.Text;
using System.Text.Json;
using TradingStrategyAPI.DTOs;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// Implementation of trade analysis service with AI-powered insights.
/// </summary>
public class TradeAnalyzer : ITradeAnalyzer
{
    private readonly IAIService _aiService;
    private readonly ILogger<TradeAnalyzer> _logger;

    public TradeAnalyzer(IAIService aiService, ILogger<TradeAnalyzer> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<TradeAnalysis> AnalyzeTradeAsync(TradeResult trade, Strategy strategy)
    {
        try
        {
            // Parse indicator values if available
            Dictionary<string, decimal>? entryIndicators = null;
            Dictionary<string, decimal>? exitIndicators = null;

            if (!string.IsNullOrEmpty(trade.IndicatorValues))
            {
                var indicators = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(trade.IndicatorValues);
                entryIndicators = indicators?.GetValueOrDefault("entry");
                exitIndicators = indicators?.GetValueOrDefault("exit");
            }

            // Determine entry reason
            var entryReason = DetermineEntryReason(strategy, entryIndicators);

            // Determine exit reason
            var exitReason = DetermineExitReason(trade);

            // Classify time of day
            var timeOfDay = ClassifyTimeOfDay(trade.EntryTime);

            // Get day of week
            var dayOfWeek = trade.EntryTime.DayOfWeek.ToString();

            // Parse trade bars to classify market condition
            string marketCondition = "unknown";
            decimal? adxValue = null;
            decimal? atrValue = null;

            if (!string.IsNullOrEmpty(trade.TradeBars))
            {
                var bars = JsonSerializer.Deserialize<List<Bar>>(trade.TradeBars);
                if (bars != null && bars.Count > 0)
                {
                    marketCondition = ClassifyMarketCondition(bars);
                    // Calculate ADX and ATR if we have enough bars
                    if (bars.Count >= 14)
                    {
                        adxValue = CalculateADX(bars);
                        atrValue = CalculateATR(bars);
                    }
                }
            }

            // Generate AI insights for what went wrong/right
            string? whatWentWrong = null;
            string? whatWentRight = null;
            string? lessonsLearned = null;

            if (trade.Result == "loss")
            {
                whatWentWrong = await GenerateWhatWentWrongAsync(trade, strategy, entryReason, exitReason, marketCondition);
                lessonsLearned = ExtractLessons(whatWentWrong);
            }
            else if (trade.Result == "win")
            {
                whatWentRight = await GenerateWhatWentRightAsync(trade, strategy, entryReason, exitReason, marketCondition);
                lessonsLearned = ExtractLessons(whatWentRight);
            }

            // Generate full narrative
            var narrative = await GenerateTradeNarrativeAsync(trade, strategy);

            return new TradeAnalysis
            {
                TradeResultId = trade.Id,
                EntryReason = entryReason,
                ExitReason = exitReason,
                MarketCondition = marketCondition,
                TimeOfDay = timeOfDay,
                DayOfWeek = dayOfWeek,
                AdxValue = adxValue,
                AtrValue = atrValue,
                WhatWentWrong = whatWentWrong,
                WhatWentRight = whatWentRight,
                Narrative = narrative,
                LessonsLearned = lessonsLearned,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing trade {TradeId}", trade.Id);

            // Return basic analysis even if AI generation fails
            return new TradeAnalysis
            {
                TradeResultId = trade.Id,
                EntryReason = "Strategy conditions met",
                ExitReason = DetermineExitReason(trade),
                MarketCondition = "unknown",
                TimeOfDay = ClassifyTimeOfDay(trade.EntryTime),
                DayOfWeek = trade.EntryTime.DayOfWeek.ToString(),
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<string> GenerateTradeNarrativeAsync(TradeResult trade, Strategy strategy)
    {
        try
        {
            // Create a temporary result for AI generation
            var tempResult = new StrategyResult
            {
                TotalTrades = 1,
                WinRate = trade.Result == "win" ? 1.0m : 0m,
                TotalPnl = trade.Pnl,
                AvgWin = trade.Result == "win" ? trade.Pnl : 0m,
                AvgLoss = trade.Result == "loss" ? trade.Pnl : 0m,
                MaxDrawdown = trade.Result == "loss" ? trade.Pnl : 0m,
                BacktestStart = trade.EntryTime,
                BacktestEnd = trade.ExitTime ?? trade.EntryTime,
                AllTrades = new List<TradeResult> { trade }
            };

            // Use AI service to generate narrative-style insights
            var narrative = await _aiService.GenerateInsightsAsync(tempResult);
            return narrative ?? "Unable to generate trade narrative.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating narrative for trade {TradeId}", trade.Id);
            return "Error generating trade narrative.";
        }
    }

    public async Task<List<TradePattern>> FindPatternsAsync(List<TradeResult> trades)
    {
        var patterns = new List<TradePattern>();

        if (trades.Count < 5)
        {
            return patterns; // Need at least 5 trades to identify patterns
        }

        // Time-based patterns
        patterns.AddRange(FindTimePatterns(trades));

        // Condition-based patterns
        patterns.AddRange(FindConditionPatterns(trades));

        // Duration-based patterns
        patterns.AddRange(FindDurationPatterns(trades));

        // Excursion patterns (MAE/MFE)
        patterns.AddRange(FindExcursionPatterns(trades));

        // Use AI to find additional patterns
        var aiPatterns = await FindAIPatternsAsync(trades);
        patterns.AddRange(aiPatterns);

        return patterns.OrderByDescending(p => p.Confidence).ToList();
    }

    public async Task<Dictionary<string, DimensionStats>> CalculateTradeStatsByDimensionAsync(List<TradeResult> trades)
    {
        var result = new Dictionary<string, DimensionStats>();

        // Stats by hour of day
        var hourStats = CalculateStatsByHour(trades);
        result["hour"] = new DimensionStats
        {
            Dimension = "Hour of Day",
            Stats = hourStats
        };

        // Stats by day of week
        var dayStats = CalculateStatsByDay(trades);
        result["day"] = new DimensionStats
        {
            Dimension = "Day of Week",
            Stats = dayStats
        };

        // Stats by market condition
        var conditionStats = await CalculateStatsByConditionAsync(trades);
        result["condition"] = new DimensionStats
        {
            Dimension = "Market Condition",
            Stats = conditionStats
        };

        return result;
    }

    public async Task<HeatmapData> GenerateHeatmapAsync(List<TradeResult> trades, string dimension)
    {
        return dimension.ToLower() switch
        {
            "hour" => GenerateHourHeatmap(trades),
            "day" => GenerateDayHeatmap(trades),
            "condition" => await GenerateConditionHeatmapAsync(trades),
            _ => throw new ArgumentException($"Unknown dimension: {dimension}")
        };
    }

    public string ClassifyMarketCondition(List<Bar> bars)
    {
        if (bars.Count < 14)
            return "unknown";

        var adx = CalculateADX(bars);
        var atr = CalculateATR(bars);
        var avgVolume = (decimal)bars.Average(b => b.Volume);
        var recentVolume = (decimal)bars.TakeLast(5).Average(b => b.Volume);

        // Trending: ADX > 25
        if (adx > 25)
            return "trending";

        // Volatile: High ATR and high volume
        if (atr > bars.Average(b => b.High - b.Low) * 1.5m && recentVolume > avgVolume * 1.3m)
            return "volatile";

        // Quiet: Low ATR and low volume
        if (atr < bars.Average(b => b.High - b.Low) * 0.7m && recentVolume < avgVolume * 0.8m)
            return "quiet";

        // Default: ranging
        return "ranging";
    }

    public string ClassifyTimeOfDay(DateTime timestamp)
    {
        var hour = timestamp.Hour;
        var minute = timestamp.Minute;
        var totalMinutes = hour * 60 + minute;

        // 9:30 AM - 12:00 PM (570 - 720 minutes)
        if (totalMinutes >= 570 && totalMinutes < 720)
            return "morning";

        // 12:00 PM - 2:00 PM (720 - 840 minutes)
        if (totalMinutes >= 720 && totalMinutes < 840)
            return "midday";

        // 2:00 PM - 4:00 PM (840 - 960 minutes)
        if (totalMinutes >= 840 && totalMinutes < 960)
            return "afternoon";

        // 4:00 PM - 4:15 PM (960 - 975 minutes)
        if (totalMinutes >= 960 && totalMinutes < 975)
            return "close";

        return "outside_hours";
    }

    public int CalculateEntryQualityScore(TradeResult trade, Strategy strategy)
    {
        int score = 50; // Start at neutral

        // Good entry quality indicators:
        // 1. Multiple conditions aligned (not just one)
        if (strategy.EntryConditions.Count >= 3)
            score += 10;

        // 2. Good risk/reward setup (if MFE > 2x MAE potential)
        if (trade.MaxFavorableExcursion > Math.Abs(trade.MaxAdverseExcursion) * 2)
            score += 15;

        // 3. Strong initial movement (didn't immediately go against position)
        var initialBars = 3;
        if (trade.BarsHeld >= initialBars)
        {
            // If MFE reached early, good sign
            score += 10;
        }

        // 4. Entry during favorable time (morning/midday better than close)
        var timeOfDay = ClassifyTimeOfDay(trade.EntryTime);
        if (timeOfDay == "morning" || timeOfDay == "midday")
            score += 15;
        else if (timeOfDay == "close")
            score -= 20;

        // 5. Not entering at extreme of day's range
        // (Would need bar data to determine this - skip for now)

        return Math.Clamp(score, 0, 100);
    }

    public int CalculateExitQualityScore(TradeResult trade)
    {
        int score = 50; // Start at neutral

        // Good exit quality indicators:
        // 1. Exit captured most of available profit
        if (trade.MaxFavorableExcursion > 0)
        {
            var efficiency = (double)trade.Pnl / (double)trade.MaxFavorableExcursion;
            if (efficiency >= 0.8)
                score += 30; // Excellent exit
            else if (efficiency >= 0.6)
                score += 20; // Good exit
            else if (efficiency >= 0.4)
                score += 10; // Fair exit
            else if (efficiency < 0.2)
                score -= 20; // Gave back too much
        }

        // 2. Didn't let loss run too far
        if (trade.Result == "loss")
        {
            // If stopped out quickly, good
            if (trade.BarsHeld <= 5)
                score += 15;
            else if (trade.BarsHeld > 20)
                score -= 10;
        }

        // 3. Exit at target vs stop
        if (trade.Result == "win")
            score += 20; // Hit target is best outcome
        else if (trade.Result == "timeout")
            score -= 5; // Timeout is suboptimal

        // 4. Minimal MAE relative to final P&L (didn't suffer much during trade)
        if (Math.Abs(trade.MaxAdverseExcursion) < Math.Abs(trade.Pnl) * 0.5m)
            score += 10;

        return Math.Clamp(score, 0, 100);
    }

    // Private helper methods

    private string DetermineEntryReason(Strategy strategy, Dictionary<string, decimal>? indicators)
    {
        var conditions = strategy.EntryConditions.Select(c =>
            $"{c.Indicator} {c.Operator} {c.Value}"
        ).ToList();

        if (conditions.Count == 0)
            return "Strategy conditions met";

        if (indicators != null && indicators.Count > 0)
        {
            var indicatorDesc = string.Join(", ", indicators.Select(kv => $"{kv.Key}={kv.Value:F2}"));
            return $"Conditions met: {string.Join(" AND ", conditions)}. Indicators: {indicatorDesc}";
        }

        return $"Conditions met: {string.Join(" AND ", conditions)}";
    }

    private string DetermineExitReason(TradeResult trade)
    {
        return trade.Result switch
        {
            "win" => $"Take profit target reached (+{trade.Pnl:F2} points)",
            "loss" => $"Stop loss hit ({trade.Pnl:F2} points)",
            "timeout" => "End of trading session / timeout",
            _ => "Unknown exit reason"
        };
    }

    private async Task<string> GenerateWhatWentWrongAsync(TradeResult trade, Strategy strategy, string entryReason, string exitReason, string marketCondition)
    {
        try
        {
            // Create a temp result with just this losing trade for AI analysis
            var tempResult = new StrategyResult
            {
                TotalTrades = 1,
                WinRate = 0m,
                TotalPnl = trade.Pnl,
                AvgLoss = trade.Pnl,
                MaxDrawdown = trade.Pnl,
                BacktestStart = trade.EntryTime,
                BacktestEnd = trade.ExitTime ?? trade.EntryTime,
                AllTrades = new List<TradeResult> { trade }
            };

            var analysis = await _aiService.GenerateInsightsAsync(tempResult);
            return analysis ?? "Unable to generate analysis";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating 'what went wrong' analysis");
            return "Error generating analysis";
        }
    }

    private async Task<string> GenerateWhatWentRightAsync(TradeResult trade, Strategy strategy, string entryReason, string exitReason, string marketCondition)
    {
        try
        {
            // Create a temp result with just this winning trade for AI analysis
            var tempResult = new StrategyResult
            {
                TotalTrades = 1,
                WinRate = 1.0m,
                TotalPnl = trade.Pnl,
                AvgWin = trade.Pnl,
                MaxDrawdown = 0m,
                BacktestStart = trade.EntryTime,
                BacktestEnd = trade.ExitTime ?? trade.EntryTime,
                AllTrades = new List<TradeResult> { trade }
            };

            var analysis = await _aiService.GenerateInsightsAsync(tempResult);
            return analysis ?? "Unable to generate analysis";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating 'what went right' analysis");
            return "Error generating analysis";
        }
    }

    private string BuildNarrativePrompt(TradeResult trade, Strategy strategy)
    {
        return $@"Write a detailed trade narrative in plain English. Tell the story of this trade from entry to exit.

Trade Details:
- Entry: {trade.EntryTime:yyyy-MM-dd HH:mm:ss} at {trade.EntryPrice:F2}
- Exit: {trade.ExitTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Still open"} at {trade.ExitPrice?.ToString("F2") ?? "N/A"}
- P&L: {trade.Pnl:F2} points ({trade.Result.ToUpper()})
- Duration: {trade.BarsHeld} bars
- Max Favorable: +{trade.MaxFavorableExcursion:F2}
- Max Adverse: {trade.MaxAdverseExcursion:F2}
- Strategy: {strategy.Name}
- Direction: {strategy.Direction}

Structure:
1. Setup: What was happening before entry (1-2 sentences)
2. Entry: Why trade was entered (1-2 sentences)
3. The Trade: How price moved, key levels, what happened (2-3 sentences)
4. Exit: How and why the trade ended (1-2 sentences)
5. Conclusion: Key takeaway (1 sentence)

Write in past tense. Be specific about price levels and timing. Keep it under 200 words.";
    }

    private string ExtractLessons(string? analysis)
    {
        if (string.IsNullOrEmpty(analysis))
            return "No lessons identified";

        // Simple extraction: look for sentences containing "avoid", "lesson", "should", etc.
        var sentences = analysis.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var lessons = sentences
            .Where(s => s.Contains("avoid", StringComparison.OrdinalIgnoreCase) ||
                       s.Contains("lesson", StringComparison.OrdinalIgnoreCase) ||
                       s.Contains("should", StringComparison.OrdinalIgnoreCase) ||
                       s.Contains("next time", StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .Select(s => s.Trim())
            .ToList();

        return lessons.Any() ? string.Join(". ", lessons) + "." : "Continue monitoring similar setups";
    }

    private List<TradePattern> FindTimePatterns(List<TradeResult> trades)
    {
        var patterns = new List<TradePattern>();

        // Group by time of day
        var byTime = trades.GroupBy(t => ClassifyTimeOfDay(t.EntryTime));

        foreach (var group in byTime)
        {
            var avgPnl = group.Average(t => t.Pnl);
            var count = group.Count();

            if (count >= 3 && Math.Abs(avgPnl) > 5) // At least 3 trades and meaningful P&L
            {
                var isPositive = avgPnl > 0;
                patterns.Add(new TradePattern
                {
                    Name = $"{group.Key}_performance",
                    Description = isPositive
                        ? $"Trades during {group.Key} show strong performance (+{avgPnl:F2} avg)"
                        : $"Trades during {group.Key} tend to lose ({avgPnl:F2} avg)",
                    Frequency = count,
                    AvgImpact = avgPnl,
                    Type = isPositive ? "positive" : "negative",
                    Confidence = Math.Min(95, 50 + count * 5) // More trades = higher confidence
                });
            }
        }

        return patterns;
    }

    private List<TradePattern> FindConditionPatterns(List<TradeResult> trades)
    {
        var patterns = new List<TradePattern>();

        // Would need to parse market conditions from trade analyses
        // For now, return empty list (will be populated when analyses are loaded)

        return patterns;
    }

    private List<TradePattern> FindDurationPatterns(List<TradeResult> trades)
    {
        var patterns = new List<TradePattern>();

        // Quick exits (< 5 bars)
        var quickExits = trades.Where(t => t.BarsHeld < 5).ToList();
        if (quickExits.Count >= 5)
        {
            var avgPnl = quickExits.Average(t => t.Pnl);
            var isPositive = avgPnl > 0;

            patterns.Add(new TradePattern
            {
                Name = "quick_exit_pattern",
                Description = isPositive
                    ? $"Quick exits (< 5 bars) often profitable (+{avgPnl:F2} avg)"
                    : $"Quick exits (< 5 bars) frequently stopped out ({avgPnl:F2} avg)",
                Frequency = quickExits.Count,
                AvgImpact = avgPnl,
                Type = isPositive ? "positive" : "negative",
                Confidence = Math.Min(85, 40 + quickExits.Count * 3)
            });
        }

        // Long holds (> 20 bars)
        var longHolds = trades.Where(t => t.BarsHeld > 20).ToList();
        if (longHolds.Count >= 3)
        {
            var avgPnl = longHolds.Average(t => t.Pnl);
            var isPositive = avgPnl > 0;

            patterns.Add(new TradePattern
            {
                Name = "long_hold_pattern",
                Description = isPositive
                    ? $"Trades held > 20 bars show patience pays off (+{avgPnl:F2} avg)"
                    : $"Holding > 20 bars often results in giving back profits ({avgPnl:F2} avg)",
                Frequency = longHolds.Count,
                AvgImpact = avgPnl,
                Type = isPositive ? "positive" : "negative",
                Confidence = Math.Min(80, 50 + longHolds.Count * 5)
            });
        }

        return patterns;
    }

    private List<TradePattern> FindExcursionPatterns(List<TradeResult> trades)
    {
        var patterns = new List<TradePattern>();

        // Trades that gave back significant profit
        var gaveBackProfit = trades.Where(t => t.GaveBackProfit() &&
                                               t.Pnl < t.MaxFavorableExcursion * 0.5m).ToList();

        if (gaveBackProfit.Count >= 3)
        {
            var avgPnl = gaveBackProfit.Average(t => t.Pnl);
            patterns.Add(new TradePattern
            {
                Name = "gave_back_profit",
                Description = $"{gaveBackProfit.Count} trades gave back significant profit (avg final P&L: {avgPnl:F2} vs peak profit)",
                Frequency = gaveBackProfit.Count,
                AvgImpact = avgPnl,
                Type = "negative",
                Confidence = Math.Min(90, 60 + gaveBackProfit.Count * 5)
            });
        }

        return patterns;
    }

    private async Task<List<TradePattern>> FindAIPatternsAsync(List<TradeResult> trades)
    {
        // Use AI to find more subtle patterns
        // For now, return empty list (can be enhanced with AI later)
        await Task.CompletedTask;
        return new List<TradePattern>();
    }

    private Dictionary<string, TradeListSummary> CalculateStatsByHour(List<TradeResult> trades)
    {
        var stats = new Dictionary<string, TradeListSummary>();

        var byHour = trades.GroupBy(t => t.EntryTime.Hour);

        foreach (var group in byHour)
        {
            var hour = group.Key;
            var tradeList = group.ToList();

            stats[$"{hour:D2}:00"] = CalculateSummary(tradeList);
        }

        return stats;
    }

    private Dictionary<string, TradeListSummary> CalculateStatsByDay(List<TradeResult> trades)
    {
        var stats = new Dictionary<string, TradeListSummary>();

        var byDay = trades.GroupBy(t => t.EntryTime.DayOfWeek);

        foreach (var group in byDay)
        {
            stats[group.Key.ToString()] = CalculateSummary(group.ToList());
        }

        return stats;
    }

    private async Task<Dictionary<string, TradeListSummary>> CalculateStatsByConditionAsync(List<TradeResult> trades)
    {
        await Task.CompletedTask; // Placeholder for async operations

        var stats = new Dictionary<string, TradeListSummary>();

        // Would need to load trade analyses to get market conditions
        // For now, return empty

        return stats;
    }

    private TradeListSummary CalculateSummary(List<TradeResult> trades)
    {
        var wins = trades.Where(t => t.Result == "win").ToList();
        var losses = trades.Where(t => t.Result == "loss").ToList();
        var timeouts = trades.Where(t => t.Result == "timeout").ToList();

        return new TradeListSummary
        {
            TotalTrades = trades.Count,
            Wins = wins.Count,
            Losses = losses.Count,
            Timeouts = timeouts.Count,
            TotalPnl = trades.Sum(t => t.Pnl),
            AvgPnl = trades.Count > 0 ? trades.Average(t => t.Pnl) : 0,
            WinRate = trades.Count > 0 ? (decimal)wins.Count / trades.Count * 100 : 0,
            AvgWin = wins.Count > 0 ? wins.Average(t => t.Pnl) : 0,
            AvgLoss = losses.Count > 0 ? losses.Average(t => t.Pnl) : 0,
            LargestWin = wins.Count > 0 ? wins.Max(t => t.Pnl) : 0,
            LargestLoss = losses.Count > 0 ? losses.Min(t => t.Pnl) : 0
        };
    }

    private HeatmapData GenerateHourHeatmap(List<TradeResult> trades)
    {
        var cells = new List<HeatmapCell>();

        var byHour = trades.GroupBy(t => t.EntryTime.Hour).OrderBy(g => g.Key);

        foreach (var group in byHour)
        {
            var hour = group.Key;
            var tradeList = group.ToList();
            var avgPnl = tradeList.Average(t => t.Pnl);
            var winRate = tradeList.Count(t => t.Result == "win") / (decimal)tradeList.Count * 100;

            cells.Add(new HeatmapCell
            {
                Label = $"{hour:D2}:00",
                Value = avgPnl,
                Count = tradeList.Count,
                Color = avgPnl > 5 ? "green" : avgPnl < -5 ? "red" : "yellow",
                Tooltip = $"{tradeList.Count} trades, {winRate:F1}% win rate, {avgPnl:F2} avg P&L"
            });
        }

        return new HeatmapData
        {
            Dimension = "hour",
            Label = "Performance by Hour of Day",
            Cells = cells
        };
    }

    private HeatmapData GenerateDayHeatmap(List<TradeResult> trades)
    {
        var cells = new List<HeatmapCell>();
        var daysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                 DayOfWeek.Thursday, DayOfWeek.Friday };

        foreach (var day in daysOfWeek)
        {
            var tradeList = trades.Where(t => t.EntryTime.DayOfWeek == day).ToList();
            if (tradeList.Count == 0)
                continue;

            var avgPnl = tradeList.Average(t => t.Pnl);
            var winRate = tradeList.Count(t => t.Result == "win") / (decimal)tradeList.Count * 100;

            cells.Add(new HeatmapCell
            {
                Label = day.ToString(),
                Value = avgPnl,
                Count = tradeList.Count,
                Color = avgPnl > 5 ? "green" : avgPnl < -5 ? "red" : "yellow",
                Tooltip = $"{tradeList.Count} trades, {winRate:F1}% win rate, {avgPnl:F2} avg P&L"
            });
        }

        return new HeatmapData
        {
            Dimension = "day",
            Label = "Performance by Day of Week",
            Cells = cells
        };
    }

    private async Task<HeatmapData> GenerateConditionHeatmapAsync(List<TradeResult> trades)
    {
        await Task.CompletedTask;

        var cells = new List<HeatmapCell>();
        var conditions = new[] { "trending", "ranging", "volatile", "quiet" };

        // Would need trade analyses loaded to get actual conditions
        // For now, return empty heatmap

        return new HeatmapData
        {
            Dimension = "condition",
            Label = "Performance by Market Condition",
            Cells = cells
        };
    }

    // Technical indicator calculations

    private decimal CalculateADX(List<Bar> bars)
    {
        if (bars.Count < 14)
            return 0;

        // Simplified ADX calculation
        // Real ADX is complex; this is an approximation
        var recentBars = bars.TakeLast(14).ToList();
        var avgRange = recentBars.Average(b => b.High - b.Low);
        var priceChange = Math.Abs(recentBars.Last().Close - recentBars.First().Close);

        var adx = (priceChange / avgRange) * 25; // Normalized to 0-100 scale
        return Math.Min(100, adx);
    }

    private decimal CalculateATR(List<Bar> bars)
    {
        if (bars.Count < 14)
            return 0;

        // Calculate True Range for each bar
        var trueRanges = new List<decimal>();
        for (int i = 1; i < bars.Count; i++)
        {
            var high = bars[i].High;
            var low = bars[i].Low;
            var prevClose = bars[i - 1].Close;

            var tr = Math.Max(
                high - low,
                Math.Max(
                    Math.Abs(high - prevClose),
                    Math.Abs(low - prevClose)
                )
            );

            trueRanges.Add(tr);
        }

        // ATR is typically a 14-period average
        return trueRanges.TakeLast(14).Average();
    }
}
