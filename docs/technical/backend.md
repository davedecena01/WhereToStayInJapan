# Backend — Where To Stay In Japan

Framework: .NET 8 Web API
Pattern: Clean layered architecture (Controller → Service → Repository/Adapter → Domain)
ORM: EF Core 8 with Npgsql
DI: Built-in `Microsoft.Extensions.DependencyInjection`

---

## Solution Structure

```
WhereToStayInJapan.sln
├── src/
│   ├── WhereToStayInJapan.API/
│   │   ├── Controllers/
│   │   │   ├── ItineraryController.cs
│   │   │   ├── RecommendationController.cs
│   │   │   ├── HotelController.cs
│   │   │   ├── AreaController.cs
│   │   │   ├── ChatController.cs
│   │   │   ├── AnalyticsController.cs
│   │   │   └── HealthController.cs
│   │   ├── Middleware/
│   │   │   ├── GlobalExceptionMiddleware.cs
│   │   │   └── RateLimitMiddleware.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── WhereToStayInJapan.Application/
│   │   ├── Services/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IItineraryParsingService.cs
│   │   │   │   ├── IRecommendationService.cs
│   │   │   │   ├── IHotelSearchService.cs
│   │   │   │   └── IChatService.cs
│   │   │   ├── ItineraryParsingService.cs
│   │   │   ├── RecommendationService.cs
│   │   │   ├── HotelSearchService.cs
│   │   │   └── ChatService.cs
│   │   ├── DTOs/
│   │   │   ├── ParsedItineraryDto.cs
│   │   │   ├── UserPreferencesDto.cs
│   │   │   ├── RecommendationResultDto.cs
│   │   │   ├── HotelItemDto.cs
│   │   │   ├── FoodItemDto.cs
│   │   │   └── AttractionItemDto.cs
│   │   └── Validation/
│   │       ├── UserPreferencesValidator.cs
│   │       └── ParsedItineraryValidator.cs
│   │
│   ├── WhereToStayInJapan.Domain/
│   │   ├── Entities/
│   │   │   ├── StationArea.cs
│   │   │   ├── CuratedFood.cs
│   │   │   ├── CuratedAttraction.cs
│   │   │   ├── GeocodeCache.cs
│   │   │   ├── RoutingCache.cs
│   │   │   ├── AiResponseCache.cs
│   │   │   ├── HotelSearchCache.cs
│   │   │   ├── RecommendationLog.cs
│   │   │   └── HotelClickLog.cs
│   │   ├── Models/
│   │   │   ├── ParsedItinerary.cs
│   │   │   ├── Destination.cs
│   │   │   ├── UserPreferences.cs
│   │   │   ├── TravelTimeMatrix.cs
│   │   │   ├── ScoredCandidate.cs
│   │   │   └── ScoreBreakdown.cs
│   │   └── Services/
│   │       ├── IScoringService.cs
│   │       ├── ScoringService.cs
│   │       ├── ItineraryNormalizer.cs
│   │       └── RegionGroupingService.cs
│   │
│   ├── WhereToStayInJapan.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Migrations/
│   │   │   └── Repositories/
│   │   │       ├── IStationAreaRepository.cs
│   │   │       ├── StationAreaRepository.cs
│   │   │       ├── IFoodRepository.cs
│   │   │       ├── FoodRepository.cs
│   │   │       ├── IAttractionRepository.cs
│   │   │       └── AttractionRepository.cs
│   │   ├── Adapters/
│   │   │   ├── AI/
│   │   │   │   ├── IAIProvider.cs
│   │   │   │   ├── GeminiAdapter.cs
│   │   │   │   ├── MockAIAdapter.cs
│   │   │   │   ├── RulesOnlyAdapter.cs
│   │   │   │   └── CachedAIProvider.cs
│   │   │   ├── Maps/
│   │   │   │   ├── IGeocodeProvider.cs
│   │   │   │   ├── IRoutingProvider.cs
│   │   │   │   ├── NominatimAdapter.cs
│   │   │   │   ├── OsrmAdapter.cs
│   │   │   │   ├── MockGeocodeAdapter.cs
│   │   │   │   ├── SeededFallbackRoutingProvider.cs
│   │   │   │   ├── CachedGeocodeProvider.cs
│   │   │   │   └── CachedRoutingProvider.cs
│   │   │   └── Hotels/
│   │   │       ├── IHotelProvider.cs
│   │   │       ├── RakutenHotelAdapter.cs
│   │   │       ├── MockHotelAdapter.cs
│   │   │       └── CachedHotelProvider.cs
│   │   ├── Cache/
│   │   │   ├── ICacheService.cs
│   │   │   └── PostgresCacheService.cs
│   │   ├── Extractors/
│   │   │   ├── IItineraryExtractor.cs
│   │   │   ├── PdfExtractor.cs
│   │   │   ├── DocxExtractor.cs
│   │   │   └── PlainTextExtractor.cs
│   │   └── Seed/
│   │       ├── DataSeeder.cs
│   │       ├── station_areas.json
│   │       ├── curated_food.json
│   │       └── curated_attractions.json
│   │
│   └── WhereToStayInJapan.Shared/
│       ├── Extensions/
│       │   ├── StringExtensions.cs      -- NormalizeKey(), etc.
│       │   └── GeoExtensions.cs         -- Haversine distance
│       └── Constants/
│           └── RegionMappings.cs        -- city → region lookup
│
└── tests/
    ├── WhereToStayInJapan.Domain.Tests/
    │   ├── ScoringServiceTests.cs
    │   ├── ItineraryNormalizerTests.cs
    │   └── RegionGroupingServiceTests.cs
    ├── WhereToStayInJapan.Application.Tests/
    │   ├── RecommendationServiceTests.cs
    │   └── ItineraryParsingServiceTests.cs
    └── WhereToStayInJapan.API.Tests/
        ├── RecommendationControllerTests.cs
        └── ItineraryControllerTests.cs
```

