# Risks and Open Questions — Where To Stay In Japan

---

## Risks

### Infrastructure Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Supabase free tier pauses DB after 1 week inactivity | **High** | **High** | Set up cron-job.org ping to `/api/health` every 6 days. Alternatively switch to Neon.tech (no inactivity pause). |
| Railway.app $5 credit exhausted | **Medium** | **Medium** | ~500 hours/month; sufficient for demo. If exceeded: $0.000463/vCPU-hr additional. Budget: <$5/month for low traffic. |
| Render.com cold start 30s (if used as fallback) | **High** | **Medium** | Prefer Railway (no sleep). If on Render: use cron-job.org to ping `/api/health` every 10 min. |
| Vercel free tier bandwidth limit (100GB/month) | **Low** | **Low** | Static SPA with ~100KB bundle; 100GB = millions of loads. Not a concern for MVP. |

### AI Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Gemini Flash free tier 429 rate limit under burst traffic | **Medium** | **Medium** | AI response cache (24hr TTL) reduces repeat calls. `RulesOnlyAdapter` fallback ensures recommendations still return. |
| Gemini API returns invalid JSON (schema mismatch) | **Medium** | **Low** | Retry once on parse failure. Fall through to `RulesOnlyAdapter` after 2 failures. Never crash the endpoint. |
| Gemini free tier discontinued or changed | **Low** | **High** | `IAIProvider` abstraction: swap to OpenAI or Anthropic with one new adapter + config change. |
| AI hallucinates non-existent Japan destinations | **Medium** | **High** | Post-processing validation: geocode all AI-returned destinations; flag those that fail geocoding as `clarification_needed`. Never surface a fake destination in the UI without warning. |
| "Gemini 1.5 Flash" model ID changes or is deprecated | **Medium** | **Low** | Model ID is in config (`AI:ModelId`). Update without code change. |

### Maps / Routing Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Nominatim rate limit (1 req/sec) slows recommendation scoring | **Medium** | **Medium** | 90-day geocode cache eliminates repeat calls. Seed script pre-geocodes all known station areas. First-time geocode of new destinations: 5 destinations = 5s max — within the 8s total target. |
| OSRM public demo server unreliable or down | **Medium** | **Medium** | `SeededFallbackRoutingProvider` returns precomputed cache. Seed script warms up routing cache at deploy. Cold start with no cache: travel times marked as "estimated" — scoring uses neutral value. |
| OSRM driving time × 1.3 transit approximation is inaccurate for some routes | **High** | **Low** | Documented assumption in recommendation UI ("Estimated travel time — may vary by transit"). Phase 2 option: hardcode known inter-city transit times from seed data. |
| Nominatim returns wrong location for ambiguous Japan place names | **Medium** | **Medium** | Add `countrycodes=jp` to all queries. Provide `cityHint` when available. Log all geocoding results for review during testing. |

### Hotel Integration Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Rakuten Travel API approval not received before demo | **Medium** | **Medium** | `MockHotelAdapter` provides full hotel UI with placeholder data. Set `Hotels:Provider = "mock"` and demo proceeds normally. |
| Rakuten Travel API changes response format | **Low** | **Medium** | `RakutenHotelAdapter` contains all Rakuten-specific mapping. Fix is isolated to one adapter class. |
| Rakuten deep links change format | **Low** | **Medium** | Deep link construction is in `BuildDeepLink()` method. Single place to fix. |
| Rakuten rate limits (undocumented for free tier) | **Unknown** | **Medium** | 30-minute hotel search cache dramatically reduces API calls. If rate-limited: increase cache TTL to 4 hours. |

### Data / Content Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Seeded station area data is incomplete (user's itinerary in uncovered region) | **Medium** | **Medium** | `NO_COVERAGE` error returned with clear message. Phase 2: expand seed to 50+ areas. V1: document coverage (Tokyo, Osaka, Kyoto, Nara, Hiroshima). |
| Curated food/attraction data is outdated (restaurant closed, etc.) | **Medium** | **Low** | V1: admin updates seed JSON and redeploys. Phase 2: admin panel for live updates. Data labeled with `source: 'admin'` so users know it's curated. |
| PDF with images only (no selectable text) | **Medium** | **Medium** | `PdfPig` returns empty string for image-only PDFs. `TextExtractionException` → 422 response with message "unable to extract text — try pasting your itinerary instead." |
| DOCX with complex formatting loses structure | **Low** | **Low** | `DocumentFormat.OpenXml` extracts paragraph text only — complex tables or layouts may lose structure. AI normalization handles messy text reasonably well. |

### Architecture Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Angular signals API changes (still relatively new in Angular 17) | **Low** | **Low** | Signals are stable as of Angular 17.3. Pattern is well-established. Risk is very low. |
| EF Core connection pool exhaustion on Supabase free tier (60 max) | **Medium** | **High** | Use Supabase PgBouncer (port 6543, transaction mode). Npgsql connection pooling defaults are fine for single-instance. |
| Single Railway instance + `MigrateAsync()` at startup — if 2 instances ever run simultaneously | **Low** | **High** | EF Core `MigrateAsync()` is safe for concurrent calls (uses locking). Only becomes a risk if manually scaled to 2+ instances — not applicable for free tier single instance. |

---

## Open Questions

Decisions deliberately deferred; must be resolved before or during implementation.

### 1. Transit Time Accuracy

**Problem:** OSRM only provides driving routes. Japan's transit system (trains, subways, buses) does not map to driving. The ×1.3 multiplier is a rough approximation that works reasonably for urban areas but is inaccurate for cross-city routes.

