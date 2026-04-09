# Execution Plan — Where To Stay In Japan

Implementation order: always build mock adapters before real adapters. Never depend on live APIs to make progress. Each phase produces a testable, runnable increment.

---

## Phase 0: Project Bootstrap

**Goal:** Running app skeleton. All layers initialized. Mock mode fully functional. CI passing.
**Estimated effort:** 1–2 days

### Tasks

- [ ] Initialize .NET solution structure:
  ```
  dotnet new sln -n WhereToStayInJapan
  dotnet new webapi -n WhereToStayInJapan.API -o src/API
  dotnet new classlib -n WhereToStayInJapan.Application -o src/Application
  dotnet new classlib -n WhereToStayInJapan.Domain -o src/Domain
  dotnet new classlib -n WhereToStayInJapan.Infrastructure -o src/Infrastructure
  dotnet new classlib -n WhereToStayInJapan.Shared -o src/Shared
  dotnet new xunit -n WhereToStayInJapan.Domain.Tests -o tests/Domain.Tests
  dotnet new xunit -n WhereToStayInJapan.Application.Tests -o tests/Application.Tests
  dotnet new xunit -n WhereToStayInJapan.API.Tests -o tests/API.Tests
  ```

- [ ] Add all project references and NuGet packages (see `backend.md` for full package list)

- [ ] Initialize Angular project:
  ```
  ng new frontend --standalone --routing true --style scss --ssr false
  ```

- [ ] Set up Supabase dev project:
  - Create project at `supabase.com`
  - Copy connection string to `appsettings.Development.json`

- [ ] Define all domain entities (`StationArea`, `CuratedFood`, `CuratedAttraction`, all cache entities, log entities)

- [ ] Set up `ApplicationDbContext` with all entity configurations

- [ ] Create initial EF Core migration and apply to dev DB:
  ```bash
  dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/API
  dotnet ef database update --project src/Infrastructure --startup-project src/API
  ```

- [ ] Implement all provider interfaces (`IAIProvider`, `IGeocodeProvider`, `IRoutingProvider`, `IHotelProvider`)

- [ ] Implement `MockAIAdapter`, `MockGeocodeAdapter`, `MockHotelAdapter` — all return hardcoded test data

- [ ] Implement `ICacheService` + `PostgresCacheService`

- [ ] Implement `DataSeeder` (10 station areas, curated food/attractions, routing cache warm-up)

- [ ] Implement `GET /api/health` endpoint

- [ ] Register all services in `Program.cs` with mock mode defaults for development

- [ ] Configure CORS for `http://localhost:4200`

- [ ] Add `GlobalExceptionMiddleware` returning `ProblemDetails`

- [ ] Write 3 domain unit tests for `RegionGroupingService.HaversineDistance()` with known inputs

- [ ] Create `.github/workflows/ci.yml` — run `dotnet test` + `ng build` on push

- [ ] Verify: `dotnet test` passes, `ng build` passes, health endpoint returns `{"status":"healthy"}`

- [ ] First commit: `git add . && git commit -m "feat: project bootstrap with mock adapters"`

---

## Phase 1: Itinerary Input + Parsing

**Goal:** User can upload or paste an itinerary, see it parsed into structured form, and save the session.
**Estimated effort:** 2–3 days

### Tasks

- [ ] Implement `IItineraryExtractor` interface + `PdfExtractor` (PdfPig), `DocxExtractor` (OpenXml), `PlainTextExtractor`

- [ ] Write unit tests for each extractor with sample files

- [ ] Implement `RulesOnlyAdapter.ParseItineraryAsync()` — regex-based extraction, always returns `parsing_confidence: 'low'`

- [ ] Implement `IItineraryParsingService` orchestration (extract text → call AI adapter → post-process via `ItineraryNormalizer`)

- [ ] Implement `ItineraryNormalizer` (deduplication, region lookup, multi-region detection)

- [ ] Write unit tests for `ItineraryNormalizer` with multi-destination test cases

- [ ] Implement `POST /api/itinerary/parse` controller (file + text modes, `[RequestSizeLimit]`, FluentValidation)

- [ ] Test endpoint manually with curl:
  ```bash
  curl -X POST http://localhost:5000/api/itinerary/parse \
    -H "Content-Type: application/json" \
    -d '{"text":"Day 1: Shinjuku. Day 2: Asakusa. Day 3: Shibuya."}'
  ```

