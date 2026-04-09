# Observability — Where To Stay In Japan

---

## Logging (Backend)

**Library:** Serilog (`Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`)

**Configuration in `Program.cs`:**
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(env.IsProduction() ? LogEventLevel.Information : LogEventLevel.Debug)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();
```

**Log sinks:**
- **Console:** Always on. Railway aggregates console logs automatically.
- **File:** Rolling daily files in `logs/`. Retained for 7 days. Useful for debugging Railway deployments.
- **Optional:** `Serilog.Sinks.Seq` (Seq Cloud free tier: 1GB/day) or `Serilog.Sinks.Logtail` (Logtail free: 1GB/day, 3-day retention)

---

## What to Log

**Always log (at INFO level):**
```csharp
// Recommendation request
Log.Information("Recommendation requested. SessionId={SessionId}, Regions={Regions}, DestinationCount={Count}",
    request.SessionId, itinerary.RegionsDetected, itinerary.Destinations.Count);

// Recommendation completed
Log.Information("Recommendations generated. SessionId={SessionId}, TopAreas={Areas}, DurationMs={Ms}, AiUsed={AiUsed}, HotelsFetched={Hotels}",
    sessionId, topAreas, stopwatch.ElapsedMilliseconds, aiUsed, hotelsAvailable);

// Cache operations
Log.Debug("Cache hit: {Table} key={Key}", tableName, key);
Log.Debug("Cache miss: {Table} key={Key} — calling provider", tableName, key);

// Provider calls
Log.Information("AI call: {PromptType} provider={Provider} tokens={Tokens} durationMs={Ms}",
    promptType, provider, tokenCount, ms);
Log.Information("Hotel search: provider={Provider} areaId={AreaId} resultCount={Count} durationMs={Ms}",
    provider, areaId, count, ms);

// Provider failures
Log.Warning("Provider unavailable: {Provider} error={Error} retryable={Retryable}",
    providerName, ex.Message, isRetryable);
```

**Always log (at WARNING level):**
```csharp
// Geocoding failures
Log.Warning("Geocoding failed for destination: {Destination} city={City}", destName, city);

// AI parse failures
Log.Warning("AI response schema validation failed. PromptType={Type} attempt={Attempt}", type, attempt);

// Recommendation performance
if (stopwatch.ElapsedMilliseconds > 5000)
    Log.Warning("Slow recommendation: {DurationMs}ms exceeded 5s threshold. SessionId={SessionId}", ms, sessionId);
```

**Never log:**
- Raw itinerary text (may contain names, PII)
- API keys or secrets
- Full hotel prices or user budget preferences (low risk, but unnecessary)
- Routing cache hit/miss at INFO level (too noisy; use DEBUG)

---

## Error Tracking (Sentry)

**Package:** `Sentry.AspNetCore`

```csharp
// Program.cs
builder.WebHost.UseSentry(o => {
    o.Dsn = builder.Configuration["Sentry:Dsn"];
    o.TracesSampleRate = 0.1;  // 10% of transactions for performance monitoring
    o.MinimumBreadcrumbLevel = LogEventLevel.Information;
    o.MinimumEventLevel = LogEventLevel.Error;
});
```

Sentry free tier: 5,000 errors/month — more than sufficient for a portfolio project.

**Frontend Sentry:**
```typescript
// app.config.ts
import * as Sentry from "@sentry/angular";

