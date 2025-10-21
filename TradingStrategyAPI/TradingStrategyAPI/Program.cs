using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradingStrategyAPI.Database;
using TradingStrategyAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Circular references are now prevented by [JsonIgnore] on navigation properties
    });

// Configure PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString!));

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://21.0.0.50:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Trading Strategy API",
        Version = "v1",
        Description = "API for trading strategy analysis and backtesting"
    });

    // Configure JWT authentication in Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register application services
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddScoped<IErrorTracker, ErrorTracker>();

// Register HttpClient for AI services
builder.Services.AddHttpClient();

// Register AI service based on configuration
builder.Services.AddSingleton<IAIService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var provider = config["AI:Provider"]?.ToLower() ?? "gemini";
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var redis = sp.GetRequiredService<IConnectionMultiplexer>();

    return provider switch
    {
        "claude" => new ClaudeService(
            httpClientFactory,
            config,
            sp.GetRequiredService<ILogger<ClaudeService>>(),
            redis),
        "gemini" => new GeminiService(
            httpClientFactory,
            config,
            sp.GetRequiredService<ILogger<GeminiService>>(),
            redis),
        _ => new GeminiService(
            httpClientFactory,
            config,
            sp.GetRequiredService<ILogger<GeminiService>>(),
            redis) // Default to Gemini (free tier)
    };
});

// Register strategy evaluator, scanner, and results analyzer
builder.Services.AddScoped<IStrategyEvaluator, StrategyEvaluator>();
builder.Services.AddScoped<IStrategyScanner, StrategyScanner>();
builder.Services.AddScoped<IResultsAnalyzer, ResultsAnalyzer>();
builder.Services.AddScoped<ITradeAnalyzer, TradeAnalyzer>();

// Register strategy manager for CRUD operations
builder.Services.AddScoped<IStrategyManager, StrategyManager>();

// Register indicator service
builder.Services.AddScoped<IIndicatorService, IndicatorService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Trading Strategy API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