**Options:**
- **(A) ×1.3 driving multiplier (V1 default):** Simple, no extra API, acknowledged as approximate.
- **(B) Hardcode known inter-city transit times in seed data:** More accurate for seeded areas, zero API dependency. E.g., `Tokyo Station → Kyoto Station = 142 min (Shinkansen)`.
- **(C) Use Google Directions API with `mode=transit`:** Most accurate, but requires paid Google Maps API key.

**Recommendation:** Option (A) for V1 with Option (B) as a seed-time enhancement. Document in UI: "Estimated travel time (approximate — based on driving distance)." Phase 2: investigate cost of Option (C) for users willing to pay.

**Action required:** Before Phase 2 recommendation launch, add a `transit_time_mins` column to `station_areas` or a separate `transit_times` table with hardcoded known times for the 15 seeded areas.

---

### 2. Rakuten API Affiliate Account Setup Timeline

**Problem:** Rakuten's affiliate program requires approval, which may take days to weeks. Without it, deep links work but do not generate affiliate revenue.

**Resolution:** V1 does not require affiliate approval — direct hotel URLs work fine for portfolio demo. Register for the affiliate program in parallel with development. Set `Hotels:AffiliateId` when approved; `BuildDeepLink()` will automatically use affiliate URLs.

**Action required:** Register at `https://affiliate.rakuten.co.jp/` as early as possible to avoid delays if revenue is a near-term goal.

---

### 3. Angular SSR Compatibility

**Problem:** SSR is not in scope for V1, but Phase 3 needs it for SEO. Decisions made now can make Phase 3 migration easy or hard.

**Resolution (already incorporated into design):**
- All `localStorage`, `window`, and `document` access is isolated in `SessionService` behind `isPlatformBrowser()` guards
- Components do not access browser APIs directly
- All HTTP calls go through `ApiService` which is platform-neutral
- Adding `@angular/ssr` in Phase 3 should be straightforward

**Action required:** During Phase 5 (polish), do a quick audit: search for any direct `window.` or `document.` usage in components. Wrap any found in a service + `isPlatformBrowser` guard.

---

### 4. AI Chat Scope for V1

**Problem:** Full chat refinement (editing itinerary via chat) is complex. It requires the AI to understand the current itinerary state, suggest diffs, and the frontend to accept/reject changes. This is non-trivial.

**Options:**
- **(A) Full chat:** User can refine itinerary conversationally. AI returns `suggested_itinerary_update`. Frontend shows diff and "Accept" button.
- **(B) Read-only Q&A chat:** User can ask questions ("How far is Nara from Kyoto?", "Is Shinjuku good for families?"). AI answers in prose. No itinerary editing from chat.

**Recommendation:** Implement (A) for V1 if time allows — it's a strong portfolio differentiator. Fall back to (B) if timeline is tight. The `POST /api/chat` endpoint already returns `suggested_itinerary_update` (nullable). The difference is purely in the frontend `ItineraryChatComponent` complexity.

**Action required:** Decide during Phase 3 based on remaining time. If falling back to (B), simply don't render the "Accept update" button when `suggested_itinerary_update` is null.

---

### 5. Multi-Language Strategy

**Problem:** Japanese travelers cannot use the app (all UI text is English). Place names in the database are English. This limits the potential audience but adds significant i18n complexity.

**Resolution:** English-only for MVP. Store place names in both English (`area_name`) and Japanese (`area_name_ja` — add column in Phase 3). The data model is designed to accommodate this without breaking changes.

**Action required:** When writing seed data, include Japanese names in the JSON files as a `name_ja` field even if not surfaced in V1. This avoids a data backfill in Phase 3.

---

## Recommended Architecture Decision (Final Summary)

### Primary Recommendation

**Angular 17 (standalone, signals) + .NET 8 Web API + PostgreSQL (Supabase/Neon) + Gemini Flash AI + OpenStreetMap/Nominatim + OSRM + Rakuten Travel API + Vercel (frontend) + Railway.app (backend)**

**Why this is the right choice:**
1. Angular + .NET 8 aligns with the developer's stated stack preference and provides portfolio value demonstrating full-stack competency in a commercially relevant stack.
2. Provider abstraction throughout (AI, Maps, Hotels, Cache) means no vendor lock-in and makes the architecture swappable, demonstrable, and testable without live API keys.
3. PostgreSQL consolidates all data (relational, cache, logs) in one system — no Redis, no secondary stores. Supabase/Neon provides free-tier hosting.
4. Gemini Flash free tier is genuinely viable for a portfolio demo. The deterministic fallback (RulesOnlyAdapter) means the core product works without any AI dependency.
5. Free-tier hosting stack (Vercel + Railway + Supabase) has real limitations (Supabase inactivity pause, Railway credit limits) but is honest and well-documented here.

### Alternative: Next.js 14 (App Router) + .NET 8 API

**When to consider:** If SEO is a V1 requirement, or if the developer decides Angular is not the best fit.

**Why not recommended for V1:**
- Angular is the developer's stated preference.
- The app is interaction-heavy (wizard-style flow), not content-heavy — SSR provides minimal SEO benefit for V1.
- Next.js App Router has its own learning curve and deployment complexity (RSC, server actions).
- The Angular app is designed SSR-compatible from day 1; migrating to `@angular/ssr` in Phase 3 is lower risk than switching frameworks now.

**If chosen:** Replace Angular frontend with Next.js. Keep .NET 8 API unchanged. Same backend architecture, same API contracts, same database.