Sentry.init({
  dsn: environment.sentryDsn,
  integrations: [Sentry.browserTracingIntegration(), Sentry.replayIntegration()],
  tracesSampleRate: 0.1,
  replaysSessionSampleRate: 0.0,   // No session replay (privacy)
  replaysOnErrorSampleRate: 0.1,
});
```

**When Sentry is not configured** (empty DSN): Sentry SDK is a no-op. Safe to deploy without a Sentry account.

---

## Analytics (Minimal)

No third-party analytics (no Google Analytics, no Mixpanel) in V1. Everything is first-party.

### `recommendation_logs` table (backend)

One row per `POST /api/recommendations` request. Written fire-and-forget after the response is sent.

```csharp
// In RecommendationService, after returning results:
_ = Task.Run(async () => {
    await _logRepo.CreateAsync(new RecommendationLog {
        SessionId    = sessionId,
        InputHash    = ComputeHash(rawInput),
        TopAreas     = results.Select(r => r.AreaName).ToArray(),
        RegionCount  = itinerary.RegionsDetected.Length,
        AiUsed       = results.Any(r => r.AiUsed),
        HotelsFetched = results.Any(r => r.HotelsAvailable),
        DurationMs   = (int)stopwatch.ElapsedMilliseconds,
        CreatedAt    = DateTime.UtcNow
    });
}, CancellationToken.None);
```

**Never await this.** Never block the recommendation response on analytics.

### `hotel_click_logs` table (backend)

```typescript
// Frontend — fire-and-forget
onHotelClick(hotel: HotelItem, areaId: string): void {
  window.open(hotel.deep_link_url, '_blank', 'noopener,noreferrer');
  this.apiService.trackHotelClick({
    session_id: this.sessionService.loadSession()?.session_id ?? 'unknown',
    hotel_id: hotel.provider_id,
    area_id: areaId,
    area_name: hotel.area_name
  });
  // No await — fire and forget
}
```

---

## Health Endpoint

`GET /api/health` — used by Railway health check probes and for manual verification.

```csharp
[HttpGet("health")]
public async Task<IActionResult> Health(CancellationToken ct)
{
    var dbHealthy = await CheckDatabase(ct);
    var aiMode = _config["AI:Mode"];
    var hotelsProvider = _config["Hotels:Provider"];

    var status = dbHealthy ? "healthy" : "degraded";
    var statusCode = dbHealthy ? 200 : 503;

    return StatusCode(statusCode, new {
        status,
        providers = new {
            database = dbHealthy,
            ai = aiMode != "mock",
            hotels = hotelsProvider != "mock",
            maps = _config["Maps:GeocodeProvider"] != "mock"
        },
        mode = new {
            ai = aiMode ?? "production",
            hotels = hotelsProvider ?? "rakuten",
            maps = _config["Maps:GeocodeProvider"] ?? "nominatim"
        }
    });
}
```

Railway health check: set path to `/api/health`, return 200 = healthy.

---

## Performance Monitoring

Log timing at each pipeline stage using `Stopwatch`:

```csharp
// RecommendationService
var sw = Stopwatch.StartNew();

// Stage 1: Normalization
await NormalizeItinerary(itinerary, ct);
Log.Debug("Stage: normalize — {Ms}ms", sw.ElapsedMilliseconds);
sw.Restart();

// Stage 2: Candidate selection
var candidates = await GetCandidateAreas(itinerary.RegionsDetected, ct);
Log.Debug("Stage: candidate_selection — {Ms}ms ({Count} candidates)", sw.ElapsedMilliseconds, candidates.Count);
sw.Restart();

// Stage 3: Travel time matrix
var matrix = await BuildTravelTimeMatrix(candidates, itinerary.Destinations, ct);
Log.Debug("Stage: travel_time_matrix — {Ms}ms", sw.ElapsedMilliseconds);
sw.Restart();

// Stage 4: Scoring
var scored = _scoringService.ScoreCandidates(candidates, matrix, preferences);
Log.Debug("Stage: scoring — {Ms}ms", sw.ElapsedMilliseconds);
```

Emit a warning if total exceeds 5 seconds. This catches regressions in routing or AI performance.

---

## Alerting (V1: None)

No automated alerting in V1. This is a portfolio project.

**If a production alerting setup were needed (for reference):**
- Railway / Render: uptime alerts via their dashboard (email on instance down)
- Sentry: email on first occurrence of new error type
- cron-job.org: alert if health check ping fails

---

## Recommended Dashboarding (Future)

If the project grows beyond portfolio use, consider:
- **Seq** (self-hosted or Seq Cloud) for structured log querying
- **Grafana + Loki** (open source, free) for log aggregation
- **Supabase table viewer** for quick analytics queries on `recommendation_logs` and `hotel_click_logs`

The structured logging format (Serilog + properties) is already compatible with all of these.
