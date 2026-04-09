# System Architecture — Where To Stay In Japan

---

## High-Level Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                     Browser (Angular 17 SPA)                     │
│                                                                  │
│  ┌─────────────┐  ┌────────────────────┐  ┌──────────────────┐  │
│  │ItineraryFlow│  │RecommendationCards  │  │AreaDetailPage    │  │
│  │  - upload   │  │  - ranked list      │  │  - hotels        │  │
│  │  - review   │  │  - score breakdown  │  │  - food          │  │
│  │  - chat     │  │  - travel times     │  │  - attractions   │  │
│  └─────────────┘  └────────────────────┘  └──────────────────┘  │
│                                                                  │
│  SessionService (localStorage)  ←→  ItineraryStore / Rec.Store  │
└───────────────────────────┬──────────────────────────────────────┘
                            │ HTTPS (REST JSON)
                            ▼
┌──────────────────────────────────────────────────────────────────┐
│                   .NET 8 Web API (Railway.app)                   │
│                                                                  │
│  Controllers (thin HTTP boundary)                                │
│    ItineraryController  RecommendationController  HotelController│
│    ChatController       AnalyticsController        HealthController│
│                                                                  │
│  Application Services (orchestration)                            │
│    IItineraryParsingService   IRecommendationService             │
│    IHotelSearchService        IChatService                       │
│                                                                  │
│  Domain (pure logic)                                             │
│    IScoringService    RegionGroupingService    ItineraryNormalizer│
│                                                                  │
│  Infrastructure (external I/O, all behind interfaces)            │
│  ┌─────────────┬──────────────┬─────────────┬──────────────────┐│
│  │ IAIProvider │IGeocodeProvider│IRoutingProvider│IHotelProvider ││
│  │             │              │             │                  ││
│  │ Gemini      │ Nominatim    │ OSRM        │ Rakuten          ││
│  │ Adapter     │ Adapter      │ Adapter     │ Adapter          ││
│  │             │              │             │                  ││
│  │ Mock        │ Mock         │ Seeded      │ Mock             ││
│  │ Adapter     │ Adapter      │ Fallback    │ Adapter          ││
│  │             │              │             │                  ││
│  │ Rules-Only  │              │             │                  ││
│  │ Adapter     │              │             │                  ││
│  └──────┬──────┴──────┬───────┴──────┬──────┴──────────┬───────┘│
│         │ (cache decorators wrap each provider)         │        │
│  ┌──────▼──────────────▼──────────────▼─────────────────▼──────┐│
│  │               ICacheService (PostgreSQL-backed)              ││
│  └──────────────────────────────────────────────────────────────┘│
│                            │                                     │
│  IRepository<T> + EF Core  │                                     │
└───────────────────────────┬──────────────────────────────────────┘
                            │ PostgreSQL (Npgsql)
                            ▼
┌──────────────────────────────────────────────────────────────────┐
│                   PostgreSQL (Supabase free tier)                │
│                                                                  │
│  station_areas    curated_food     curated_attractions           │
│  geocode_cache    routing_cache    ai_response_cache             │
│  hotel_search_cache               recommendation_logs           │
│  hotel_click_logs                                               │
└──────────────────────────────────────────────────────────────────┘

External APIs (called only through adapters, never directly from services):
  - api.generativelanguage.googleapis.com  (Gemini Flash 1.5)
  - nominatim.openstreetmap.org            (geocoding)
  - router.project-osrm.org               (routing)
  - app.rakuten.co.jp/services/api/Travel  (hotel search)
```

---

## Request Lifecycle: Recommendation Flow

```
1. POST /api/recommendations
   Body: { itinerary: ParsedItinerary, preferences: UserPreferences }

2. RecommendationController → IRecommendationService.GetRecommendationsAsync()

