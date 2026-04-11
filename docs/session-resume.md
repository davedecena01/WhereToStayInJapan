# Session Resume — Where To Stay In Japan

**Last updated:** 2026-04-12 (mid-Phase 6)
**Current branch:** `feature/phase-6-deployment`
**Project phase:** Phase 6 in progress — deployment config partially complete

---

## What This Is

This file is a handover document. Read it at the start of a new session to understand exactly where the project stands and what to do next.

---

## Current State

### Phase 0 — Complete ✅

- Full .NET 10 solution with all layers (API, Application, Domain, Infrastructure, Shared)
- All domain entities + EF Core `InitialCreate` migration applied to Supabase
- 15 station areas, 41 food items, 51 attractions seeded via JSON files in `src/Infrastructure/Seed/`
- Mock adapters for AI, Hotels, Maps all wired and working
- `GET /api/health` returns `{ "status": "healthy", "db": "connected" }` ✅
- CI pipeline (dotnet test + ng build) in `.github/workflows/ci.yml`

### Phase 1 — Complete ✅

- `IItineraryExtractor` + PlainText/PDF/Docx extractors
- `ItineraryParsingService` + `ItineraryNormalizer` (dedup, region lookup, multi-region detection)
- `POST /api/itinerary/parse` endpoint working
- Angular: itinerary input form, review page, session service, itinerary store

### Phase 2 — Complete ✅

