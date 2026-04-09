# User Flows — Where To Stay In Japan

---

## Primary Happy Path

### Flow: New User — Complete Recommendation Journey

```
[1] Homepage
    User lands on app.
    UI state: Preferences form + itinerary input area visible.
    Components: ItineraryInputComponent, PreferencesFormComponent

    User fills in:
      - Travel dates (check-in / check-out)
      - Number of travelers
      - Budget tier (budget / mid / luxury)
      - Hotel type preferences (e.g., business hotel, ryokan, capsule)
      - Optional: avoid long walking (checkbox)
      - Optional: must-be-near station (text input)
      - Optional: preferred atmosphere (multi-select: nightlife / family-friendly / quiet / shopping)

    ↓

[2] Itinerary Input
    User either:
      (a) Drags and drops a PDF/DOCX/TXT file onto the upload zone, OR
      (b) Clicks "browse" to select a file, OR
      (c) Pastes itinerary text into the text area

    UI state: File name + size shown after upload, or text area with character count.
    User clicks "Parse My Itinerary".

    ↓

[3] Parsing (Loading State)
    POST /api/itinerary/parse fires.
    UI state: Skeleton loader / spinner over the confirmation area.
    Text: "Reading your itinerary..."

    ↓

[4] Itinerary Confirmation Step
    UI state: Structured list of extracted destinations, grouped by day (if dates present)
    or as a flat list (if no dates).

    Displayed per destination:
      - Destination name (normalized)
      - City/region label
      - Day number or "Unscheduled"

    If parsing_confidence = 'low': yellow warning banner:
      "We're not 100% sure about some destinations. Review carefully or use
       the chat below to clarify."

    If clarification_needed = true: chat window auto-opens with prompt:
      "I noticed some destinations were unclear — want me to help sort them out?"

    User reviews → clicks "Looks Good" to proceed.
    (Optional: user edits/removes destinations inline — Phase 2 feature; V1 = read-only review.)

    ↓

[5] AI Chat Refinement (Optional)
    User can type in chat: "Actually day 3 is in Nara, not Osaka"
    AI responds, suggests updated ParsedItinerary.
    User accepts or ignores suggestion.
    If accepted: itinerary state updates, review step refreshes.

    User clicks "Get Recommendations".

    ↓

[6] Recommendations Loading
    POST /api/recommendations fires with ParsedItinerary + UserPreferences.
    UI state: 3 skeleton recommendation cards.
    Text: "Finding the best areas for your trip..."

    Deterministic scoring completes first (~1–2s).
    Hotel previews and AI explanations load asynchronously and fill in as they arrive.

    ↓

[7] Recommendation Results
    UI state: 3–5 ranked recommendation cards, sorted by score descending.

    Each card shows:
      - Rank badge (1st, 2nd, 3rd...)
      - Area name + nearest station
      - City / region
      - Score visualization (bar or stars)
      - Brief explanation (1–2 sentences from AI)
      - Travel time summary: list of destinations + estimated minutes
      - Pros (2–3 bullet points)
      - Cons (1–2 bullet points)
      - 3 hotel previews (name, price, rating, thumbnail)
      - "See more hotels →" link

    ↓

[8] Recommendation Detail (Expanded)
    User clicks a recommendation card (or "See more hotels →").
    Navigates to /area/{area_id} route.

    UI state: Full recommendation detail page.
    Sections:
      - Area overview + explanation
      - Travel time table (all destinations + minutes)
      - Hotel list (paginated, 10 per page) — fetched via GET /api/hotels
      - Food suggestions (5+ items with name, cuisine, address)
      - Nearby attractions (categorized list)

    ↓

[9] Hotel Click → External Booking
    User clicks "Book on Rakuten" button on a hotel card.
    Browser opens Rakuten Travel in a new tab (target="_blank", rel="noopener").
    POST /api/analytics/hotel-click fires (fire-and-forget).

    ↓

[10] Session Auto-Save
    Throughout steps 4–9, session is written to localStorage after each significant state change:
    - After itinerary confirmed (step 4)
    - After recommendations received (step 7)
    - After hotel results loaded (step 8)

    Session key: `wtsjp_session`
    TTL: 30 days from last_updated timestamp.
```

---

## Edge Case Flows

### Multi-Region Itinerary

```
After step [4] — Itinerary Confirmation:

  System detects destinations span > 1 region AND regions are > 100km apart.
  (e.g., Tokyo + Kyoto + Osaka in same itinerary)

  UI inserts yellow info banner above recommendation section:
    "Your itinerary spans multiple regions (Tokyo · Kansai).
     We'll recommend a base area for each region.
     Staying in just one place would mean very long travel days."

  Recommendations are grouped by region:
    --- Tokyo area (Days 1–4) ---
    [Card: Shinjuku] [Card: Shibuya] [Card: Asakusa]

    --- Kansai area (Days 5–9) ---
    [Card: Namba, Osaka] [Card: Gion, Kyoto]

  Each group functions identically to the standard recommendation flow.
```

### AI Unavailable

```
POST /api/itinerary/parse — AI call fails, fallback to RulesOnlyAdapter.

  Parsing response includes:
    parsing_confidence: 'low'
    clarification_needed: true

  UI shows:
    Orange info banner: "AI assistance is currently unavailable.
    We extracted destinations using basic text parsing.
    Please review carefully — some destinations may be missing or misread."

POST /api/recommendations — AI explanation generation fails.

  Response includes recommendations without `explanation` field (null).

  UI shows per card:
    (explanation section hidden, not "null")
    Small label: "Explanation unavailable — AI is temporarily offline."

  Scoring, travel times, and hotel results are unaffected (fully deterministic).
```

### Hotel API Unavailable