3. IRecommendationService:
   a. NormalizeItinerary() — deterministic, no I/O
      - Merge duplicates, normalize names, group by region
      - Detect multi-region (haversine distance check)

   b. GetCandidateAreas() — IStationAreaRepository
      - Query station_areas WHERE region IN (detected_regions) AND is_active = true
      - Returns StationArea[] (from seeded DB data)

   c. BuildTravelTimeMatrix() — IRoutingProvider (cache-first)
      - For each candidate × each destination: geocode + get travel time
      - All calls go through CachedRoutingProvider → CachedGeocodeProvider
      - Cache hits skip external API calls

   d. IScoringService.ScoreCandidates() — pure deterministic function, no I/O
      - Input: StationArea[], TravelTimeMatrix, UserPreferences
      - Output: ScoredCandidate[] sorted descending by CandidateScore
      - Formula: (travel_time_score × 0.40) + (cost_score × 0.30)
               + (station_proximity_score × 0.15) + (food_access_score × 0.10)
               + (shopping_score × 0.05)

   e. Take top 3–5 ScoredCandidates

   f. [Async, parallel for all candidates]:
      - IHotelProvider.SearchAsync() — hotel preview (3 hotels per area)
      - IAIProvider.GenerateExplanationAsync() — text explanation
      - IFoodRepository.GetCuratedFoodAsync() + IAIProvider.SuggestFoodAsync()
      - IAttractionRepository.GetCuratedAttractionsAsync()

4. Return RecommendationResult[] (partial results returned if some async tasks fail)

