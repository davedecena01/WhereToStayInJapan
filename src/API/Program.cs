using System.Text.Json;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WhereToStayInJapan.API.Middleware;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Application.Services;
// ReSharper disable once RedundantUsingDirective (kept for explicitness at registration site)
using WhereToStayInJapan.Application.Services.Interfaces;
using WhereToStayInJapan.Application.Validation;
using WhereToStayInJapan.Domain.Services;
using WhereToStayInJapan.Infrastructure.Adapters.AI;
using WhereToStayInJapan.Infrastructure.Adapters.Hotels;
using WhereToStayInJapan.Infrastructure.Adapters.Maps;
using WhereToStayInJapan.Infrastructure.Cache;
using WhereToStayInJapan.Infrastructure.Parsing;
using WhereToStayInJapan.Infrastructure.Persistence;
using WhereToStayInJapan.Infrastructure.Persistence.Repositories;
using WhereToStayInJapan.Infrastructure.Seed;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console()
        .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));

    // Database
    builder.Services.AddDbContext<ApplicationDbContext>(opts =>
        opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Cache
    builder.Services.AddScoped<ICacheService, PostgresCacheService>();

    // Repositories
    builder.Services.AddScoped<IStationAreaRepository, StationAreaRepository>();
    builder.Services.AddScoped<IFoodRepository, FoodRepository>();
    builder.Services.AddScoped<IAttractionRepository, AttractionRepository>();

    // AI provider — selected by config
    var aiMode = builder.Configuration["AI:Mode"] ?? "mock";
    builder.Services.AddScoped<IAIProvider>(sp =>
    {
        IAIProvider inner = aiMode.ToLowerInvariant() switch
        {
            "production" => new GeminiAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("gemini"),
                builder.Configuration["AI:GeminiApiKey"] ?? string.Empty,
                builder.Configuration["AI:GeminiModel"] ?? "gemini-1.5-flash"),
            "rules_only" => new RulesOnlyAdapter(),
            _ => new MockAIAdapter()
        };
        return new CachedAIProvider(inner, sp.GetRequiredService<ICacheService>());
    });

    // Hotel provider — selected by config
    var hotelProvider = builder.Configuration["Hotels:Provider"] ?? "mock";
    builder.Services.AddScoped<IHotelProvider>(sp =>
    {
        IHotelProvider inner = hotelProvider.ToLowerInvariant() switch
        {
            "rakuten" => new RakutenHotelAdapter(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("rakuten"),
                builder.Configuration,
                sp.GetRequiredService<ILogger<RakutenHotelAdapter>>()),
            _ => new MockHotelAdapter()
        };
        return new CachedHotelProvider(inner, sp.GetRequiredService<ICacheService>());
    });

    // Maps providers — config-driven (nominatim/osrm in prod, mock/seeded in dev)
    var geocodeProvider = builder.Configuration["Maps:GeocodeProvider"] ?? "mock";
    builder.Services.AddScoped<IGeocodeProvider>(sp =>
        new CachedGeocodeProvider(
            geocodeProvider == "nominatim"
                ? new NominatimAdapter(sp.GetRequiredService<IHttpClientFactory>().CreateClient("nominatim"))
                : new MockGeocodeAdapter(),
            sp.GetRequiredService<ICacheService>()));

    var routingProvider = builder.Configuration["Maps:RoutingProvider"] ?? "seeded";
    builder.Services.AddScoped<IRoutingProvider>(sp =>
        new CachedRoutingProvider(
            routingProvider == "osrm"
                ? new OsrmAdapter(sp.GetRequiredService<IHttpClientFactory>().CreateClient("osrm"))
                : new SeededFallbackRoutingProvider(sp.GetRequiredService<ICacheService>()),
            sp.GetRequiredService<ICacheService>()));

    // Domain services
    builder.Services.AddSingleton<RegionGroupingService>();
    builder.Services.AddScoped<ItineraryNormalizer>();
    builder.Services.AddScoped<IScoringService, ScoringService>();

    // File extractors
    builder.Services.AddScoped<IItineraryExtractor, PlainTextExtractor>();
    builder.Services.AddScoped<IItineraryExtractor, PdfExtractor>();
    builder.Services.AddScoped<IItineraryExtractor, DocxExtractor>();

    // Application services
    builder.Services.AddScoped<IItineraryParsingService, ItineraryParsingService>();
    builder.Services.AddScoped<IItineraryGenerationService, ItineraryGenerationService>();
    builder.Services.AddScoped<IRecommendationService, RecommendationService>();
    builder.Services.AddScoped<IHotelSearchService, HotelSearchService>();
    builder.Services.AddScoped<IChatService, ChatService>();

    // Validators
    builder.Services.AddValidatorsFromAssemblyContaining<ParseItineraryRequestValidator>();
    builder.Services.AddFluentValidationAutoValidation();

    // Rate limiting — per-IP, fixed window, per-route policies
    builder.Services.AddRateLimiter(opts =>
    {
        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        opts.AddPolicy("parse", ctx => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimit:ParseRequestsPerMinute", 10),
                Window = TimeSpan.FromMinutes(1)
            }));

        opts.AddPolicy("recommend", ctx => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimit:RecommendationRequestsPerMinute", 20),
                Window = TimeSpan.FromMinutes(1)
            }));

        opts.AddPolicy("chat", ctx => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimit:ChatRequestsPerMinute", 15),
                Window = TimeSpan.FromMinutes(1)
            }));

        opts.AddPolicy("analytics", ctx => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimit:AnalyticsRequestsPerMinute", 60),
                Window = TimeSpan.FromMinutes(1)
            }));
    });

    // Background services
    builder.Services.AddHostedService<DataSeeder>();
    builder.Services.AddHostedService<CacheCleanupService>();

    // HTTP clients
    builder.Services.AddHttpClient("gemini", c =>
    {
        c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        c.Timeout = TimeSpan.FromSeconds(60);
    });
    // Rakuten requests are routed through a Vercel proxy to satisfy Rakuten's IP-based access
    // control (Railway IPs are blocked; Vercel IPs match the registered application domain).
    builder.Services.AddHttpClient("rakuten", c =>
    {
        c.BaseAddress = new Uri("https://where-to-stay-in-japan.vercel.app/");
        var proxySecret = builder.Configuration["Hotels:ProxySecret"];
        if (!string.IsNullOrEmpty(proxySecret))
            c.DefaultRequestHeaders.Add("x-proxy-secret", proxySecret);
    });
    builder.Services.AddHttpClient("nominatim", c =>
    {
        c.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
        c.Timeout = TimeSpan.FromSeconds(5);
        c.DefaultRequestHeaders.Add("User-Agent",
            builder.Configuration["Maps:NominatimUserAgent"] ?? "WhereToStayInJapan/1.0");
    });
    builder.Services.AddHttpClient("osrm", c =>
    {
        c.BaseAddress = new Uri("https://router.project-osrm.org/");
        c.Timeout = TimeSpan.FromSeconds(5);
    });

    // Controllers + CORS
    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });
    builder.Services.AddOpenApi();

    // Only include stable, known origins — no rotating preview/deployment URLs
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:4200"];
    builder.Services.AddCors(opts => opts.AddDefaultPolicy(p =>
        p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

    // Startup assertions: fail fast if required production secrets are absent
    if (builder.Environment.IsProduction())
    {
        if (aiMode == "production" && string.IsNullOrWhiteSpace(builder.Configuration["AI:GeminiApiKey"]))
            throw new InvalidOperationException("AI:GeminiApiKey is required when AI:Mode is 'production'.");

        if (hotelProvider == "rakuten" && string.IsNullOrWhiteSpace(builder.Configuration["Hotels:ProxySecret"]))
            throw new InvalidOperationException("Hotels:ProxySecret is required when Hotels:Provider is 'rakuten'.");

        if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required in production.");
    }

    var app = builder.Build();

    // Auto-run EF Core migrations at startup — Production only (Railway direct connection)
    // Skipped in Development: local Supabase pooler connection triggers Npgsql 10 write bug
    if (app.Environment.IsProduction())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    if (app.Environment.IsProduction())
        app.UseHttpsRedirection();

    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed.");
}
finally
{
    Log.CloseAndFlush();
}
