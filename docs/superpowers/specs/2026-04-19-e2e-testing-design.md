# E2E Functional Test Suite Design

**Date:** 2026-04-19
**Target:** Live Vercel deployment — `https://where-to-stay-in-japan-r03l6apz7-davedecena01s-projects.vercel.app`
**Tool:** Playwright (all layers — API + browser)
**Scope:** Full end-to-end coverage of all user flows and API contracts

---

## Approach

All tests use Playwright exclusively:
- **API tests** — `APIRequestContext` (no browser, pure HTTP)
- **UI tests** — Chromium browser driving the live Angular frontend

Tests are colocated in `frontend/e2e/` and run with a single command:
```bash
npm run test:e2e
```

---

## Folder Structure

```
frontend/e2e/
  fixtures/
    itinerary-samples/
      simple.txt          # single-city itinerary
      multi-region.txt    # Tokyo + Kyoto + Osaka
      messy.txt           # poorly formatted input
    test-file.pdf         # minimal valid PDF for upload tests
    test-file.docx        # minimal valid DOCX for upload tests
  api/
    health.spec.ts
    itinerary.spec.ts
    recommendations.spec.ts
    hotels.spec.ts
    chat.spec.ts
  ui/
    input.spec.ts
    review.spec.ts
    results.spec.ts
    hotels.spec.ts
  support/
    test-constants.ts     # BASE_URL, API_URL, known area_id seed
  playwright.config.ts
```

---

## Configuration

**`playwright.config.ts`:**
- `BASE_URL` env var, defaulting to the live Vercel URL
- `API_URL` env var, defaulting to `${BASE_URL}/api`
- Single Chromium browser project for E2E
- `retries: 1` to tolerate live network flakiness
- `timeout: 15000` per test
- HTML reporter + JSON for CI output
- Console error listener on all browser tests — fails on unhandled JS errors

**`package.json` addition:**
```json
"test:e2e": "playwright test"
```

---

## Test Data

| File | Content |
|---|---|
| `simple.txt` | `"Day 1-3: Tokyo (Shinjuku, Shibuya). Day 4: Nikko."` |
| `multi-region.txt` | `"Days 1-3: Tokyo. Days 4-6: Kyoto. Day 7: Osaka."` |
| `messy.txt` | Poorly formatted itinerary to test parser resilience |
| `test-file.pdf` | Minimal valid PDF with simple.txt content |
| `test-file.docx` | Minimal valid DOCX with simple.txt content |

A known valid `area_id` GUID is extracted from a live recommendations response during test setup and stored in `test-constants.ts` for hotel tests.

---

## API Test Scenarios

### `health.spec.ts`
- `GET /api/health` → 200, `{ status: "healthy" }`

### `itinerary.spec.ts`
- `POST /api/itinerary/parse` with valid text → 200, returns `ParsedItineraryDto`
- `POST /api/itinerary/parse` with empty string → 400
- `POST /api/itinerary/parse` with missing body → 400
- `POST /api/itinerary/parse/file` with valid `.txt` → 200, valid `ParsedItineraryDto`
- `POST /api/itinerary/parse/file` with no file → 400
- `POST /api/itinerary/parse/file` with oversized file (>10MB) → 413

### `recommendations.spec.ts`
- `POST /api/recommendations` with valid itinerary + preferences → 200, ≥1 recommendation
- `POST /api/recommendations` with multi-region itinerary → 200, multiple region recommendations
- `POST /api/recommendations` with missing body → 400 or 422

### `hotels.spec.ts`
- `GET /api/hotels?area_id=<valid-guid>` → 200, `HotelSearchResultDto`
- `GET /api/hotels?area_id=<valid-guid>&budget_tier=budget` → 200
- `GET /api/hotels?area_id=<invalid-guid>` → 400 or empty (not 500)
- `GET /api/hotels` with no `area_id` → 400

### `chat.spec.ts`
- `POST /api/chat` with valid message + itinerary → 200, non-empty reply
- `POST /api/chat` with no itinerary (first message) → 200, graceful response
- `POST /api/chat` with empty message → 400 or graceful fallback

---

## UI Browser Test Scenarios

### `input.spec.ts` — Itinerary Input Page
- Page loads — heading and textarea visible
- Submit with empty input → button disabled, no navigation
- Submit with invalid check-out ≤ check-in → inline date error shown
- Unsupported file type (`.jpg`) → error message shown
- Clear file → file removed, textarea re-enabled
- Drag-and-drop `.txt` file → file accepted, textarea disabled
- Paste valid text + preferences → submits, navigates to `/review`
- Upload valid `.txt` file → submits, navigates to `/review`
- Budget tier selector → all 3 options selectable
- Atmosphere toggles → multi-select works, deselect works

### `review.spec.ts` — Parse Review + Chat
- Direct navigation to `/review` with no itinerary → redirects to `/`
- After valid parse: destinations displayed, dates shown
- Ambiguous destinations flagged visually
- Multi-region warning shown when applicable
- Chat input visible; sending message → response in thread
- Confirm action → navigates to `/results`
- Back navigation → returns to `/`

### `results.spec.ts` — Recommendation Results
- Direct navigation to `/results` with no data → redirects to `/`
- At least 1 recommendation card shown
- Each card shows: area name, station, score, pros/cons, explanation
- Multi-region itinerary → multiple region groups shown
- "View Hotels" → navigates to `/hotels/:areaId`
- "Start Over" → clears state, navigates to `/`
- Retry button shown on error state

### `hotels.spec.ts` — Hotel List + Deep-link
- `/hotels/:areaId` loads hotel results
- Hotel cards show: name, price, rating, distance to station
- "Book Now" link → opens external URL in new tab
- Pagination controls work (if results exceed page size)
- No results state shown gracefully (not a crash)
- Back to results → returns to `/results` with state intact

---

## Error & Edge Case Strategy

- **5xx from live API** → UI shows error message, no blank screen, no JS crash
- **Empty API responses** → graceful empty state rendered
- **Unhandled JS errors** → `page.on('console')` listener captures and fails the test
- **Network timeouts** → Playwright `timeout: 15000` per test, `retries: 1`

---

## CI Integration (Optional)

```yaml
- run: npx playwright install --with-deps chromium
- run: npm run test:e2e
  env:
    BASE_URL: https://where-to-stay-in-japan-r03l6apz7-davedecena01s-projects.vercel.app
```

---

## Test Count Estimate

| Layer | Files | Estimated Tests |
|---|---|---|
| API | 5 spec files | ~18 tests |
| UI | 4 spec files | ~22 tests |
| **Total** | **9 spec files** | **~40 tests** |