- [ ] Build Angular `ItineraryInputComponent`:
  - Preferences form (travel dates, travelers, budget, hotel types, atmosphere)
  - File drag-drop upload zone with client-side validation
  - Plain text paste area
  - "Parse My Itinerary" submit button
  - Loading skeleton while parsing

- [ ] Build Angular `ItineraryReviewComponent`:
  - Display parsed destinations in a structured list
  - Show `parsing_confidence: 'low'` warning banner
  - "Looks Good" button → store in `ItineraryStore` → navigate to `/results`

- [ ] Implement `SessionService` with full `WtsjpSession` schema, TTL validation, and save triggers

- [ ] Implement session restore banner in `AppComponent`

- [ ] Wire `ItineraryStore` to call `ApiService.parseFile()` / `parseText()` and update signals

- [ ] Verify: Upload a PDF → see structured destination list → click "Looks Good" → see session saved in localStorage

- [ ] Commit: `feat: itinerary upload, parsing, and session storage`

---

## Phase 2: Recommendation Engine

**Goal:** Deterministic scoring works end-to-end. Recommendations appear with travel times. No real AI yet (mock explanations).
**Estimated effort:** 3–4 days

### Tasks

- [ ] Implement `NominatimAdapter.GeocodeAsync()` with Polly rate limit handling and `User-Agent` header

- [ ] Implement `CachedGeocodeProvider` (cache decorator, 90-day TTL)

- [ ] Implement `OsrmAdapter.GetTravelTimeAsync()` with transit multiplier (×1.3)

- [ ] Implement `SeededFallbackRoutingProvider` (returns from routing_cache only)

- [ ] Implement `CachedRoutingProvider` (cache decorator, 7-day TTL)

- [ ] Write unit tests for `ScoringService.ScoreCandidates()`:
  - Test: lower travel time → higher rank
  - Test: lower cost → higher rank when travel times equal
  - Test: min-max normalization with 3 candidates
  - Test: preference modifier (avoid_walking increases station_proximity weight)

- [ ] Implement `IScoringService` / `ScoringService` (pure function, no I/O)

- [ ] Implement `IStationAreaRepository.GetByRegionsAsync()`

- [ ] Implement `IRecommendationService` orchestration:
  - Candidate selection
  - Travel time matrix (using cached providers)
  - Scoring
  - Async enrichment (mock AI explanations, mock hotels, curated food/attractions)

- [ ] Implement `POST /api/recommendations` controller

- [ ] Write integration test: `POST /api/recommendations` with mock adapters → verify 3 results returned with non-null scores

- [ ] Build Angular `RecommendationListComponent` (3 skeleton cards → replace with real cards)

- [ ] Build `RecommendationCardComponent` with rank badge, area, station, score bar, travel times, pros/cons, mock explanation

- [ ] Implement multi-region warning banner (shown when `is_multi_region = true`)

- [ ] Add recommendations to `SessionService.saveSession()` triggers

- [ ] Verify: Submit Tokyo + Kyoto itinerary → see multi-region warning → see grouped recommendations with travel times

- [ ] Commit: `feat: deterministic recommendation engine with travel time scoring`

---

## Phase 3: AI Integration

**Goal:** Real Gemini AI for parsing, explanations, food suggestions, and chat.
**Estimated effort:** 2 days

### Tasks

- [ ] Implement `GeminiAdapter.ParseItineraryAsync()` with JSON schema prompt + response validation

- [ ] Implement `GeminiAdapter.GenerateExplanationAsync()`

- [ ] Implement `GeminiAdapter.SuggestFoodAsync()`

- [ ] Implement `GeminiAdapter.SuggestAttractionsAsync()`

- [ ] Implement `CachedAIProvider` (cache decorator, SHA256 input hash key)

- [ ] Implement Polly retry policy for 429 responses in `GeminiAdapter`

- [ ] Verify fallback chain: set `AI:Mode = "rules_only"` → confirm recommendations still return without AI

- [ ] Implement `POST /api/chat` controller + `IChatService` / `ChatService`

- [ ] Build Angular `ItineraryChatComponent`:
  - Scrollable message list
  - Bottom input with send button
  - "Accept" button when AI suggests itinerary update

- [ ] Set `AI:Mode = "production"` in local dev with a real Gemini API key and test

- [ ] Verify: Paste a messy itinerary → see `parsing_confidence: 'high'` → see AI explanation on recommendation cards

- [ ] Commit: `feat: Gemini AI integration with fallback to rules-only mode`

---

## Phase 4: Hotel Integration

**Goal:** Real Rakuten hotel search, pagination, and deep-link click-out.
**Estimated effort:** 2–3 days

