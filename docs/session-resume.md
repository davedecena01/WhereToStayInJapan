# Session Resume — Where To Stay In Japan

_Last updated: 2026-04-09_

---

## What This Is

This file is a handover document. Read it at the start of a new session to quickly understand where the project stands and what to do next.

---

## Current State

**No code has been written yet.** The repository contains comprehensive planning documentation only.

### What Exists

| Location | Contents |
|---|---|
| `docs/product/project-spec.md` | Full MVP product spec, user stories, success criteria |
| `docs/product/user-flows.md` | User journey flows |
| `docs/technical/system-architecture.md` | Architecture diagram, layer responsibilities, provider abstraction pattern |
| `docs/technical/technical-spec.md` | Full technical specification |
| `docs/technical/data-model.md` | Entity definitions and DB schema |
| `docs/technical/api-contracts.md` | API endpoint contracts and DTOs |
| `docs/technical/backend.md` | Backend coding standards, NuGet packages, project structure |
| `docs/technical/ui.md` | Angular structure, component design, styling spec |
| `docs/technical/ai-strategy.md` | AI provider abstraction, fallback modes, prompt design |
| `docs/technical/maps-and-routing.md` | Nominatim/OSRM usage, caching, transit approximation |
| `docs/technical/hotel-integration.md` | Rakuten Travel API integration spec |
| `docs/technical/auth-and-storage.md` | Guest session (localStorage), Phase 2 auth plan |
| `docs/technical/deployment.md` | Vercel + Railway + Supabase deployment guide |
| `docs/technical/observability.md` | Logging and analytics scope |
| `docs/planning/execution-plan.md` | Phase-by-phase implementation checklist (the build order) |
| `docs/planning/phased-roadmap.md` | MVP, Phase 2, and Phase 3 feature roadmap |
| `docs/planning/risks-and-open-questions.md` | Known risks and deferred decisions |
| `.claude/CLAUDE.md` | Claude behavior rules for this project |

---

## What to Build Next: Phase 0 — Project Bootstrap

**All implementation tasks are in [`docs/planning/execution-plan.md`](planning/execution-plan.md).** That is the authoritative source of truth for build order. Do not deviate from it.

Phase 0 is the starting point. It produces a running skeleton with mock adapters — no live API keys required.

### Phase 0 Checklist (nothing is done yet)

- [ ] Initialize .NET solution with 5 projects: `API`, `Application`, `Domain`, `Infrastructure`, `Shared`
- [ ] Add 3 xUnit test projects: `Domain.Tests`, `Application.Tests`, `API.Tests`
- [ ] Initialize Angular project (standalone, signals, SCSS, no SSR)
- [ ] Set up Supabase dev project and paste connection string into `appsettings.Development.json`
- [ ] Define all domain entities and configure `ApplicationDbContext`
- [ ] Create and apply initial EF Core migration
- [ ] Implement all provider interfaces: `IAIProvider`, `IGeocodeProvider`, `IRoutingProvider`, `IHotelProvider`
- [ ] Implement `MockAIAdapter`, `MockGeocodeAdapter`, `MockHotelAdapter` (hardcoded test data)
- [ ] Implement `ICacheService` + `PostgresCacheService`
- [ ] Implement `DataSeeder` (10 station areas seeded, curated food/attractions, routing cache warm-up)
- [ ] Implement `GET /api/health` endpoint
- [ ] Register all services in `Program.cs` (mock mode by default for dev)
- [ ] Configure CORS for `http://localhost:4200`
- [ ] Add `GlobalExceptionMiddleware` returning `ProblemDetails`
- [ ] Write 3 unit tests for `RegionGroupingService.HaversineDistance()`
- [ ] Create `.github/workflows/ci.yml` (runs `dotnet test` + `ng build` on push)
- [ ] Verify: `dotnet test` passes, `ng build` passes, `/api/health` returns `{"status":"healthy"}`
- [ ] First commit: `feat: project bootstrap with mock adapters`

---

## Phases at a Glance

| Phase | Goal | Status |
|---|---|---|
| **Phase 0** | Running skeleton. Mock adapters. CI passing. | **Not started** |
| **Phase 1** | Itinerary upload/paste, parsing, review, session save | Not started |
| **Phase 2** | Deterministic recommendation engine with travel time scoring | Not started |
| **Phase 3** | Gemini AI integration (parsing, explanations, chat, food) | Not started |
| **Phase 4** | Rakuten hotel search, pagination, deep-link click-out | Not started |
| **Phase 5** | Seed content, sakura theme, accessibility, responsive polish | Not started |
| **Phase 6** | Production deployment (Vercel + Railway + Supabase) | Not started |

---

## Key Architecture Decisions Already Made

These are locked in. Do not re-litigate them.

- **Stack:** Angular 17 (standalone, signals) + .NET 8 Web API + PostgreSQL
- **AI:** Gemini Flash free tier with `RulesOnlyAdapter` fallback — never AI-only
- **Scoring:** Deterministic scoring engine. AI is only for explanations + suggestions, never for ranking
- **Providers:** All external integrations (`IAIProvider`, `IGeocodeProvider`, `IRoutingProvider`, `IHotelProvider`) are behind interfaces with cache decorators
- **No auth in MVP:** Guest-only with localStorage session (30-day TTL)
- **No in-app booking:** Rakuten deep-link only
- **Hosting:** Vercel (frontend) + Railway (backend) + Supabase (PostgreSQL) — all free tier

---

## Open Questions (deferred decisions)

See `docs/planning/risks-and-open-questions.md` for full context.

| # | Question | Decision |
|---|---|---|
| 1 | Transit time accuracy (OSRM driving × 1.3 vs hardcoded transit times) | Use ×1.3 for V1; add hardcoded seed times as enhancement |
| 2 | Rakuten affiliate approval timeline | Register early; demo uses direct links until approved |
| 3 | Angular SSR compatibility | Design SSR-compatible now; activate in Phase 3 |
| 4 | AI chat scope (full edit vs Q&A only) | Implement full edit if time allows; fall back to Q&A only |
| 5 | Multi-language | English only for MVP; include `name_ja` in seed JSON for Phase 3 backfill |

---

## How to Start a New Session

1. Read this file.
2. Open `docs/planning/execution-plan.md`.
3. Find the first unchecked task in the current phase.
4. Check `docs/technical/` for the relevant spec before implementing.
5. Follow the architecture in `docs/technical/system-architecture.md`.
6. Follow Claude behavior rules in `.claude/CLAUDE.md`.

Always check specs before building. Always build mock adapters before real adapters. Always keep controllers thin.