---

## Key Service Interfaces and Signatures

### `IItineraryParsingService`
```csharp
public interface IItineraryParsingService
{
    Task<ParsedItinerary> ParseFromFileAsync(IFormFile file, CancellationToken ct = default);
    Task<ParsedItinerary> ParseFromTextAsync(string text, CancellationToken ct = default);
}
```

### `IRecommendationService`
```csharp
public interface IRecommendationService
{
    Task<IReadOnlyList<RecommendationResultDto>> GetRecommendationsAsync(
        ParsedItinerary itinerary,
        UserPreferences preferences,
        CancellationToken ct = default);
}
```

### `IScoringService` (Domain — pure function)
```csharp
public interface IScoringService
{
    // No async — no I/O. All inputs provided; pure deterministic computation.
    IReadOnlyList<ScoredCandidate> ScoreCandidates(
        IReadOnlyList<StationArea> candidates,
        TravelTimeMatrix travelTimes,
        UserPreferences preferences);
}
```

### `IHotelSearchService`
```csharp
public interface IHotelSearchService
{
    Task<HotelSearchResult> SearchAsync(
        StationArea area,
        HotelSearchParams searchParams,
        CancellationToken ct = default);
}

public record HotelSearchParams(
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Travelers,
    BudgetTier BudgetTier,
    int Page = 1,
    int PageSize = 10
);
```

### `ICacheService`
```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default);
}
```

---

## Controller Pattern

Controllers are thin. Each controller action does exactly:
1. Receive and bind the request DTO
2. Validate (FluentValidation via filter)
3. Call one application service method
4. Map result to response DTO
5. Return HTTP response

**Example — `RecommendationController.cs`:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class RecommendationController(IRecommendationService svc) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(IReadOnlyList<RecommendationResultDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> GetRecommendations(
        [FromBody] RecommendationRequestDto request,
        CancellationToken ct)
    {
        var results = await svc.GetRecommendationsAsync(
            request.Itinerary,
            request.Preferences,
            ct);

        var statusCode = results.Any(r => !r.HotelsAvailable) ? 206 : 200;
        return StatusCode(statusCode, results);
    }
}
```

---

## EF Core Setup

**`ApplicationDbContext.cs`:**
```csharp
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<StationArea> StationAreas => Set<StationArea>();
    public DbSet<CuratedFood> CuratedFood => Set<CuratedFood>();
    public DbSet<CuratedAttraction> CuratedAttractions => Set<CuratedAttraction>();
    public DbSet<GeocodeCache> GeocodeCaches => Set<GeocodeCache>();
    public DbSet<RoutingCache> RoutingCaches => Set<RoutingCache>();
    public DbSet<AiResponseCache> AiResponseCaches => Set<AiResponseCache>();
    public DbSet<HotelSearchCache> HotelSearchCaches => Set<HotelSearchCache>();
    public DbSet<RecommendationLog> RecommendationLogs => Set<RecommendationLog>();
    public DbSet<HotelClickLog> HotelClickLogs => Set<HotelClickLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

**Migrations command (run from `src/Infrastructure/`):**
```bash
dotnet ef migrations add InitialCreate --startup-project ../API/
dotnet ef database update --startup-project ../API/
```

---

## Configuration Schema (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=...;Username=...;Password=..."
  },
  "AI": {
    "Mode": "production",
    "Provider": "gemini",
    "ApiKey": "",
    "ModelId": "gemini-1.5-flash"
  },
  "Hotels": {
    "Provider": "rakuten",
    "ApiKey": "",
    "SearchRadiusKm": 2,
    "MinReviewRating": 3.5
  },
  "Maps": {
    "GeocodeProvider": "nominatim",
    "RoutingProvider": "osrm",
    "UserAgent": "WhereToStayInJapan/1.0 (contact@example.com)"
  },
  "Cache": {
    "GeocodeTtlDays": 90,
    "RoutingTtlDays": 7,
    "HotelTtlMinutes": 30,
    "AiResponseTtlHours": 24
  },
  "Seed": {
    "MinimumStationAreaCount": 10
  },
  "RateLimit": {
    "RecommendationsPerHour": 10,
    "ParsePerHour": 20,
    "ChatPerHour": 30
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  },
  "Sentry": {
    "Dsn": ""
  }
}
```

**Environment variable overrides** (Railway/Render format, double underscore = colon):
```
CONNECTIONSTRINGS__DEFAULTCONNECTION
AI__APIKEY
AI__MODE
HOTELS__APIKEY
CORS__ALLOWEDORIGINS__0
SENTRY__DSN
```

---

## DI Registration (`Program.cs`)

```csharp
// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IStationAreaRepository, StationAreaRepository>();
builder.Services.AddScoped<IFoodRepository, FoodRepository>();
builder.Services.AddScoped<IAttractionRepository, AttractionRepository>();

