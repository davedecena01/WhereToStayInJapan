# Session Resume — Where To Stay In Japan

**Last updated:** 2026-04-11  
**Current branch:** `feature/phase-2-recommendation-engine`  
**Project phase:** Phase 2 complete — backend + frontend committed, Phase 3 next

---

## What This Is

This file is a handover document. Read it at the start of a new session to understand exactly where the project stands and what to do next.

---

## Current State

### Phase 0 — Complete ✅

- Full .NET 10 solution with all layers (API, Application, Domain, Infrastructure, Shared)
- All domain entities + EF Core `InitialCreate` migration applied to Supabase
- 15 station areas, 41 food items, 51 attractions seeded via Supabase MCP (not via DataSeeder)
- Mock adapters for AI, Hotels, Maps all wired and working
- `GET /api/health` returns `{ "status": "healthy", "db": "connected" }` ✅
- `GET /api/areas/{id}/food` and `/attractions` endpoints exist and work
- CI pipeline (dotnet test + ng build) in `.github/workflows/ci.yml`

### Phase 1 — Complete ✅

**Branch:** `feature/phase-1-itinerary-parsing` (committed, not yet PR'd)

- `IItineraryExtractor` + PlainText/PDF/Docx extractors
- `ItineraryParsingService` + `ItineraryNormalizer` (dedup, region lookup, multi-region detection)
- `POST /api/itinerary/parse` endpoint working
- Angular: itinerary input form, review page, session service, itinerary store
- `dotnet test`: 23 passed; `ng build` + `ng test`: passing

### Phase 2 — Complete ✅

**Branch:** `feature/phase-2-recommendation-engine` (1 commit, not yet pushed/PR'd)

**Backend:**
- Provider/repository interfaces moved to `Application/Interfaces/` (correct clean architecture layering)
  - `IGeocodeProvider`, `IRoutingProvider`, `IHotelProvider`, `IStationAreaRepository`, `IFoodRepository`, `IAttractionRepository`
  - Infrastructure files redirected via `global using` aliases
- `IScoringService` / `ScoringService` registered in DI
- `RecommendationService` fully implemented:
  - Fetches candidates from `IStationAreaRepository` by detected regions
  - Builds `TravelTimeMatrix` via geocode + routing providers (cache-first)
  - Scores candidates with `ScoringService` (weights: travel 0.4, cost 0.3, station 0.15, food 0.1, shop 0.05)
  - Enriches top 5 in parallel: hotel preview, AI explanation, curated food, curated attractions
  - Deterministic pros/cons generated from score breakdown
- `StayAreaRecommendationDto` updated: added `HotelPreview`, `HotelsAvailable`
- 7 `ScoringServiceTests` added — `dotnet test`: **32 passed, 0 failed**

**Frontend:**
- `RecommendationResult`, `StayAreaRecommendation`, `HotelItem`, `FoodItem`, etc. added to `itinerary.models.ts`
- `ApiService.getRecommendations()` — `POST /api/recommendations`
- `RecommendationStore` — signals: result, loading, error; computed: recommendations, isMultiRegion, hasResults
- `ResultsComponent` — ranked area cards with score bars, breakdown pills, pros/cons, hotel preview rows, food tags, loading skeleton, error state, multi-region warning banner
- `ItineraryReviewComponent` — now triggers `recStore.fetchRecommendations()` then navigates to `/results`
- Routes: `/results` → `ResultsComponent` (was placeholder)
- `ng build` + `ng test`: passing (1 test)

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

### 1. Phase 3 — Gemini AI Integration

Phase 2 is complete. Next is real AI for parsing, explanations, and food suggestions.

See `docs/planning/execution-plan.md` Phase 3 for detailed scope. High-level:
- `GeminiAdapter.ParseItineraryAsync()` with JSON schema prompt + response validation
- `GeminiAdapter.GenerateExplanationAsync()`
- `GeminiAdapter.SuggestFoodAsync()` + `SuggestAttractionsAsync()`
- `CachedAIProvider` SHA256 key (already exists — just needs real adapter)
- Polly retry for 429 responses
- Verify fallback chain: `AI:Mode = "rules_only"` still returns recommendations
- `POST /api/chat` controller + `ChatService`
- Angular `ItineraryChatComponent`

### 2. Phase 4 — Rakuten Hotel Integration

- `RakutenHotelAdapter.SearchAsync()` with price range + rating filter
- `GET /api/hotels` paginated endpoint
- `POST /api/analytics/hotel-click` fire-and-forget
- Angular `HotelListComponent` + `HotelCardComponent` with deep-link

---

## Resume Prompt

Use this verbatim to continue in a new Claude session:

---

> I'm building a portfolio Angular + .NET 10 app called "Where To Stay In Japan". Phase 2 is complete. Please read `docs/session-resume.md` first to understand exactly where we left off, then continue from the top of the **Pending Tasks** list.
>
> Key context:
> - Branch: `feature/phase-2-recommendation-engine` (1 commit ahead of origin, not yet pushed/PR'd)
> - All Phase 2 backend + frontend work is committed and clean
> - `git` command in bash fails with `_lc: command not found` — use full path workaround (see Known Issues)
> - Next: Phase 3 — Gemini AI integration
> - CLAUDE.md rules apply: no commits to main, thin controllers, mock-first, token-efficient responses

---

## Phases at a Glance

| Phase | Goal | Status |
|---|---|---|
| **Phase 0** | Running skeleton. Mock adapters. CI passing. | ✅ Complete |
| **Phase 1** | Itinerary upload/paste, parsing, review, session save | ✅ Complete |
| **Phase 2** | Deterministic recommendation engine with travel time scoring | ✅ Complete |
| **Phase 3** | Gemini AI integration (parsing, explanations, chat) | Not started |
| **Phase 4** | Rakuten hotel search, pagination, deep-link click-out | Not started |
| **Phase 5** | Seed content, sakura theme, accessibility, responsive polish | Not started |
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
| `src/API/Program.cs` | All DI registrations |
| `src/Application/Interfaces/` | All provider + repository interfaces (correct layer) |
| `src/Application/Services/RecommendationService.cs` | Recommendation orchestration |
| `src/Domain/Services/ScoringService.cs` | Pure scoring logic (weights, min-max normalization) |
| `src/Domain/Services/ItineraryNormalizer.cs` | Dedup, region lookup, multi-region flag |
| `src/Infrastructure/Adapters/AI/MockAIAdapter.cs` | Mock AI (active in dev) |
| `src/Infrastructure/Adapters/Hotels/MockHotelAdapter.cs` | Mock hotels (active in dev) |
| `frontend/src/app/core/stores/recommendation.store.ts` | Recommendation state (signals) |
| `frontend/src/app/features/results/results/results.component.ts` | Results page |
| `docs/planning/execution-plan.md` | Phase-by-phase build checklist |
| `docs/technical/api-contracts.md` | API endpoint contracts and DTOs |
