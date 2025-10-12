using StackExchange.Redis;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TradingStrategyAPI.Models;

namespace TradingStrategyAPI.Services;

/// <summary>
/// AI service implementation using Google's Gemini API.
/// Provides fast, cost-effective strategy parsing and insights generation.
/// </summary>
public partial class GeminiService : IAIService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;
    private readonly IConnectionMultiplexer? _redis;
    private readonly string _apiKey;
    private readonly string _model;

    private const int MaxRetries = 3;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(30);

    public string ProviderName => "Gemini";

    public GeminiService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _redis = redis;
        _apiKey = configuration["AI:Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API key not configured");
        _model = configuration["AI:Gemini:Model"] ?? "gemini-1.5-flash";

        _logger.LogInformation("GeminiService initialized with model {Model}", _model);
    }

    public async Task<Strategy> ParseStrategyAsync(string description)
    {
        var sw = Stopwatch.StartNew();
        var cacheKey = $"parsed:{ComputeHash(description)}";

        try
        {
            // Try cache first
            if (_redis is not null)
            {
                var cached = await _redis.GetDatabase().StringGetAsync(cacheKey);
                if (cached.HasValue)
                {
                    sw.Stop();
                    _logger.LogDebug("Cache hit for strategy parsing. Retrieved in {ElapsedMs}ms", sw.ElapsedMilliseconds);
                    return JsonSerializer.Deserialize<Strategy>(cached!)!;
                }
            }

            // Build prompt
            var prompt = BuildParseStrategyPrompt(description);
            _logger.LogInformation("Parsing strategy with Gemini. Prompt: {PromptPreview}...",
                prompt.Length > 100 ? prompt[..100] : prompt);

            // Call Gemini API with retry
            var response = await CallGeminiWithRetryAsync(prompt);

            // Extract JSON from response
            var jsonText = ExtractJson(response);
            _logger.LogDebug("Received response from Gemini: {ResponsePreview}...",
                jsonText.Length > 100 ? jsonText[..100] : jsonText);

            // Parse strategy
            var strategy = ParseStrategyJson(jsonText);

            sw.Stop();
            _logger.LogInformation("Successfully parsed strategy with Gemini in {ElapsedMs}ms", sw.ElapsedMilliseconds);

            // Cache result
            if (_redis is not null)
            {
                var serialized = JsonSerializer.Serialize(strategy);
                await _redis.GetDatabase().StringSetAsync(cacheKey, serialized, CacheTtl);
            }

            return strategy;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error parsing strategy with Gemini after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<string> GenerateInsightsAsync(StrategyResult result)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Build prompt
            var prompt = BuildInsightsPrompt(result);
            _logger.LogInformation("Generating insights with Gemini for strategy result {ResultId}", result.Id);

            // Call Gemini API with retry
            var insights = await CallGeminiWithRetryAsync(prompt);

            sw.Stop();
            _logger.LogInformation("Generated insights with Gemini in {ElapsedMs}ms. Insights: {InsightsPreview}...",
                sw.ElapsedMilliseconds, insights.Length > 100 ? insights[..100] : insights);

            return insights.Trim();
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error generating insights with Gemini after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<string> CallGeminiWithRetryAsync(string prompt)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < MaxRetries)
        {
            attempt++;
            var sw = Stopwatch.StartNew();

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 1024
                    }
                };

                var requestJson = JsonSerializer.Serialize(request);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(endpoint, content);
                sw.Stop();

                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini API error (attempt {Attempt}/{MaxRetries}): {StatusCode} - {Response}",
                        attempt, MaxRetries, response.StatusCode, responseBody);

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < MaxRetries)
                    {
                        var delayMs = (int)Math.Pow(2, attempt) * 1000; // Exponential backoff
                        _logger.LogInformation("Rate limited. Retrying after {DelayMs}ms...", delayMs);
                        await Task.Delay(delayMs);
                        continue;
                    }

                    throw new InvalidOperationException($"Gemini API error: {response.StatusCode} - {responseBody}");
                }

                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);
                var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                    ?? throw new InvalidOperationException("Empty response from Gemini");

                _logger.LogDebug("Gemini API call succeeded in {ElapsedMs}ms (attempt {Attempt})",
                    sw.ElapsedMilliseconds, attempt);

                return text;
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;
                _logger.LogWarning("Gemini API timeout (attempt {Attempt}/{MaxRetries})", attempt, MaxRetries);

                if (attempt < MaxRetries)
                {
                    await Task.Delay(1000 * attempt); // Linear backoff for timeouts
                    continue;
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Gemini API network error (attempt {Attempt}/{MaxRetries})", attempt, MaxRetries);

                if (attempt < MaxRetries)
                {
                    await Task.Delay(1000 * attempt);
                    continue;
                }
            }
        }

        throw new InvalidOperationException($"Failed to call Gemini API after {MaxRetries} attempts", lastException);
    }

    private static string BuildParseStrategyPrompt(string description)
    {
        return $@"Parse this trading strategy into JSON with the following structure:
{{
  'entry_conditions': [
    {{'indicator': 'price', 'operator': '>', 'value': 'vwap'}},
    {{'indicator': 'volume', 'operator': '>', 'value': '1.5x_average'}},
    ...
  ],
  'stop_loss': {{'type': 'points', 'value': 10}},
  'take_profit': {{'type': 'points', 'value': 20}},
  'direction': 'long'
}}

User strategy: {description}

Rules:
- Only return valid JSON (no markdown, no explanation)
- Infer missing details with reasonable defaults
- If ambiguous, assume most common interpretation
- Set direction based on keywords (long/buy vs short/sell)
- Default stop loss: 10 points, take profit: 20 points if not specified

IMPORTANT - Value Format Rules:
- For multiplier expressions, use format 'Xx_indicator' (NOT 'X * indicator')
  Examples: '1.5x_average', '0.8x_vwap', '2.0x_avgVolume20'
- Valid indicators: price, volume, vwap, ema9, ema20, ema50, avgVolume20
- For average volume comparisons, use 'avgVolume20' or '1.5x_average' format
- Never use spaces or multiplication operators (*) in value expressions
- Use underscores to connect multipliers: '1.5x_average' NOT '1.5 * average'";
    }

    private static string BuildInsightsPrompt(StrategyResult result)
    {
        // Calculate hour distribution
        var hourDistribution = result.AllTrades
            .Where(t => t.Result == "loss")
            .GroupBy(t => t.EntryTime.Hour)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => $"{g.Key}:00 ({g.Count()} losses)")
            .ToList();

        var worstTrades = result.AllTrades
            .Where(t => t.Result == "loss")
            .OrderBy(t => t.Pnl)
            .Take(5)
            .Sum(t => t.Pnl);

        return $@"Analyze these trading results and identify the main problems:

