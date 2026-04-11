# Session Resume — Where To Stay In Japan

**Last updated:** 2026-04-12 (Phase 5)  
**Current branch:** `feature/phase-5-polish`  
**Project phase:** Phase 5 complete — responsive polish, ARIA labels, keyboard nav, global CSS vars committed, Phase 6 next

---

## What This Is

This file is a handover document. Read it at the start of a new session to understand exactly where the project stands and what to do next.

---

## Current State

### Phase 0 — Complete ✅

- Full .NET 10 solution with all layers (API, Application, Domain, Infrastructure, Shared)
- All domain entities + EF Core `InitialCreate` migration applied to Supabase
- 15 station areas, 41 food items, 51 attractions seeded via Supabase MCP
- Mock adapters for AI, Hotels, Maps all wired and working
- `GET /api/health` returns `{ "status": "healthy", "db": "connected" }` ✅
- `GET /api/areas/{id}/food` and `/attractions` endpoints exist and work
- CI pipeline (dotnet test + ng build) in `.github/workflows/ci.yml`

### Phase 1 — Complete ✅

- `IItineraryExtractor` + PlainText/PDF/Docx extractors
- `ItineraryParsingService` + `ItineraryNormalizer` (dedup, region lookup, multi-region detection)
- `POST /api/itinerary/parse` endpoint working
- Angular: itinerary input form, review page, session service, itinerary store

### Phase 2 — Complete ✅

