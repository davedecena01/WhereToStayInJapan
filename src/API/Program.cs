using FluentValidation;
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
            "rakuten" => new RakutenHotelAdapter(sp.GetRequiredService<IHttpClientFactory>().CreateClient("rakuten")),
            _ => new MockHotelAdapter()
        };
        return new CachedHotelProvider(inner, sp.GetRequiredService<ICacheService>());
    });

    // Maps providers — mock for Phase 0
    builder.Services.AddScoped<IGeocodeProvider>(sp =>
        new CachedGeocodeProvider(
            new MockGeocodeAdapter(),
            sp.GetRequiredService<ICacheService>()));

    builder.Services.AddScoped<IRoutingProvider>(sp =>
        new CachedRoutingProvider(
            new SeededFallbackRoutingProvider(sp.GetRequiredService<ICacheService>()),
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
    builder.Services.AddScoped<IRecommendationService, RecommendationService>();
    builder.Services.AddScoped<IHotelSearchService, HotelSearchService>();
    builder.Services.AddScoped<IChatService, ChatService>();

    // Validators
    builder.Services.AddValidatorsFromAssemblyContaining<ParseItineraryRequestValidator>();

    // Background services
    builder.Services.AddHostedService<DataSeeder>();
    builder.Services.AddHostedService<CacheCleanupService>();

    // HTTP clients
    builder.Services.AddHttpClient("gemini", c =>
        c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/"));
    builder.Services.AddHttpClient("rakuten", c =>
        c.BaseAddress = new Uri("https://app.rakuten.co.jp/"));
    builder.Services.AddHttpClient("nominatim", c =>
    {
        c.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
        c.DefaultRequestHeaders.Add("User-Agent",
            builder.Configuration["Maps:NominatimUserAgent"] ?? "WhereToStayInJapan/1.0");
    });
    builder.Services.AddHttpClient("osrm", c =>
        c.BaseAddress = new Uri("https://router.project-osrm.org/"));

    // Controllers + CORS
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:4200"];
    builder.Services.AddCors(opts => opts.AddDefaultPolicy(p =>
        p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();

    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    app.UseCors();
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