```
POST /api/recommendations — hotel preview fetch fails.
GET /api/hotels — Rakuten API returns error.

  Recommendations display normally (area, station, explanation, travel times, food, attractions).
  Hotel section shows:
    Empty state with icon + text:
    "Hotel results are temporarily unavailable. Try refreshing, or search
     directly on Rakuten Travel: [link]"

  "See more hotels →" link replaced with direct Rakuten search link:
    https://travel.rakuten.co.jp/hotelSearch/?f_area={area_name}

  HTTP response: 206 Partial Content with flag `hotels_available: false`.
```

### No Hotels Found in Area

```
GET /api/hotels returns empty array (valid API response, zero results for filters).

  Hotel section shows:
    "No hotels found matching your filters in this area.
     Try adjusting your budget range or dates."

  "Expand search" button: relaxes radius filter from 1km to 3km and re-queries.
  If still empty: show Rakuten direct search link.
```

### Itinerary Too Vague to Parse

```
POST /api/itinerary/parse — AI returns destinations: [] or all destinations fail geocoding.

  UI shows:
    Red error banner:
    "We couldn't identify specific destinations in your itinerary.
     Try pasting more specific place names (e.g., 'Senso-ji Temple' instead of 'visit a temple')."

  Chat window auto-opens with prompt:
    "Your itinerary was a bit hard to read. Can you tell me which cities and
     specific places you're planning to visit?"

  User can type in chat to provide clarification.
  AI returns a structured ParsedItinerary from the chat response.
  "Get Recommendations" button remains disabled until at least 1 destination is confirmed.
```

### Returning User with Saved Session

```
User revisits app with valid session in localStorage (not expired).

  On app init, SessionService.loadSession() detects existing session.

  UI shows top-of-page restoration banner:
    "Welcome back! You have a saved trip from [relative date].
     [Continue your session] [Start fresh]"

  If user clicks "Continue":
    → App restores itinerary + preferences + recommendations from localStorage
    → Skips directly to step [7] Recommendation Results
    → Hotels re-fetched live (cached results may be stale)

  If user clicks "Start fresh":
    → SessionService.clearSession() is called
    → Banner dismissed, fresh state, user starts at step [1]

  If session is expired (> 30 days):
    → SessionService.loadSession() detects expiry, calls clearSession()
    → No banner shown; fresh start
```

### File Upload Failure

```
User uploads a file that cannot be parsed (e.g., image-only PDF, corrupted DOCX).

  POST /api/itinerary/parse returns 422 Unprocessable Entity.

  UI shows:
    Red inline error under upload zone:
    "We couldn't extract text from this file. It may be image-only or corrupted.
     Try copying and pasting your itinerary text instead."

  Upload zone resets to initial state.
  Text paste area is highlighted/focused as alternative.
```

### File Too Large

```
User uploads a file > 10MB.

  Client-side validation fires before upload:
  "File is too large (X MB). Maximum size is 10 MB."

  Upload is blocked. No API call is made.
```

---

## Guest Session Flow (Detail)

```
FIRST VISIT
  App loads → SessionService.loadSession() → no key found → fresh state
  session_id generated (UUID v4) → written to session on first state save

STATE SAVE TRIGGERS (writes to localStorage key: wtsjp_session)
  1. Itinerary confirmed (step 4): saves itinerary + preferences
  2. Recommendations received (step 7): saves recommendations
  3. Hotel results loaded for an area: saves hotel_results

SESSION STRUCTURE
  {
    version: '1.0',
    session_id: 'uuid-v4',
    created_at: '2025-04-10T09:00:00Z',
    expires_at: '2025-05-10T09:00:00Z',   // +30 days from created_at
    last_updated: '2025-04-10T11:30:00Z',
    itinerary: { ... ParsedItinerary ... },
    preferences: { ... UserPreferences ... },
    recommendations: [ ... RecommendationResult[] ... ],
  }

RETURN VISIT
  App loads → SessionService.loadSession()
  → expires_at > now? → restore session → show banner
  → expires_at <= now? → clearSession() → fresh start

CLEAR TRIGGERS
  - User clicks "Start fresh" on banner
  - User explicitly starts a new itinerary (confirmation dialog: "Start over? Your saved session will be lost.")
  - Session expired on load
```

---

## Flow Diagrams (ASCII)

### Primary Flow

```
Homepage
   │
   ▼
[Enter Preferences] ──────────────────────────────────┐
   │                                                   │
   ▼                                              (return visit)
[Upload / Paste Itinerary]               [Session Restore Banner]
   │                                                   │
   ▼                                                   │
[POST /api/itinerary/parse]                            │
   │                                                   │
   ├── success ──► [Review Confirmation Step]          │
   │                       │                           │
   │               (optional chat)                     │
   │                       │                           │
   │               [POST /api/recommendations]  ◄──────┘
   │                       │
   │               ┌───────┴──────────────────────────┐
   │           scoring                             AI + hotels
   │           (sync, ~1s)                         (async, ~3-7s)
   │               │
   │               ▼
   │          [Recommendation Cards]
   │                   │
   │            (click card)
   │                   │
   │                   ▼
   │          [Area Detail Page]
   │                   │
   │            (click hotel)
   │                   │
   │                   ▼
   │          [Rakuten Travel (new tab)]
   │
   └── 422/AI fail ──► [Error Banner + Paste Suggestion]
```

### Multi-Region Split

```
ParsedItinerary
   │
   ▼
[Region Detection]
   │
   ├── single region ──► standard recommendation flow
   │
   └── multiple regions (dist > 100km)
            │
            ▼
       [Warning Banner]
            │
            ▼
       [Recommendations grouped by region]
            │
         per region
            │
            ▼
       [Standard recommendation cards]
```