**Branch:** `feature/phase-2-recommendation-engine` (pushed, PR #1 open → `feature/phase-0-bootstrap`)

- `IScoringService` / `ScoringService` — pure min-max scoring
- `RecommendationService` — fetches candidates, builds TravelTimeMatrix, scores, enriches top 5
- Weights: travel 0.4, cost 0.3, station 0.15, food 0.1, shop 0.05
- `POST /api/recommendations` controller wired
- Angular: `RecommendationStore`, `ResultsComponent` with score bars, pros/cons, hotel preview, food tags

### Phase 3 — Complete ✅

**Branch:** `feature/phase-3-gemini-ai` (pushed, PR #2 open → `feature/phase-2-recommendation-engine`)

- `GeminiAdapter` — `ParseItineraryAsync`, `GenerateExplanationAsync`, `SuggestFoodAsync`, `SuggestAttractionsAsync`
- Polly v8 retry pipeline for HTTP 429 (exponential backoff, 3 retries)
- `ChatService.SendMessageAsync()` with heuristic new-itinerary vs chat detection
- Angular: `ItineraryChatComponent` embedded in review page

### Phase 4 — Complete ✅

**Branch:** `feature/phase-4-rakuten-hotels` (pushed, PR #3 open → `feature/phase-3-gemini-ai`)

- `RakutenHotelAdapter` — price range mapping, Rakuten API deserialization, deep-link building
- `HotelSearchService` — non-throwing, returns empty on provider failure
- Global snake_case JSON policy in `Program.cs` (`JsonNamingPolicy.SnakeCaseLower`)
- Angular: `HotelCardComponent`, `HotelListComponent` at `/hotels/:areaId`, pagination

### Phase 5 — Complete ✅

**Branch:** `feature/phase-5-polish` (pushed, PR #4 open → `feature/phase-4-rakuten-hotels`)

- Global CSS vars (`--primary`, `--text-primary`, `--text-muted`, `--border`, `--surface`) in `styles.scss`
- Skip-to-main-content link + `<main id="main-content">` in `app.html`
- Drop zone: `role="button"`, `tabindex="0"`, `aria-label`, Enter/Space activation
- Hotel card Book button: `aria-label` with hotel name
- Pagination: `<nav aria-label>`, Prev/Next `aria-label`, `aria-live` on page count
- Mobile responsive breakpoints (≤600px) on all components
- `dotnet test`: 32 passed | `ng build`: passing

### Phase 6 — In Progress 🔄

**Branch:** `feature/phase-6-deployment` (local only, not yet pushed)

**Completed so far (NOT YET COMMITTED):**
- `Dockerfile` created at repo root — .NET 10 multi-stage build for Railway
- `frontend/vercel.json` created — SPA rewrite rule + build command

**Still TODO (pick up here next session):**

| Task | File | Notes |
|---|---|---|
| Add `fileReplacements` to prod build | `frontend/angular.json` | Wire `environment.prod.ts` into production config |
| Update prod API URL | `frontend/src/environments/environment.prod.ts` | Replace placeholder with `https://{your-railway-app}.up.railway.app` |
| Add `MigrateAsync()` at startup | `src/API/Program.cs` | Run EF Core migrations on startup before app starts serving |
| Wire Nominatim + OSRM via config | `src/API/Program.cs` | Add `nominatim`/`osrm` cases to geocode/routing provider selection |
| Update CI for prod build | `.github/workflows/ci.yml` | Change frontend build step to use `--configuration production` |
| Commit + push Phase 6 branch | git | Then create PR #5 → `feature/phase-5-polish` |

---

## Known Issues

### Npgsql 10.0.1 + Supabase Supavisor write bug ⚠️
- `SaveChangesAsync` throws `ObjectDisposedException: ManualResetEventSlim` on any write
- All write operations affected (recommendation logs, hotel click logs, etc.)
- Workaround in place: `AnalyticsController` uses fire-and-forget try/catch
- **Resolution for Phase 6:** Use direct connection (port 5432) instead of Supavisor (port 6543) in production — the bug is specific to PgBouncer/transaction mode

### `git` command fails in Claude Code bash hook ⚠️
- Running `git` in bash results in `_lc: command not found`
- Workaround: use full path `"C:/Program Files/Git/cmd/git.exe" -C <repo> <command>`

---

## Pending Tasks (in order)

### 1. Finish Phase 6 deployment config (pick up mid-task)

**Step 1 — `frontend/angular.json`:** Add `fileReplacements` to the `production` configuration block so `environment.prod.ts` is swapped in at build time:

```json
"production": {
  "fileReplacements": [
    {
      "replace": "src/environments/environment.ts",
      "with": "src/environments/environment.prod.ts"
    }
  ],
  "budgets": [ ... ],
  "outputHashing": "all"
}
```

**Step 2 — `frontend/src/environments/environment.prod.ts`:** Update the placeholder URL:
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://YOUR-APP.up.railway.app'  // fill in after Railway deploy
};
```
(Commit with a `TODO` comment — update after first Railway deploy confirms the URL.)

**Step 3 — `src/API/Program.cs`:** Add auto-migration at startup (after `var app = builder.Build();`):
```csharp
// Auto-run EF Core migrations at startup (safe for single-instance Railway deploy)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}
```

**Step 4 — `src/API/Program.cs`:** Wire Nominatim and OSRM providers via config (currently hardcoded to mock):
```csharp
var geocodeProvider = builder.Configuration["Maps:GeocodeProvider"] ?? "mock";
builder.Services.AddScoped<IGeocodeProvider>(sp =>
    new CachedGeocodeProvider(
        geocodeProvider == "nominatim"
            ? new NominatimAdapter(sp.GetRequiredService<IHttpClientFactory>().CreateClient("nominatim"))
            : new MockGeocodeAdapter(),
        sp.GetRequiredService<ICacheService>()));

var routingProvider = builder.Configuration["Maps:RoutingProvider"] ?? "seeded";
builder.Services.AddScoped<IRoutingProvider>(sp =>
    new CachedRoutingProvider(
        routingProvider == "osrm"
            ? new OsrmAdapter(sp.GetRequiredService<IHttpClientFactory>().CreateClient("osrm"))
            : new SeededFallbackRoutingProvider(sp.GetRequiredService<ICacheService>()),
        sp.GetRequiredService<ICacheService>()));
```

**Step 5 — `.github/workflows/ci.yml`:** Change frontend build step:
```yaml
- name: Build
  working-directory: frontend
  run: npm run build -- --configuration production
```

**Step 6 — Commit, push, create PR #5 → `feature/phase-5-polish`.**

### 2. Manual deployment steps (outside of code changes)

These are done in browser/dashboard — not automatable by Claude:

**Railway (backend):**
1. Go to railway.app → New Project → Deploy from GitHub Repo
2. Select `davedecena01/WhereToStayInJapan`, root directory = repo root (uses `Dockerfile`)
3. Add environment variables (see list below)
4. Note the Railway URL (e.g. `https://abc123.up.railway.app`) → update `environment.prod.ts`

**Vercel (frontend):**
1. Go to vercel.com → Add New Project → import `davedecena01/WhereToStayInJapan`
2. Root Directory: `frontend`
3. Build settings are auto-read from `frontend/vercel.json`
4. After deploy, note the Vercel URL → add it to Railway `CORS__ALLOWEDORIGINS__0`

**Supabase inactivity workaround:**
- Go to cron-job.org → create free cron job
- URL: `https://{railway-url}/api/health`, interval: every 6 days
- This prevents Supabase free tier from pausing the database

### 3. Railway environment variables to set

```
CONNECTIONSTRINGS__DEFAULTCONNECTION=Host=db.{ref}.supabase.co;Port=5432;Database=postgres;Username=postgres;Password={pw};SSL Mode=Require;Trust Server Certificate=true
AI__MODE=production
AI__PROVIDER=gemini
AI__APIKEY=your-gemini-api-key
AI__GEMINIMODEL=gemini-1.5-flash
HOTELS__PROVIDER=rakuten
HOTELS__APIKEY=your-rakuten-app-id
HOTELS__SEARCHRADIUSKM=2
HOTELS__MINREVIEWRATING=3.5
MAPS__GEOCODEPROVIDER=nominatim
MAPS__ROUTINGPROVIDER=osrm
MAPS__NOMINATIMUSERAGENT=WhereToStayInJapan/1.0 (your-email@example.com)
CORS__ALLOWEDORIGINS__0=https://your-vercel-app.vercel.app
ASPNETCORE_ENVIRONMENT=Production
```

**Note on connection string:** Use port 5432 (direct), NOT 6543 (Supavisor/PgBouncer). The Npgsql 10.0.1 + PgBouncer write bug requires the direct connection to avoid `ObjectDisposedException` on writes.

---

## Resume Prompt

Use this verbatim to continue in a new Claude session:

---

> I'm building a portfolio Angular + .NET 10 app called "Where To Stay In Japan". Phase 6 (deployment) is in progress. Please read `docs/session-resume.md` first to understand exactly where we left off, then continue from the top of the **Pending Tasks** list.
>
> Key context:
> - Branch: `feature/phase-6-deployment` (local only, not yet pushed — has Dockerfile + vercel.json uncommitted)
> - Phases 0–5 are complete and pushed. All PRs are chained: #1→#2→#3→#4 (no main branch on remote yet)
> - `dotnet test`: 32 passed | `ng build`: passing
> - `git` bash fails with `_lc: command not found` — use full path workaround: `"C:/Program Files/Git/cmd/git.exe" -C "c:/Users/My PC/source/repos/WhereToStayInJapan" <command>`
> - Next: finish Phase 6 code changes (angular.json fileReplacements, Program.cs MigrateAsync + maps config, CI prod build), commit, push, create PR #5
> - After code changes: manual Railway + Vercel deployment (see Pending Tasks section 2 + 3)
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
| **Phase 5** | Responsive polish, ARIA labels, keyboard navigation | ✅ Complete |
| **Phase 6** | Production deployment (Vercel + Railway + Supabase) | 🔄 In progress |

---

## Key Architecture Decisions

- **Stack:** Angular (standalone, signals) + .NET 10 Web API + PostgreSQL (Supabase)
- **AI:** `MockAIAdapter` in dev (`AI:Mode = "mock"`), `GeminiAdapter` in production
- **Scoring:** Deterministic scoring engine only — AI is for explanations, never ranking
- **Providers:** All external integrations behind interfaces in `Application/Interfaces/`
- **No auth in MVP:** Guest-only, localStorage session
- **No in-app booking:** Rakuten deep-link only
- **Hosting:** Vercel (frontend) + Railway (backend) + Supabase (PostgreSQL)
- **JSON naming:** Global snake_case policy (`JsonNamingPolicy.SnakeCaseLower`) in `Program.cs`

## Key Files

| File | Purpose |
|---|---|
| `src/API/Program.cs` | All DI registrations — provider selection by config key |
| `src/Application/Interfaces/` | All provider + repository interfaces |
| `src/Application/Services/RecommendationService.cs` | Recommendation orchestration |
| `src/Domain/Services/ScoringService.cs` | Pure scoring logic (weights, min-max normalization) |
| `src/Infrastructure/Seed/*.json` | Station areas, food, attractions seed data |
| `src/Infrastructure/Adapters/AI/GeminiAdapter.cs` | Gemini AI adapter |
| `src/Infrastructure/Adapters/Hotels/RakutenHotelAdapter.cs` | Rakuten hotel adapter |
| `frontend/src/environments/environment.prod.ts` | Production API URL (update after Railway deploy) |
| `frontend/vercel.json` | Vercel build + SPA rewrite config |
| `Dockerfile` | Railway backend Docker image |
| `docs/technical/deployment.md` | Full deployment reference |