### Tasks

- [ ] Implement `RakutenHotelAdapter.SearchAsync()` with price range mapping and rating filter

- [ ] Implement `CachedHotelProvider` (30-minute TTL)

- [ ] Implement `IHotelSearchService` (thin orchestration over `IHotelProvider`)

- [ ] Implement `GET /api/hotels` controller with pagination

- [ ] Implement `POST /api/analytics/hotel-click` (fire-and-forget log write)

- [ ] Build Angular `HotelListComponent` with pagination controls

- [ ] Build `HotelCardComponent` with hotel image, name, price, rating, "Book on Rakuten" button

- [ ] Wire hotel card click: `window.open(deep_link_url, '_blank', 'noopener,noreferrer')` + call `ApiService.trackHotelClick()`

- [ ] Build `RecommendationDetailComponent` (area detail page): hotel list + food + attractions

- [ ] Implement `GET /api/areas/:id/food` (curated first, AI-supplemented if < 5)

- [ ] Implement `GET /api/areas/:id/attractions`

- [ ] Test hotel failure path: set `Hotels:Provider = "mock"` then simulate Rakuten error → verify `hotels_available: false` + frontend error state shown

- [ ] Verify: Navigate to area detail → see paginated hotels → click "Book on Rakuten" → Rakuten opens in new tab with hotel pre-selected

- [ ] Commit: `feat: Rakuten hotel integration with deep-link booking`

---

## Phase 5: Content and Polish

**Goal:** Curated seed data complete, visual theme applied, all states handled.
**Estimated effort:** 2–3 days

### Tasks

- [ ] Write curated food seed data for all 15 station areas (5–8 items each → ~100 records)
  - Include: name, cuisine_type, address (real or approximate), notes
  - Sources: personal knowledge, travel blogs, static references

- [ ] Write curated attractions seed data for all 15 station areas (5–10 items each)
  - Include: name, category, walk_minutes from station, notes

- [ ] Apply sakura visual theme (`styles/_variables.scss`):
  - Colors: `#F8A7BB`, `#1A2B4A`, `#FFF8F0`, `#D4A017`
  - Typography: Noto Sans JP + Inter (via Google Fonts or self-hosted)
  - Card shadows, border radii, hover states

- [ ] Build all skeleton card variants (recommendation, hotel, food)

- [ ] Build `EmptyStateComponent` variants (no hotels, no food, no recommendations)

- [ ] Build `ErrorBannerComponent` with type="error"|"warning"|"info", optional retry button

- [ ] Add responsive styles (tablet 1024px + mobile 375px) for all major components

- [ ] Add ARIA labels to all interactive elements (upload zone, recommendation cards, hotel cards)

- [ ] Keyboard navigation: verify all flows completable with keyboard only

- [ ] Verify: App looks polished at 1440px, functional at 375px, no raw skeleton states visible on final screens

- [ ] Commit: `feat: sakura visual theme, seed content, accessibility, and responsive polish`

---

## Phase 6: Deployment

**Goal:** App is live and accessible via public URLs.
**Estimated effort:** 1–2 days

### Tasks

- [ ] Create Supabase production project, copy connection string

- [ ] Create Railway project, connect GitHub repo, add all backend environment variables

- [ ] Set `AI:Mode = "production"`, `Hotels:Provider = "rakuten"` in Railway env vars

- [ ] Verify Railway deploys successfully and `/api/health` returns `{"status":"healthy"}`

- [ ] Verify EF Core migration runs at startup (check Railway logs)

- [ ] Verify seed data populated (check Supabase table viewer → `station_areas` has 15 rows)

- [ ] Import GitHub repo in Vercel, set build config and `vercel.json`

- [ ] Set `CORS:AllowedOrigins` in Railway to the Vercel production URL

- [ ] End-to-end smoke test on production:
  1. Open Vercel URL
  2. Paste sample Tokyo + Kyoto itinerary
  3. Submit → confirm 3+ recommendations appear
  4. Click hotel → confirm Rakuten opens in new tab

- [ ] Set up cron-job.org ping to `https://{railway-api-url}/api/health` every 6 days (prevents Supabase pause)

- [ ] Commit: `feat: production deployment configuration`

---

## Ordering Principle

Each phase builds on the previous. Never skip:
- Mock adapter → Real adapter
- Domain logic test → Service implementation
- API endpoint → Angular integration

If an external API is unavailable (Rakuten not yet approved, Gemini not set up), use mock mode and continue building. Real integrations are drop-in replacements.
