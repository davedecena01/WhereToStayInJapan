# Project Specification — Where To Stay In Japan

## Problem Statement

International tourists planning a Japan trip face a recurring, concrete problem: their itinerary spans multiple neighborhoods, cities, or regions, and choosing where to stay is not obvious. Picking the wrong base area means hours of extra transit per day. Most tools (Google Maps, booking sites) show you hotels but don't reason about your itinerary. This app closes that gap.

---

## Target User

**Primary:** English-speaking international tourist, planning a 5–14 day Japan trip.

**Characteristics:**
- Has a list of places they want to visit (temples, neighborhoods, attractions, food spots)
- Has a rough sense of travel dates and budget
- Does not know Japan's transit network well enough to self-optimize lodging location
- Will book on an existing platform (Rakuten, Booking.com) — not comfortable with novel booking flows
- May be traveling solo, as a couple, or in a small group (2–4 people)

**Out of scope for V1:** Japanese domestic tourists, business travelers, groups >10.

---

## MVP Scope

### IN — V1

| Feature | Notes |
|---|---|
| Itinerary upload (PDF, DOCX, TXT) | Max 10MB per file |
| Itinerary paste (plain text) | Max 50KB |
| AI-assisted itinerary parsing | Extracts destinations, dates, activity types |
| Parsed itinerary review/confirmation step | User can see and verify what was extracted |
| AI chat refinement | User can clarify or correct the parsed itinerary conversationally |
| 3–5 ranked lodging area recommendations | Scored by travel efficiency, cost, station proximity |
| Recommendation explanations | Human-readable reasoning per recommendation |
| Multi-region detection and warning | Warns user when itinerary spans far-apart regions |
| Nearby food suggestions (5+) per area | Mix of curated and AI-generated |
| Nearby attractions per area | Curated from seed data + AI supplemented |
| Hotel search per recommended area | Filtered by budget, rating, distance to station |
| Deep-link to Rakuten Travel for booking | Opens in new tab; no in-app booking |
| Hotel click tracking | Logged anonymously for analytics |
| Guest session (localStorage) | 30-day TTL; session restore on return visit |
| Japan/sakura visual theme | Desktop-first, responsive to tablet and mobile |

### OUT — V1 (with reasoning)

| Feature | Reason excluded |
|---|---|
| In-app hotel booking / payment | Significant legal/financial complexity; booking platforms already do this well |
| User accounts / authentication | Not needed for core value; adds auth complexity; deferred to Phase 2 |
| Admin content management panel | Seed data + SQL updates sufficient for MVP scale |
| Multi-language (Japanese) | English-first for portfolio demo; significant i18n effort |
| Interactive map | Adds dependency (Leaflet) and scope; static visuals sufficient for V1 |
| SEO / server-side rendering | Not a V1 priority; design SSR-compatible but skip for now |
| iOS/Android native app | Web-first; no native app in V1 |
| Advanced analytics / dashboards | Basic logging only; no reporting UI |
| Itinerary sharing (shareable URL) | Phase 2; requires server-side session persistence |

### FUTURE — Phase 2+

User accounts, saved itineraries, shareable URLs, admin panel, interactive maps, SEO pages, second hotel provider, affiliate tracking, multi-language.

---

## User Stories

**As a tourist planning my Japan trip, I want to:**

1. **Upload or paste my itinerary** so that I don't have to manually re-enter all my planned destinations.

2. **See a structured summary of what the app understood from my itinerary** so that I can confirm it extracted the right places before getting recommendations.

3. **Correct or clarify my itinerary via chat** so that if the AI misunderstood something, I can fix it without starting over.

4. **Receive at least 3 recommended base areas to stay** so that I have real options to compare, not just one suggestion.

5. **Understand why each area was recommended** so that I trust the recommendation rather than just accepting it blindly.

6. **See estimated travel times from each recommended area to my itinerary destinations** so that I can validate the recommendation makes sense for my actual plans.

