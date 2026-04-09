# Phased Roadmap — Where To Stay In Japan

---

## MVP (Phase 1) — Current Target

**Theme:** Core recommendation value. Fully functional without user accounts.

### In Scope

| Feature | Implementation Notes |
|---|---|
| Itinerary upload (PDF, DOCX, TXT) | PdfPig + OpenXml, 10MB limit |
| Itinerary paste (plain text) | 50KB limit |
| AI-assisted itinerary parsing | GeminiAdapter with RulesOnlyAdapter fallback |
| Parsed itinerary review/confirmation step | Read-only structured list |
| AI chat refinement | POST /api/chat; Gemini with fallback message |
| 3–5 ranked area recommendations | Deterministic scoring (travel time, cost, station proximity, food, shopping) |
| Recommendation explanations | AI-generated, null if AI unavailable |
| Multi-region detection + warning | Haversine distance > 100km |
| Food suggestions (5+ per area) | Curated-first + AI supplemented |
| Nearby attractions | Curated from seed data |
| Hotel search + deep-link | Rakuten Travel API + direct booking link |
| Hotel click tracking | Anonymous, fire-and-forget |
| Guest session (localStorage) | 30-day TTL, restore on return |
| Japan/sakura visual theme | Custom SCSS, Noto Sans JP |
| Desktop-first responsive | 1440px primary, functional at 375px |

### Out of Scope (MVP)

| Feature | Reason |
|---|---|
| User accounts / auth | Not needed for core value; adds significant complexity |
| In-app hotel booking | Legal/payment complexity; external booking is better UX anyway |
| Payment processing | Out of scope for a recommendation app |
| Admin content panel | SQL seed updates sufficient at MVP scale |
| Interactive map | Adds Leaflet dependency and scope without proportional value |
| SEO / server-side rendering | Not portfolio-critical for V1; design SSR-compatible |
| Multi-language | English-first; Japanese translation is Phase 3 |
| iOS/Android native app | Web-first strategy |
| Itinerary sharing | Requires server-side session persistence (Phase 2) |

### Infrastructure

| Component | Service | Notes |
|---|---|---|
| Frontend | Vercel (free) | |
| Backend | Railway.app ($5/mo credit) | |
| Database | Supabase (free tier) | 500MB; ping to prevent pause |
| AI | Gemini Flash 1.5 (free tier) | 1M tokens/day |
| Maps | Nominatim + OSRM (free) | OSM public APIs |
| Hotels | Rakuten Travel API (free for devs) | |

### MVP Definition of Done

1. User submits a real Japan itinerary (5+ destinations) → receives 3+ recommendations
2. Each recommendation shows station, explanation, travel times, food, hotels
3. Hotel deep-link opens Rakuten with correct context
4. Session saves/restores from localStorage
5. App deployed on public URL (Vercel + Railway + Supabase)
6. App degrades gracefully when AI or hotels unavailable
7. App is usable on mobile (375px) and polished on desktop (1440px)
8. `dotnet test` passes in CI with mock adapters

---

## Phase 2 — Post-MVP Expansion

**Theme:** Persistence, sharing, better maps, and content management.
**Trigger:** MVP is live, getting real user feedback, and worth investing further.
**Estimated effort:** 4–8 developer-weeks beyond MVP

### Features

**User Authentication (Supabase Auth)**
- Email magic link login (no password, no friction)
- Session migration from localStorage to server on first login
- Protected routes: `/account`, `/saved-trips`
- Infrastructure addition: `users` table in PostgreSQL

**Saved Itineraries (Server-Side)**
- Authenticated users can save named itineraries
- `POST /api/itineraries` (create), `GET /api/itineraries` (list), `GET /api/itineraries/:id`
- New tables: `user_itineraries`, `user_recommendations`
- Frontend: saved trips list page

**Shareable Itinerary URLs**
- `GET /api/itineraries/:shortId` → returns shared itinerary
- Short ID: base62 encoded UUID (6 chars)
- Frontend: "Share this trip" button → copy link to clipboard
- No auth required to view a shared itinerary

**Interactive Map (Leaflet.js + OSM Tiles)**
- Map view on recommendation detail page
- Pins for: recommended area centroid, itinerary destinations
- OSM tile layer: `https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png`
- Attribution: `© OpenStreetMap contributors` (required)
- npm package: `leaflet` + `@types/leaflet`