Total trades: {result.TotalTrades}
Win rate: {result.WinRate:P1}
Total P&L: ${result.TotalPnl:F2}
Average win: ${result.AvgWin:F2}
Average loss: ${result.AvgLoss:F2}

Loss distribution by hour: {string.Join(", ", hourDistribution)}

Worst 5 trades lost ${Math.Abs(worstTrades):F2}

In 2-3 sentences, explain the primary weakness of this strategy.
Be specific and actionable. Focus on when/why losses occur.";
    }

    [GeneratedRegex(@"```(?:json)?\s*(\{[\s\S]*?\})\s*```|(\{[\s\S]*?\})", RegexOptions.Multiline)]
    private static partial Regex JsonExtractorRegex();

    private string ExtractJson(string response)
    {
        var match = JsonExtractorRegex().Match(response);
        if (match.Success)
        {
            return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        }

        // If no JSON found, try to return the whole response
        var trimmed = response.Trim();
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            return trimmed;
        }

        _logger.LogError("Failed to extract JSON from Gemini response: {Response}", response);
        throw new InvalidOperationException("Could not extract JSON from Gemini response");
    }

    private Strategy ParseStrategyJson(string jsonText)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var parsed = JsonSerializer.Deserialize<ParsedStrategyDto>(jsonText, options)
                ?? throw new InvalidOperationException("Deserialized strategy is null");

            // Convert to Strategy model
            var strategy = new Strategy
            {
                Name = "AI Generated Strategy",
                Description = "Parsed from natural language",
                Direction = parsed.Direction ?? "long",
                EntryConditions = parsed.EntryConditions?.Select(c => new Models.Condition
                {
                    Indicator = c.Indicator ?? "price",
                    Operator = c.Operator ?? ">",
                    Value = c.Value ?? "0"
                }).ToList() ?? new List<Models.Condition>(),
                StopLoss = new StopLoss
                {
                    Type = parsed.StopLoss?.Type ?? "points",
                    Value = parsed.StopLoss?.Value ?? 10m
                },
                TakeProfit = new TakeProfit
                {
                    Type = parsed.TakeProfit?.Type ?? "points",
                    Value = parsed.TakeProfit?.Value ?? 20m
                }
            };

            return strategy;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse strategy JSON: {Json}", jsonText);
            throw new InvalidOperationException("Invalid strategy JSON format", ex);
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    // DTOs for Gemini API
    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class ParsedStrategyDto
    {
        [JsonPropertyName("entry_conditions")]
        public List<ConditionDto>? EntryConditions { get; set; }

        [JsonPropertyName("stop_loss")]
        public StopLossTakeProfitDto? StopLoss { get; set; }

        [JsonPropertyName("take_profit")]
        public StopLossTakeProfitDto? TakeProfit { get; set; }

        [JsonPropertyName("direction")]
        public string? Direction { get; set; }
    }

    private class ConditionDto
    {
        [JsonPropertyName("indicator")]
        public string? Indicator { get; set; }

        [JsonPropertyName("operator")]
        public string? Operator { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    private class StopLossTakeProfitDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("value")]
        public decimal Value { get; set; }
    }
}