7. **See pros and cons of each recommended area** so that I can factor in non-travel-time considerations (atmosphere, nightlife, family-friendly, etc.).

8. **Browse food suggestions near each recommended area** so that I can factor in dining into my lodging decision.

9. **Browse hotels in each recommended area for my travel dates** so that I can assess whether suitable accommodation exists before committing to an area.

10. **Click through to Rakuten Travel to book a hotel** so that I can complete booking on a trusted platform without having to search for it myself.

11. **Have my session automatically saved** so that I can close the browser and return to my recommendation without starting over.

12. **Be warned if my itinerary spans multiple far-apart regions** so that I know a single base might not be efficient and consider multiple lodging bases.

---

## Non-Functional Requirements

| Requirement | Target |
|---|---|
| Recommendation response time (deterministic scoring only) | < 2 seconds |
| Full recommendation response (including AI explanation + hotels) | < 8 seconds total (AI and hotels can load async) |
| Itinerary parsing response time | < 5 seconds |
| Mobile responsiveness | Functional at 375px width minimum |
| Accessibility | WCAG 2.1 AA target (keyboard nav, screen reader labels, color contrast) |
| Browser support | Chrome, Firefox, Safari — latest 2 versions |
| Uptime (portfolio/demo) | Best-effort; free-tier hosting acknowledged |
| Data privacy | No PII sent to server; session data stays in localStorage |

---

## Phase Boundary Table

| Capability | MVP | Phase 2 | Phase 3 |
|---|---|---|---|
| Itinerary upload/paste | ✅ | — | — |
| AI parsing + confirmation | ✅ | — | — |
| Area recommendations (3–5) | ✅ | — | — |
| Hotel search + deep-link | ✅ | — | — |
| Food + attraction suggestions | ✅ | — | — |
| Guest session (localStorage) | ✅ | — | — |
| Sakura UI theme | ✅ | — | — |
| User accounts (email magic link) | — | ✅ | — |
| Server-side saved itineraries | — | ✅ | — |
| Shareable itinerary URLs | — | ✅ | — |
| Interactive map (Leaflet) | — | ✅ | — |
| Admin content panel | — | ✅ | — |
| SEO / SSR pages | — | ✅ | — |
| Affiliate link tracking | — | — | ✅ |
| Multi-language (EN + JP) | — | — | ✅ |
| Second hotel provider | — | — | ✅ |
| Blog / content pages | — | — | ✅ |
| Premium AI provider option | — | — | ✅ |

---

## MVP Success Criteria

The MVP is "done" when:

1. A user can paste or upload a real Japan itinerary (at least 5 destinations) and receive 3+ ranked area recommendations.
2. Each recommendation shows: area name, nearest station, explanation, estimated travel times, pros/cons, food suggestions, and at least 3 hotels.
3. Clicking a hotel opens Rakuten Travel in a new tab with the correct hotel pre-selected (or correct search context).
4. The session is saved to localStorage and restored on return visit.
5. The app is deployed and accessible via a public URL (Vercel + Render/Railway + Supabase).
6. The app degrades gracefully when AI or hotel API is unavailable — recommendations still show with appropriate notices.
7. The UI is usable on desktop (1440px) and functional on mobile (375px).

---

## Constraints

- **Stack:** Angular 17+ (standalone, signals) + .NET 8 Web API + PostgreSQL. No strong reason to change.
- **Hosting budget:** Free tier only for MVP. Acknowledge limitations honestly (cold starts, inactivity pauses).
- **AI budget:** Gemini Flash free tier (1M tokens/day). Must have deterministic fallback for AI-off operation.
- **Hotel API:** Rakuten Travel API (free for developers). Affiliate account needed for affiliate links (can demo with mock if not yet approved).
- **Maps:** OpenStreetMap/Nominatim + OSRM. Google Maps is a paid swap-in for Phase 2+.
- **Portfolio goal:** Code quality and architecture matter. No throwaway demo shortcuts.
