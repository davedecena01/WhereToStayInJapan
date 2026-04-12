# Session Resume — Where To Stay In Japan

**Last updated:** 2026-04-12 (Phase 6 complete — all bugs fixed, tested, committed)
**Current branch:** `feature/phase-6-deployment`
**Project phase:** Phase 6 — PR #5 open, pending manual Railway + Vercel deployment

---

## What This Is

This file is a handover document. Read it at the start of a new session to understand exactly where the project stands and what to do next.

---

## Current State

### Phases 0–5 — All Complete ✅

- Phase 0: .NET 10 skeleton, EF Core migrations applied to Supabase, 15 station areas + 41 food + 51 attractions seeded, mock adapters all wired
- Phase 1: Itinerary parse/review flow (text + file upload)
- Phase 2: Deterministic recommendation engine, scoring, `POST /api/recommendations`
- Phase 3: Gemini AI adapter, ChatService, `ItineraryChatComponent` in review page
- Phase 4: Rakuten hotel adapter, `HotelSearchService`, `/hotels/:areaId` page with pagination
- Phase 5: Responsive polish, ARIA labels, keyboard navigation, `ng build` + `dotnet test` (32 passed) green

### Phase 6 — PR Open, Awaiting Deployment ✅

**Branch:** `feature/phase-6-deployment` (pushed, PR #5 open → `feature/phase-5-polish`)

**All committed changes:**

| Commit | What |
|---|---|
| `33f8c24` | Backend runtime bugs from Phase 6 Playwright testing |
| `27a67e6` | P1/P2 UX fixes — session, chat, date validation |

**Deployment config (committed earlier):**
- `Dockerfile` at repo root — .NET 10 multi-stage build for Railway
- `frontend/vercel.json` — SPA rewrite rule + build command
- `src/API/Program.cs` — `MigrateAsync()` at startup (Production only), Nominatim/OSRM wired via config
- `.github/workflows/ci.yml` — updated to `--configuration production`

---

## What Was Completed This Session

### E2E Playwright Testing

All flows tested and passing:

| Flow | Status |
|---|---|
| Home page → parse → review → results | ✅ |
| Hotel list page + "Book on Rakuten" links | ✅ |
| File upload (.txt) → review page | ✅ |
| Mobile responsive layout (375×812) | ✅ |
| Keyboard navigation (tab order, skip link, Enter/Space on dropzone) | ✅ |
| ARIA attributes (role=alert, status, log, progressbar) | ✅ |

### Bugs Fixed and Committed

1. **HotelController `[FromQuery(Name)]`** — `area_id` query param was not binding to `Guid areaId` parameter (underscore vs camelCase). Fixed with explicit `[FromQuery(Name = "area_id")]`. This caused all hotel list pages to show "No hotels found."
2. **ChatController `[ValidateNever]`** — camelCase CurrentItinerary DTO was failing snake_case model binding validation
3. **UserPreferencesDto `bool? MustBeNearStation`** — Angular sends null for optional bool; was crashing deserialization
4. **RecommendationService sequential `EnrichAsync`** — `Task.WhenAll` on shared scoped `DbContext` caused concurrent-operation exception
5. **PostgresCacheService try/catch** — all DB operations now wrapped for graceful degradation
6. **Program.cs** — `MigrateAsync()` restricted to Production only; `PropertyNameCaseInsensitive = true` added to JSON options

### UX Fixes Applied and Committed

1. **`sessionId` class field** — was re-created as local variable on every chat send
2. **Dismiss banner** — now hides via local signal, does NOT clear session data
3. **`firstValueFrom`** — submit handler converted from `.subscribe()` to async/await
4. **Date validation** — checkout must be after checkin, inline error with `role="alert"`
5. **`$any()` removed** — replaced with typed `onInput(event: Event)` handler
6. **Unused param removed** — `onItineraryAccepted(updated: ParsedItinerary)` → no param

---

## Pending Tasks (pick up here)

### Step 1 — Manual deployment (outside code)

**Railway (backend):**
1. railway.app → New Project → Deploy from GitHub Repo → `davedecena01/WhereToStayInJapan`, root = repo root
2. Add environment variables (see list below)
3. Note the Railway URL → update `frontend/src/environments/environment.prod.ts`

**Vercel (frontend):**
1. vercel.com → New Project → import `davedecena01/WhereToStayInJapan`, Root Directory: `frontend`
2. After deploy, add Vercel URL to Railway `CORS__ALLOWEDORIGINS__0`
3. Commit and push `environment.prod.ts` update

**Supabase keep-alive:**
- cron-job.org → `https://{railway-url}/api/health` every 6 days

### Step 2 — Merge PR chain

Once deployed and verified:
1. Merge PR #5 → `feature/phase-5-polish`
2. Merge PR #4 → ... → `main`

---

## Railway Environment Variables

```
CONNECTIONSTRINGS__DEFAULTCONNECTION=Host=db.{ref}.supabase.co;Port=5432;Database=postgres;Username=postgres;Password={pw};SSL Mode=Require;Trust Server Certificate=true
AI__MODE=production
AI__GEMINIMODEL=gemini-1.5-flash
AI__GEMINIAPIKEY=your-gemini-api-key
HOTELS__PROVIDER=rakuten
HOTELS__APPLICATIONID=your-rakuten-app-id
HOTELS__SEARCHRADIUSKM=2
HOTELS__MINREVIEWRATING=3.5
MAPS__GEOCODEPROVIDER=nominatim
MAPS__ROUTINGPROVIDER=osrm
MAPS__NOMINATIMUSERAGENT=WhereToStayInJapan/1.0 (your-email@example.com)
CORS__ALLOWEDORIGINS__0=https://your-vercel-app.vercel.app
ASPNETCORE_ENVIRONMENT=Production
```

**Important:** Use port 5432 (direct connection), NOT the Supavisor pooler, in production.

---

## Known Issues

### Npgsql 10 + Supabase Supavisor (dev only) ⚠️
- In development, the app connects through the Supavisor pooler
- Workaround: `No Reset On Close=true` in `appsettings.Development.json` (gitignored, apply locally)
- Production uses direct DB connection — not affected

### `git` command fails in Claude Code bash ⚠️
- `git` in bash gives `_lc: command not found`
- Workaround: `"C:/Program Files/Git/cmd/git.exe" -C "c:/Users/My PC/source/repos/WhereToStayInJapan" <command>`

---

## Resume Prompt

Use this verbatim to continue in a new Claude session:

---

> I'm building a portfolio Angular + .NET 10 app called "Where To Stay In Japan". Please read `docs/session-resume.md` first to understand exactly where the project stands, then continue from the **Pending Tasks** list.
>
> Key context:
> - Branch: `feature/phase-6-deployment` (pushed, PR #5 open)
> - Phases 0–6 code is complete. All bugs fixed, all E2E tests passing. Only manual deployment steps remain (Railway + Vercel).
> - `git` in bash gives `_lc: command not found` — use full path: `"C:/Program Files/Git/cmd/git.exe" -C "c:/Users/My PC/source/repos/WhereToStayInJapan" <command>`
> - CLAUDE.md rules apply: no commits to main, token-efficient responses, spec-driven

---

## Phases at a Glance

| Phase | Goal | Status |
|---|---|---|
| **Phase 0** | Running skeleton. Mock adapters. CI passing. | ✅ Complete |
| **Phase 1** | Itinerary upload/paste, parsing, review, session save | ✅ Complete |
| **Phase 2** | Deterministic recommendation engine with travel time scoring | ✅ Complete |
| **Phase 3** | Gemini AI integration (parsing, explanations, chat) | ✅ Complete |
| **Phase 4** | Rakuten hotel search, pagination, deep-link click-out | ✅ Complete |
| **Phase 5** | Responsive polish, ARIA labels, keyboard navigation | ✅ Complete |
| **Phase 6** | Production deployment (Vercel + Railway + Supabase) | 🔄 PR open — deploy manually |

---

## Key Architecture Decisions

- **Stack:** Angular (standalone, signals) + .NET 10 Web API + PostgreSQL (Supabase)
- **AI:** `MockAIAdapter` in dev (`AI:Mode = "mock"`), `GeminiAdapter` in production
- **Scoring:** Deterministic only — AI is for explanations, never ranking
- **Providers:** All external integrations behind interfaces in `Application/Interfaces/`
- **No auth in MVP:** Guest-only, localStorage session
- **No in-app booking:** Rakuten deep-link only
- **Hosting:** Vercel (frontend) + Railway (backend) + Supabase (PostgreSQL)
- **JSON naming:** Global snake_case policy (`JsonNamingPolicy.SnakeCaseLower`) in `Program.cs`

## Key Files

| File | Purpose |
|---|---|
| `src/API/Program.cs` | All DI registrations — provider selection by config key |
| `src/Application/Services/RecommendationService.cs` | Recommendation orchestration (sequential DB ops) |
| `src/Domain/Services/ScoringService.cs` | Pure scoring logic (weights, min-max normalization) |
| `src/Infrastructure/Seed/*.json` | Station areas, food, attractions seed data |
| `src/Infrastructure/Cache/PostgresCacheService.cs` | Cache service — all DB ops wrapped in try/catch |
| `src/Infrastructure/Adapters/AI/GeminiAdapter.cs` | Gemini AI adapter |
| `src/Infrastructure/Adapters/Hotels/RakutenHotelAdapter.cs` | Rakuten hotel adapter |
| `frontend/src/environments/environment.prod.ts` | Production API URL (update after Railway deploy) |
| `frontend/vercel.json` | Vercel build + SPA rewrite config |
| `Dockerfile` | Railway backend Docker image |
| `docs/technical/deployment.md` | Full deployment reference |