**Improved Transit Routing**
- Option A: Integrate GTFS-based transit data (static, free, complex)
- Option B: Use Zenrin/HyperDia API (paid, accurate Japan transit)
- Option C: Google Directions API with `mode=transit` (paid)
- Decision: defer to Phase 2 when budget/priority is clearer

**Admin Content Panel**
- Simple CRUD for `station_areas`, `curated_food`, `curated_attractions`
- Protected by admin role (added to Supabase Auth)
- Framework: minimal Angular form pages, no external admin library
- Replaces manual SQL seed updates

**SEO-Friendly Static Pages**
- Individual pages for major Japan destinations: `/tokyo`, `/kyoto`, `/osaka`
- Angular SSR activated (Angular Universal or Angular `@angular/ssr`)
- Requires: `ng add @angular/ssr`, update `app.config.ts` for server-side rendering
- Supabase → pre-populate destination content

### Infrastructure Changes

| Component | Change |
|---|---|
| Backend | Add `users` table + `user_itineraries` + `user_recommendations` |
| Auth | Supabase Auth (built-in, no extra service) |
| Frontend | Angular SSR activated for SEO pages |
| Maps | Leaflet.js added as npm dependency |

---

## Phase 3 — Monetization and Scale

**Theme:** Revenue, audience growth, and second-tier providers.
**Trigger:** App has real users and a justification for investment.
**Estimated effort:** 6–12 developer-weeks beyond Phase 2

### Features

**Rakuten Affiliate Integration**
- Register for Rakuten Affiliate Program
- Wrap all hotel deep links in Rakuten affiliate URLs
- Track affiliate clicks in `hotel_click_logs` (already implemented)
- Estimated revenue: ¥500–¥3,000 per booking referral (Rakuten affiliate rates vary)

**Blog / Content Pages (SEO)**
- Static CMS-backed blog posts: "Best areas to stay in Tokyo", "Kyoto neighborhood guide"
- Markdown-based content or headless CMS (Contentful free tier)
- Angular routes: `/blog`, `/blog/:slug`
- Target: long-tail SEO keywords for Japan travel

**Social Login (Google)**
- Google OAuth via Supabase Auth (1 config change)
- Add `Sign in with Google` button to login page

**Multi-Language Support (English + Japanese)**
- Angular i18n (`@angular/localize`)
- Translation files: `messages.en.xlf`, `messages.ja.xlf`
- Place names already stored in English; add `name_ja` column to `station_areas`
- UI text strings extracted and translated

**Premium AI Provider (Optional)**
- Add `OpenAIAdapter` (GPT-4o) or `AnthropicAdapter` (Claude) for higher quality parsing
- Activated by user account tier (free = Gemini, premium = GPT-4o)
- IAIProvider abstraction already supports this — new adapter + DI config

**Second Hotel Provider**
- Add `BookingComAdapter : IHotelProvider`
- Show hotels from multiple providers on detail page (merged + deduped)
- Or: A/B test Rakuten vs Booking.com by region

**Analytics Dashboard**
- Internal-only page at `/admin/analytics`
- Query `recommendation_logs` + `hotel_click_logs`
- Charts: daily recommendation count, top areas recommended, hotel click rate per area

### Infrastructure Changes

| Component | Change |
|---|---|
| AI | OpenAI adapter (optional, paid) |
| Hotels | BookingCom adapter |
| Storage | `name_ja` columns in station/content tables |
| Frontend | `@angular/localize`, separate build per locale |
| Backend | Affiliate link construction, user tiers |

---

## Roadmap Summary

```
MVP (Now)
  └── Core recommendation flow
  └── Guest session
  └── Hotel deep-links (Rakuten)
  └── Sakura UI
  └── Free-tier deployment

Phase 2 (3–6 months post-MVP)
  └── User auth (email magic link)
  └── Saved itineraries
  └── Shareable URLs
  └── Interactive map
  └── Admin panel
  └── SEO pages + SSR

Phase 3 (6–12+ months)
  └── Affiliate revenue
  └── Blog content
  └── Multi-language
  └── Second hotel provider
  └── Analytics dashboard
```

---

## What Each Phase Unlocks

| Phase | Technical Unlock | Business Unlock |
|---|---|---|
| MVP | Provider abstraction, deterministic engine | Portfolio showcase, proof of concept |
| Phase 2 | Auth, server-side sessions, SEO | User retention, organic traffic |
| Phase 3 | Monetization hooks, multi-provider | Revenue, broader audience |
