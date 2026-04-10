# Session Resume — Where To Stay In Japan

**Last updated:** 2026-04-11  
**Current branch:** `feature/phase-1-itinerary-parsing`  
**Project phase:** Phase 1 complete — backend + frontend committed, Phase 2 next

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
- 3 unit tests for `RegionGroupingService.HaversineDistance` passing
- `DataSeeder` wrapped in try/catch to survive Npgsql write bug on startup
- Committed and pushed to `feature/phase-0-bootstrap`

### Phase 1 — Complete ✅

**Branch:** `feature/phase-1-itinerary-parsing` (2 commits, not yet pushed/PR'd)

**Backend:**
- `IAIProvider` moved to Application layer (correct layering)
- `IItineraryExtractor` abstraction + PlainText/PDF/Docx extractors
- `ItineraryParsingService` fully implemented
- `ItineraryNormalizer` bug fix: `IsMultiRegion` also checks `regions.Count > 1`
- 18 `ItineraryNormalizerTests` passing (21 total in Domain.Tests)
- `dotnet test`: 23 passed, 0 failed

**Frontend:**
- `core/models/itinerary.models.ts` — TS interfaces matching backend DTOs
- `core/services/api.service.ts` — parseText + parseFile
- `core/services/session.service.ts` — localStorage, 7-day TTL
- `core/stores/itinerary.store.ts` — signals: parsedItinerary, loading, error, userPreferences
- `ItineraryInputComponent` — preferences form, file drag-drop, textarea, parse button
- `ItineraryReviewComponent` — destination list, low-confidence/multi-region banners
- `ResultsPlaceholderComponent` — Phase 2 stub
- `AppComponent` — session resume banner
- `styles.scss` — CSS vars + Google Fonts
- `ng build` and `ng test`: passing

---

## Known Issues

### Npgsql 10.0.1 + Supabase Supavisor write bug ⚠️
- `SaveChangesAsync` throws `ObjectDisposedException: ManualResetEventSlim` on any write
- All write operations are affected (logs, future session persistence, etc.)
- Tried and failed: `No Reset On Close=true`, `Pooling=false`, `CancellationToken.None`, both pooler modes
- Npgsql 10.0.1 is already the latest for .NET 10; direct IPv6 is blocked at network level
- **Phase 1 is unaffected** — parsing is fully in-memory; no DB writes needed
- Phase 2+ will need this resolved before recommendation logs can be written

---

## Pending Tasks (in order)

### 1. Phase 2 — Recommendation Engine (backend)

Phase 1 is complete. Next work is the deterministic recommendation engine.

See `docs/planning/execution-plan.md` Phase 2 for detailed scope. High-level:
- `POST /api/recommendations` endpoint
- Score candidates by: travel time (0.4), hotel cost (0.3), station proximity (0.15), food access (0.1), shopping (0.05)
- `TravelTimeService` using seeded OSRM-style fallback data
- `RecommendationService` orchestrating scoring + explanation
- Unit tests for scoring logic

### 2. Phase 2 — Results page (frontend)

- Replace `ResultsPlaceholderComponent` with real `ResultsComponent`
- Call `POST /api/recommendations` after "Looks Good →"
- Display ranked area cards with scores, pros/cons, explanations
- Hotel preview rows with deep-link buttons

---

## Resume Prompt

Use this verbatim to continue in a new Claude session:

---

> I'm building a portfolio Angular + .NET 10 app called "Where To Stay In Japan". Phase 1 is complete. Please read `docs/session-resume.md` first to understand exactly where we left off, then continue from the top of the **Pending Tasks** list.
>
> Key context:
> - Branch: `feature/phase-1-itinerary-parsing` (2 commits ahead of origin, not yet pushed/PR'd)
> - All Phase 1 backend + frontend work is committed and clean
> - Next: Phase 2 — deterministic recommendation engine (backend) + results page (frontend)
> - CLAUDE.md rules apply: no commits to main, thin controllers, mock-first, token-efficient responses

---

## Phases at a Glance

| Phase | Goal | Status |
|---|---|---|
| **Phase 0** | Running skeleton. Mock adapters. CI passing. | ✅ Complete |
| **Phase 1** | Itinerary upload/paste, parsing, review, session save | ✅ Complete |
| **Phase 2** | Deterministic recommendation engine with travel time scoring | Not started |
| **Phase 3** | Gemini AI integration (parsing, explanations, chat) | Not started |
| **Phase 4** | Rakuten hotel search, pagination, deep-link click-out | Not started |
| **Phase 5** | Seed content, sakura theme, accessibility, responsive polish | Not started |
| **Phase 6** | Production deployment (Vercel + Railway + Supabase) | Not started |

---

## Key Architecture Decisions

- **Stack:** Angular (standalone, signals) + .NET 10 Web API + PostgreSQL (Supabase)
- **AI:** `MockAIAdapter` in dev (`AI:Mode = "mock"`), `RulesOnlyAdapter` for regex-only, `GeminiAdapter` for Phase 3
- **Scoring:** Deterministic scoring engine only — AI is for explanations, never ranking
- **Providers:** All external integrations behind interfaces with cache decorators
- **No auth in MVP:** Guest-only, localStorage session
- **No in-app booking:** Rakuten deep-link only
- **Hosting:** Vercel (frontend) + Railway (backend) + Supabase (PostgreSQL)

## Key Files

| File | Purpose |
|---|---|
| `src/API/Program.cs` | All DI registrations |
| `src/Application/Interfaces/IAIProvider.cs` | AI abstraction (Application layer) |
| `src/Application/Interfaces/IItineraryExtractor.cs` | File extraction abstraction |
| `src/Application/Services/ItineraryParsingService.cs` | Parse orchestration |
| `src/Infrastructure/Parsing/` | PlainText, PDF, Docx extractors |
| `src/Infrastructure/Adapters/AI/RulesOnlyAdapter.cs` | Regex-based parser (active in dev) |
| `src/Domain/Services/ItineraryNormalizer.cs` | Dedup, region lookup, multi-region flag |
| `docs/planning/execution-plan.md` | Phase-by-phase build checklist |
| `docs/technical/api-contracts.md` | API endpoint contracts and DTOs |
| `docs/technical/ui.md` | Angular structure and component spec |