**Branch:** `feature/phase-2-recommendation-engine` (pushed, PR #1 open → `feature/phase-0-bootstrap`)

**Backend:**
- All provider/repository interfaces in `Application/Interfaces/` (correct clean architecture layering):
  - `IGeocodeProvider`, `IRoutingProvider`, `IHotelProvider`
  - `IStationAreaRepository`, `IFoodRepository`, `IAttractionRepository`
  - Infrastructure files redirect via `global using` type aliases
- `IScoringService` / `ScoringService` — pure min-max scoring, registered in DI
- `RecommendationService` fully implemented:
  - Fetches candidates by detected regions from `IStationAreaRepository`
  - Builds `TravelTimeMatrix` via geocode + routing providers (cache-first)
  - Scores candidates with weights: travel 0.4, cost 0.3, station 0.15, food 0.1, shop 0.05
  - Enriches top 5 in parallel: hotel preview, AI explanation, curated food, curated attractions
  - Deterministic pros/cons generated from score breakdown
- `StayAreaRecommendationDto` has `HotelPreview` + `HotelsAvailable`
- `POST /api/recommendations` controller wired
- 7 `ScoringServiceTests` — `dotnet test`: **32 passed, 0 failed**

**Frontend:**
- `RecommendationResult`, `StayAreaRecommendation`, `HotelItem`, `FoodItem` etc. in `itinerary.models.ts`
- `ApiService.getRecommendations()` — `POST /api/recommendations`
- `RecommendationStore` — signals: result, loading, error; computed: recommendations, isMultiRegion, hasResults
- `ResultsComponent` — ranked area cards with score bars, breakdown pills, pros/cons, hotel preview rows, food tags, loading skeleton, error state, multi-region warning banner
- `ItineraryReviewComponent` — triggers `recStore.fetchRecommendations()` then navigates to `/results`
- Routes: `/results` → `ResultsComponent`
- `ng build` + `ng test`: passing

### Phase 3 — Complete ✅

**Branch:** `feature/phase-3-gemini-ai` (pushed, PR #2 open → `feature/phase-2-recommendation-engine`)

**Backend:**
- `GeminiAdapter` fully implemented: `ParseItineraryAsync` (JSON schema prompt + fallback), `GenerateExplanationAsync`, `SuggestFoodAsync`, `SuggestAttractionsAsync`
- Polly v8 retry pipeline for HTTP 429 (exponential backoff, 3 retries)
- `ChatService.SendMessageAsync()` fully implemented — heuristic to detect new itinerary vs. chat, uses `IAIProvider`
- `appsettings.json` updated with `AI:GeminiModel` key
- `Program.cs` passes `IHttpClientFactory.CreateClient("gemini")` to `GeminiAdapter`
- `dotnet test`: **32 passed, 0 failed**

**Frontend:**
- `ChatMessage`, `ChatResponse`, `ChatItinerary`, `ChatDestination` interfaces in `itinerary.models.ts`
- `ApiService.sendChatMessage()` — `POST /api/chat`
- `ItineraryChatComponent` — scrollable message list, bottom input, "Accept" button on itinerary updates
- Embedded in `ItineraryReviewComponent` with `chatItinerary` computed signal for format conversion
- `ng build` passing

### Phase 4 — Complete ✅

**Branch:** `feature/phase-4-rakuten-hotels` (pushed, PR #3 open → `feature/phase-3-gemini-ai`)

**Backend:**
- `RakutenHotelAdapter` — full implementation: price range mapping, Rakuten API response deserialization, min-rating filter, deep-link building with optional affiliate ID
- `HotelProviderException` — custom exception for adapter failures
- `HotelSearchService` — real implementation: fetches area lat/lng from `IStationAreaRepository`, calls `IHotelProvider`, maps to `HotelItemDto`, returns empty on provider failure (non-throwing)
- `IHotelProvider.HotelSearchParams` — added `Travelers` field
- `HotelSearchResultDto` — added `Total` and `Provider` fields
- `Program.cs` — added **snake_case JSON policy** (fixes frontend/backend naming alignment), passes config + logger to `RakutenHotelAdapter`
- `AnalyticsController` — fire-and-forget hotel click log with try/catch (handles Npgsql write bug gracefully)
- `dotnet test`: **32 passed, 0 failed**

**Frontend:**
- `HotelCardComponent` — card with thumbnail, price, rating, "Book on Rakuten →" deep-link button
- `HotelListComponent` — paginated hotel list page at `/hotels/:areaId`, loads from `GET /api/hotels`
- `ApiService.getHotels()` + `trackHotelClick()` added
- `HotelSearchResult`, `HotelClickRequest` interfaces added to `itinerary.models.ts`
- Results page: "View all hotels →" router link per area card
- `app.routes.ts`: `/hotels/:areaId` route
- `ng build` passing

### Phase 5 — Complete ✅

**Branch:** `feature/phase-5-polish` (not yet pushed/PR'd)

**Frontend:**
- Global CSS variables (`--primary`, `--text-primary`, `--text-muted`, `--border`, `--surface`) defined in `styles.scss`
- Skip-to-main-content link in `app.html` for keyboard nav
- `<main id="main-content">` wrapper in `app.html`
- Drop zone: `role="button"`, `tabindex="0"`, `aria-label`, keyboard activation (`keydown.enter`/`keydown.space`)
- Hotel card book button: `aria-label` with hotel name
- Pagination: `<nav aria-label>`, `aria-label` on prev/next, `aria-live` on page indicator
- `aria-busy` on parse submit button during loading
- Mobile responsive breakpoints (≤600px) added to all components: input page, review page, results page, hotel list, app shell

**Seed data:** Already complete from Phase 4 — 15 areas, 41 food items, 51 attractions in `src/Infrastructure/Seed/`

**Still stub — NOT yet implemented:**

| File | Phase | Notes |
|---|---|---|
| `src/Infrastructure/Adapters/Maps/NominatimAdapter.cs` | Phase 6 | Throws — MockGeocodeAdapter active in dev |
| `src/Infrastructure/Adapters/Maps/OsrmAdapter.cs` | Phase 6 | Throws — SeededFallbackRoutingProvider active |

---

## Known Issues

### Npgsql 10.0.1 + Supabase Supavisor write bug ⚠️
- `SaveChangesAsync` throws `ObjectDisposedException: ManualResetEventSlim` on any write
- All write operations are affected (recommendation logs, hotel click logs, etc.)
- Phase 2 is unaffected — all reads, no writes needed for recommendations
- Phase 3+ will need this resolved before recommendation logs can be written

### `git` command fails in Claude Code bash hook ⚠️
- Running `git status` in bash results in `_lc: command not found`
- Workaround: use full path `"C:/Program Files/Git/cmd/git.exe" -C <repo> <command>`
- Root cause: a Claude Code bash hook tries to invoke `_lc` (lean-ctx CLI) which isn't on PATH

---

## Pending Tasks (in order)

### 1. Push Phase 5 branch + create PR

Branch `feature/phase-5-polish` has commits and is not yet pushed.

```bash
"C:/Program Files/Git/cmd/git.exe" -C "c:/Users/My PC/source/repos/WhereToStayInJapan" push -u origin feature/phase-5-polish
```

Then create a PR. Base: `feature/phase-4-rakuten-hotels`.

**To enable real Rakuten hotels locally:**
1. Register at `https://webservice.rakuten.co.jp/` and get an `applicationId`
2. Add `Hotels__ApiKey=<your_key>` to environment or `appsettings.Development.json`
3. Set `Hotels:Provider` to `"rakuten"`
4. Verify: go to results → click "View all hotels →" → see real hotels

### 2. Phase 6 — Production deployment (Vercel + Railway + Supabase)

Create new branch `feature/phase-6-deployment` before starting.

---

## Resume Prompt

Use this verbatim to continue in a new Claude session:

---

> I'm building a portfolio Angular + .NET 10 app called "Where To Stay In Japan". Phase 5 is complete. Please read `docs/session-resume.md` first to understand exactly where we left off, then continue from the top of the **Pending Tasks** list.
>
> Key context:
> - Branch: `feature/phase-5-polish` (commits ahead of origin, not yet pushed/PR'd)
> - All Phase 5 frontend polish is committed and clean — `dotnet test` 32 passed, `ng build` passing
> - `git` command in bash fails with `_lc: command not found` — use full path workaround (see Known Issues)
> - Next: push Phase 5 branch, create PR, then start Phase 6 — production deployment (Vercel + Railway + Supabase) on a new branch
> - CLAUDE.md rules apply: no commits to main, thin controllers, mock-first, token-efficient responses

---

## Phases at a Glance

| Phase | Goal | Status |
|---|---|---|
| **Phase 0** | Running skeleton. Mock adapters. CI passing. | ✅ Complete |
| **Phase 1** | Itinerary upload/paste, parsing, review, session save | ✅ Complete |
| **Phase 2** | Deterministic recommendation engine with travel time scoring | ✅ Complete |
| **Phase 3** | Gemini AI integration (parsing, explanations, chat) | ✅ Complete |
| **Phase 4** | Rakuten hotel search, pagination, deep-link click-out | ✅ Complete |
| **Phase 5** | Seed content, sakura theme, accessibility, responsive polish | ✅ Complete |
| **Phase 6** | Production deployment (Vercel + Railway + Supabase) | Not started |

---

## Key Architecture Decisions

- **Stack:** Angular (standalone, signals) + .NET 10 Web API + PostgreSQL (Supabase)
- **AI:** `MockAIAdapter` in dev (`AI:Mode = "mock"`), `RulesOnlyAdapter` for regex-only, `GeminiAdapter` for Phase 3
- **Scoring:** Deterministic scoring engine only — AI is for explanations, never ranking
- **Providers:** All external integrations behind interfaces in `Application/Interfaces/` with cache decorators in Infrastructure
- **No auth in MVP:** Guest-only, localStorage session
- **No in-app booking:** Rakuten deep-link only
- **Hosting:** Vercel (frontend) + Railway (backend) + Supabase (PostgreSQL)

## Key Files

| File | Purpose |
|---|---|
| `src/API/Program.cs` | All DI registrations — provider selection by config key |
| `src/Application/Interfaces/` | All provider + repository interfaces (correct layer) |
| `src/Application/Services/RecommendationService.cs` | Recommendation orchestration |
| `src/Domain/Services/ScoringService.cs` | Pure scoring logic (weights, min-max normalization) |
| `src/Domain/Services/ItineraryNormalizer.cs` | Dedup, region lookup, multi-region flag |
| `src/Infrastructure/Adapters/AI/GeminiAdapter.cs` | **Phase 3 target** — currently throws |
| `src/Infrastructure/Adapters/AI/CachedAIProvider.cs` | Cache decorator for any IAIProvider — already done |
| `src/Infrastructure/Adapters/AI/MockAIAdapter.cs` | Mock AI (active in dev) |
| `src/Infrastructure/Adapters/Hotels/MockHotelAdapter.cs` | Mock hotels (active in dev) |
| `frontend/src/app/core/stores/recommendation.store.ts` | Recommendation state (signals) |
| `frontend/src/app/features/results/results/results.component.ts` | Results page |
| `docs/planning/execution-plan.md` | Phase-by-phase build checklist |
| `docs/technical/api-contracts.md` | API endpoint contracts and DTOs |