// Cache
builder.Services.AddScoped<ICacheService, PostgresCacheService>();

// AI Provider (resolved from config)
builder.Services.AddScoped<IAIProvider>(sp => {
    var cfg = sp.GetRequiredService<IConfiguration>();
    var cache = sp.GetRequiredService<ICacheService>();
    IAIProvider inner = cfg["AI:Mode"] switch {
        "mock"       => new MockAIAdapter(),
        "rules_only" => new RulesOnlyAdapter(),
        _            => new GeminiAdapter(cfg["AI:ApiKey"]!, cfg["AI:ModelId"]!)
    };
    return new CachedAIProvider(inner, cache);
});

// Maps
builder.Services.AddHttpClient<NominatimAdapter>();
builder.Services.AddHttpClient<OsrmAdapter>();
builder.Services.AddScoped<IGeocodeProvider>(sp => {
    var inner = cfg["Maps:GeocodeProvider"] == "mock"
        ? (IGeocodeProvider) new MockGeocodeAdapter()
        : sp.GetRequiredService<NominatimAdapter>();
    return new CachedGeocodeProvider(inner, sp.GetRequiredService<ICacheService>());
});
builder.Services.AddScoped<IRoutingProvider>(sp => {
    IRoutingProvider inner = cfg["Maps:RoutingProvider"] == "mock"
        ? new SeededFallbackRoutingProvider(sp.GetRequiredService<ICacheService>())
        : sp.GetRequiredService<OsrmAdapter>();
    return new CachedRoutingProvider(inner, sp.GetRequiredService<ICacheService>());
});

// Hotels
builder.Services.AddHttpClient<RakutenHotelAdapter>();
builder.Services.AddScoped<IHotelProvider>(sp => {
    IHotelProvider inner = cfg["Hotels:Provider"] == "mock"
        ? new MockHotelAdapter()
        : sp.GetRequiredService<RakutenHotelAdapter>();
    return new CachedHotelProvider(inner, sp.GetRequiredService<ICacheService>());
});

// Application services
builder.Services.AddScoped<IItineraryParsingService, ItineraryParsingService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IHotelSearchService, HotelSearchService>();
builder.Services.AddScoped<IChatService, ChatService>();

// Domain services
builder.Services.AddSingleton<IScoringService, ScoringService>();
builder.Services.AddSingleton<ItineraryNormalizer>();
builder.Services.AddSingleton<RegionGroupingService>();

// File extractors
builder.Services.AddTransient<PdfExtractor>();
builder.Services.AddTransient<DocxExtractor>();
builder.Services.AddTransient<PlainTextExtractor>();

// Hosted services
builder.Services.AddHostedService<DataSeeder>();
builder.Services.AddHostedService<CacheCleanupService>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<UserPreferencesValidator>();
builder.Services.AddFluentValidationAutoValidation();
```

---

## NuGet Packages

```xml
<!-- API project -->
<PackageReference Include="Serilog.AspNetCore" Version="8.*" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.*" />
<PackageReference Include="Serilog.Sinks.File" Version="5.*" />
<PackageReference Include="Sentry.AspNetCore" Version="4.*" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />

<!-- Infrastructure project -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.*" />
<PackageReference Include="UglyToad.PdfPig" Version="0.*" />
<PackageReference Include="DocumentFormat.OpenXml" Version="3.*" />
<PackageReference Include="Polly" Version="8.*" />
<PackageReference Include="Polly.Extensions.Http" Version="3.*" />

<!-- Test projects -->
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.*" />
```

---

## Testing Strategy

**Domain tests** (unit, no mocks needed — pure functions):
- `ScoringServiceTests`: verify score formula, min-max normalization, preference modifiers
- `ItineraryNormalizerTests`: deduplication, region inference, multi-region detection
- `RegionGroupingServiceTests`: haversine distance, grouping by region

**Application tests** (unit with mocks):
- `RecommendationServiceTests`: mock `IScoringService`, `IHotelProvider`, `IAIProvider`, `IStationAreaRepository`
- `ItineraryParsingServiceTests`: mock `IAIProvider`, `IGeocodeProvider`

**API tests** (integration with `WebApplicationFactory`):
- `RecommendationControllerTests`: full pipeline with in-memory DB, `MockAIAdapter`, `MockHotelAdapter`
- `ItineraryControllerTests`: test file upload handling, text parsing

**Rule:** Never mock `IScoringService` — it's a pure function, test it directly. Only mock I/O interfaces.