5. Log to recommendation_logs (fire-and-forget, don't block response)
```

---

## Provider Abstraction Pattern

Every external integration follows this pattern:

```
Interface          Adapter(s)                  Cache Decorator
─────────────      ─────────────────────────   ─────────────────────
IAIProvider     →  GeminiAdapter               CachedAIProvider
                   MockAIAdapter               (wraps any IAIProvider,
                   RulesOnlyAdapter             checks ai_response_cache
                                                before calling inner)

IGeocodeProvider → NominatimAdapter            CachedGeocodeProvider
                   MockGeocodeAdapter          (checks geocode_cache)

IRoutingProvider → OsrmAdapter                 CachedRoutingProvider
                   SeededFallbackProvider      (checks routing_cache)

IHotelProvider  →  RakutenHotelAdapter         CachedHotelProvider
                   MockHotelAdapter            (checks hotel_search_cache)
```

Registration in `Program.cs` (resolved from config):
```csharp
// AI provider resolved from AI:Mode config
services.AddScoped<IAIProvider>(sp =>
{
    var mode = config["AI:Mode"];
    var inner = mode switch {
        "mock"       => (IAIProvider) new MockAIAdapter(),
        "rules_only" => new RulesOnlyAdapter(),
        _            => new GeminiAdapter(config["AI:ApiKey"])
    };
    return new CachedAIProvider(inner, sp.GetRequiredService<ICacheService>());
});
```

---

## Backend Layer Responsibilities

| Layer | Location | Responsibility | Must NOT |
|---|---|---|---|
| Controllers | `src/API/Controllers/` | Parse HTTP request, validate DTO, call one service method, return HTTP response | Contain business logic, call repositories directly, call external providers |
| Application Services | `src/Application/Services/` | Orchestrate use cases, coordinate between domain and infrastructure | Access DB directly (use repositories), know about HTTP |
| Domain | `src/Domain/` | Entities, value objects, scoring logic, business rules | Have any async I/O, reference infrastructure |
| Infrastructure | `src/Infrastructure/` | EF Core repos, provider adapters, cache implementation | Contain business rules |
| Shared | `src/Shared/` | Extension methods, utilities, constants | Have dependencies on application or domain layers |

---

## Frontend Architecture

```
src/app/
├── features/
│   ├── itinerary/           Feature: itinerary input, parsing, review, chat
│   │   ├── itinerary-input/
│   │   ├── itinerary-review/
│   │   ├── itinerary-chat/
│   │   └── itinerary.store.ts   (signal-based state)
│   ├── recommendations/     Feature: recommendation cards, detail
│   │   ├── recommendation-list/
│   │   ├── recommendation-card/
│   │   ├── recommendation-detail/
│   │   └── recommendations.store.ts
│   └── hotels/              Feature: hotel list, card, deep-link
│       ├── hotel-list/
│       ├── hotel-card/
│       └── hotels.store.ts
├── shared/
│   ├── components/          Reusable UI: spinner, error-banner, empty-state
│   ├── services/
│   │   ├── api.service.ts   HTTP wrapper (HttpClient)
│   │   └── session.service.ts  localStorage management
│   └── models/              TypeScript interfaces matching API contracts
└── app.routes.ts            Routes: /, /results, /area/:id
```

**State management:** Angular `signal()` + `computed()` in injectable store services. No NgRx. Stores are the single source of truth per feature; components read signals, call store methods for mutations.

**HTTP:** All API calls go through `ApiService` which wraps `HttpClient`, handles base URL configuration, and centralizes error normalization. No component calls `HttpClient` directly.

---

## Seeded Data Role

`station_areas` is the **candidate pool** for recommendations. The recommendation engine only surfaces areas that exist in this table. If a user's itinerary is in a city with no seeded areas, they'll get no results (edge case; documented in risks).

MVP seed: 10–15 station areas covering Tokyo, Osaka, Kyoto, and Hiroshima — sufficient for the most common tourist itineraries.

Seed data lives in `src/Infrastructure/Seed/` as JSON files, loaded by `DataSeeder : IHostedService` on startup (idempotent check: `station_areas` count < configured minimum).

---

## Caching Layer

All cache tables are in PostgreSQL. The `ICacheService<T>` interface:

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl);
}
```

Implemented by `PostgresCacheService`. Each cache table has its own `expires_at` column. A `CleanupHostedService` (runs every 6 hours) deletes rows where `expires_at < now()`.

Cache hit rates are logged: `Cache hit: {table} key={key}` — useful for profiling Nominatim rate limit exposure.

---

## Recommended Architecture Decision

### Primary: Angular SPA + .NET 8 API + PostgreSQL (Supabase) + Gemini Flash + Nominatim/OSRM + Rakuten + Vercel/Railway

**Why this is right for this project:**
- Angular + .NET 8 is the user's preferred stack. Both are mature, well-documented, and demonstrate full-stack competency for portfolio purposes.
- Provider abstraction throughout means no vendor lock-in; every external service can be swapped with a config change.
- PostgreSQL with all caches in the same DB keeps infrastructure simple — no Redis, no separate cache service.
- Gemini Flash free tier is genuinely usable at MVP scale. The deterministic fallback means AI failure doesn't break the core product.
- Free-tier hosting (Vercel + Railway + Supabase) is sufficient for a portfolio demo. Costs are near-zero.

### Alternative: Next.js 14 (App Router) + .NET 8 API + same backend stack

**Only reason to consider it:** If SEO is a V1 requirement (it is not). Next.js SSR would enable server-rendered pages for Japan destination SEO content.

**Why rejected for V1:** Angular is the user's stated preference, the app is interaction-heavy (not content-heavy), and SSR adds significant deployment complexity. Design the Angular app to be SSR-compatible (`isPlatformBrowser` guards) so migration is possible in Phase 3 if needed.

---

## Anti-Patterns to Avoid

| Anti-Pattern | Why it's a problem | Correct approach |
|---|---|---|
| Fat controllers | Hard to test, business logic scattered | Thin controllers: parse HTTP, call one service, return result |
| Direct provider calls in services | Bypasses cache, breaks abstraction | Always inject `IAIProvider` etc. (resolved with cache decorator) |
| Hardcoded API keys | Security risk, breaks CI mock mode | Environment variables only; mock adapters for dev/CI |
| AI-only recommendation logic | AI fails → app broken | Deterministic scoring first; AI only for explanations + suggestions |
| `localStorage` access in components | Hard to test, SSR incompatible | `SessionService` only; components call service methods |
| `window`/`document` direct access in components | Breaks future SSR | Always guard with `isPlatformBrowser()` from `SessionService` |
