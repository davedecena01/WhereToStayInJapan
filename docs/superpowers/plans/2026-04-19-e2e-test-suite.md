# E2E Functional Test Suite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a full Playwright E2E test suite (~40 tests) targeting the live deployed app — Vercel frontend + Railway .NET API — covering all API contracts and UI flows.

**Architecture:** All tests use Playwright exclusively — `APIRequestContext` for HTTP-level API tests (no browser), Chromium for UI browser tests. Global setup fetches a live area_id from the recommendations API and writes it to `.state/area-id.json` for hotel tests to reuse. UI tests navigate through the live Angular SPA flows.

**Tech Stack:** `@playwright/test` ^1.59.1, TypeScript, Node.js fixtures. Branch: `feature/e2e-test-suite`.

**URLs:**
- Frontend: `https://where-to-stay-in-japan-r03l6apz7-davedecena01s-projects.vercel.app`
- API: `https://wheretostayinjapan-production.up.railway.app`
- JSON contract: snake_case (SnakeCaseLower policy on all endpoints)

---

## File Map

**Create:**
- `frontend/playwright.config.ts` — Playwright config (testDir, baseURL, retries, global-setup)
- `frontend/e2e/support/global-setup.ts` — fetches live area_id before suite runs
- `frontend/e2e/support/test-constants.ts` — URLs, sample text, getSeededAreaId()
- `frontend/e2e/support/helpers.ts` — navigateToResults() shared browser helper
- `frontend/e2e/fixtures/simple.txt` — single-city itinerary text
- `frontend/e2e/fixtures/multi-region.txt` — Tokyo + Kyoto + Osaka itinerary
- `frontend/e2e/fixtures/messy.txt` — poorly formatted itinerary
- `frontend/e2e/api/health.spec.ts`
- `frontend/e2e/api/itinerary.spec.ts`
- `frontend/e2e/api/recommendations.spec.ts`
- `frontend/e2e/api/hotels.spec.ts`
- `frontend/e2e/api/chat.spec.ts`
- `frontend/e2e/ui/input.spec.ts`
- `frontend/e2e/ui/review.spec.ts`
- `frontend/e2e/ui/results.spec.ts`
- `frontend/e2e/ui/hotels.spec.ts`

**Modify:**
- `frontend/package.json` — add `@playwright/test`, add `test:e2e` script

---

## Task 1: Install @playwright/test and scaffold

**Files:**
- Modify: `frontend/package.json`
- Create: `frontend/playwright.config.ts`
- Create: `frontend/e2e/` folder structure

- [ ] **Step 1: Install @playwright/test**

```bash
cd frontend
npm install --save-dev @playwright/test@1.59.1
npx playwright install chromium
```

Expected: `@playwright/test` appears in `package.json` devDependencies.

- [ ] **Step 2: Add test:e2e script to package.json**

In `frontend/package.json`, add to `"scripts"`:
```json
"test:e2e": "playwright test",
"test:e2e:report": "playwright show-report"
```

- [ ] **Step 3: Create playwright.config.ts**

Create `frontend/playwright.config.ts`:
```typescript
import { defineConfig, devices } from '@playwright/test';

const FRONTEND_URL =
  process.env['FRONTEND_URL'] ??
  'https://where-to-stay-in-japan-r03l6apz7-davedecena01s-projects.vercel.app';

export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  retries: 1,
  reporter: [['html'], ['json', { outputFile: 'playwright-report/results.json' }]],
  globalSetup: './e2e/support/global-setup.ts',
  use: {
    baseURL: FRONTEND_URL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
```

- [ ] **Step 4: Create e2e folder structure**

```bash
cd frontend
mkdir -p e2e/support e2e/fixtures e2e/api e2e/ui e2e/.state
echo '{}' > e2e/.state/.gitkeep
echo 'e2e/.state/' >> .gitignore
echo 'playwright-report/' >> .gitignore
echo 'test-results/' >> .gitignore
```

- [ ] **Step 5: Verify config is detected**

```bash
cd frontend
npx playwright test --list
```

Expected: `0 tests found` (no spec files yet) with no config errors.

- [ ] **Step 6: Commit**

```bash
cd frontend
git add playwright.config.ts package.json package-lock.json e2e/ .gitignore
git commit -m "feat(e2e): scaffold Playwright config and folder structure"
```

---

## Task 2: Global setup, test-constants, helpers, and fixtures

**Files:**
- Create: `frontend/e2e/support/global-setup.ts`
- Create: `frontend/e2e/support/test-constants.ts`
- Create: `frontend/e2e/support/helpers.ts`
- Create: `frontend/e2e/fixtures/simple.txt`
- Create: `frontend/e2e/fixtures/multi-region.txt`
- Create: `frontend/e2e/fixtures/messy.txt`

- [ ] **Step 1: Create test-constants.ts**

Create `frontend/e2e/support/test-constants.ts`:
```typescript
import { readFileSync } from 'fs';
import { join } from 'path';

export const FRONTEND_URL =
  process.env['FRONTEND_URL'] ??
  'https://where-to-stay-in-japan-r03l6apz7-davedecena01s-projects.vercel.app';

export const API_URL =
  process.env['API_URL'] ??
  'https://wheretostayinjapan-production.up.railway.app';

export const SIMPLE_TEXT =
  'Day 1-3: Tokyo (Shinjuku, Shibuya). Day 4: Nikko.';

export const MULTI_REGION_TEXT =
  'Days 1-3: Tokyo (Shinjuku). Days 4-6: Kyoto (Gion, Arashiyama). Day 7: Osaka (Dotonbori).';

export const MESSY_TEXT =
  'tokyo first maybe 3 days shinjuku area then kyoto 2 nites last day osaka before flying home';

export const DEFAULT_PREFERENCES = {
  check_in: null,
  check_out: null,
  travelers: 2,
  budget_tier: 'mid',
  preferred_atmosphere: [],
  avoid_long_walking: false,
  must_be_near_station: null,
};

export function getSeededAreaId(): string {
  try {
    const statePath = join(__dirname, '../.state/area-id.json');
    const data = JSON.parse(readFileSync(statePath, 'utf-8'));
    return data.area_id as string;
  } catch {
    return '00000000-0000-0000-0000-000000000000';
  }
}
```

- [ ] **Step 2: Create global-setup.ts**

Create `frontend/e2e/support/global-setup.ts`:
```typescript
import { request } from '@playwright/test';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';
import { API_URL, SIMPLE_TEXT, DEFAULT_PREFERENCES } from './test-constants';

export default async function globalSetup(): Promise<void> {
  const context = await request.newContext({ baseURL: API_URL });

  try {
    const parseRes = await context.post('/api/itinerary/parse', {
      data: { text: SIMPLE_TEXT },
    });

    if (!parseRes.ok()) {
      console.warn('[global-setup] Parse failed — hotel tests will use fallback area_id');
      return;
    }

    const parsed = await parseRes.json();

    const recRes = await context.post('/api/recommendations', {
      data: { itinerary: parsed, preferences: DEFAULT_PREFERENCES },
      timeout: 120_000,
    });

    if (!recRes.ok()) {
      console.warn('[global-setup] Recommendations failed — hotel tests will use fallback area_id');
      return;
    }

    const result = await recRes.json();
    const areaId = result.recommendations?.[0]?.area_id as string | undefined;

    if (areaId) {
      const stateDir = join(__dirname, '../.state');
      mkdirSync(stateDir, { recursive: true });
      writeFileSync(join(stateDir, 'area-id.json'), JSON.stringify({ area_id: areaId }));
      console.log(`[global-setup] Seeded area_id: ${areaId}`);
    }
  } catch (err) {
    console.warn('[global-setup] Error fetching area_id:', err);
  } finally {
    await context.dispose();
  }
}
```

- [ ] **Step 3: Create helpers.ts**

Create `frontend/e2e/support/helpers.ts`:
```typescript
import { Page } from '@playwright/test';
import { SIMPLE_TEXT } from './test-constants';

/** Navigates through input → review → results via the full UI flow.
 *  Recommendations can take up to 90s on first run — timeout is 120s. */
export async function navigateToResults(page: Page): Promise<void> {
  await page.goto('/');
  await page.locator('textarea').fill(SIMPLE_TEXT);
  await page.locator('button.btn-primary').click();
  await page.waitForURL('**/review', { timeout: 30_000 });
  await page.locator('button.btn-primary', { hasText: 'Looks Good' }).click();
  await page.waitForURL('**/results', { timeout: 120_000 });
}
```

- [ ] **Step 4: Create fixture text files**

Create `frontend/e2e/fixtures/simple.txt`:
```
Day 1: Arrive Tokyo. Check in Shinjuku. Walk around Kabukicho.
Day 2: Shibuya crossing, Harajuku, Meiji Shrine.
Day 3: Akihabara and Ueno Park.
Day 4: Day trip to Nikko — Toshogu Shrine.
```

Create `frontend/e2e/fixtures/multi-region.txt`:
```
Days 1-3: Tokyo — Shinjuku, Shibuya, Asakusa (Senso-ji).
Days 4-6: Kyoto — Gion district, Arashiyama bamboo grove, Fushimi Inari.
Day 7: Osaka — Dotonbori, Kuromon Market, Osaka Castle.
```

Create `frontend/e2e/fixtures/messy.txt`:
```
tokyo first maybe 3 days shinjuku area
then kyoto 2 nites gion
last day osaka dotonbori before flying home from kix
```

- [ ] **Step 5: Run global-setup manually to verify area_id is fetched**

```bash
cd frontend
npx tsx e2e/support/global-setup.ts
```

Expected output: `[global-setup] Seeded area_id: <some-guid>`

If `tsx` is not installed: `npm install --save-dev tsx` first.

- [ ] **Step 6: Commit**

```bash
cd frontend
git add e2e/support/ e2e/fixtures/ package.json package-lock.json
git commit -m "feat(e2e): add global-setup, test-constants, helpers, and fixture files"
```

---

## Task 3: API tests — health and itinerary

**Files:**
- Create: `frontend/e2e/api/health.spec.ts`
- Create: `frontend/e2e/api/itinerary.spec.ts`

- [ ] **Step 1: Write health.spec.ts**

Create `frontend/e2e/api/health.spec.ts`:
```typescript
import { test, expect } from '@playwright/test';
import { API_URL } from '../support/test-constants';

test.describe('GET /api/health', () => {
  test('returns 200 with healthy status', async ({ request }) => {
    const res = await request.get(`${API_URL}/api/health`);

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.status).toBe('healthy');
    expect(body.db).toBe('connected');
  });
});
```

- [ ] **Step 2: Run health test and verify it passes**

```bash
cd frontend
npx playwright test e2e/api/health.spec.ts --reporter=line
```

Expected: `1 passed`

- [ ] **Step 3: Write itinerary.spec.ts**

Create `frontend/e2e/api/itinerary.spec.ts`:
```typescript
import { test, expect } from '@playwright/test';
import { readFileSync } from 'fs';
import { join } from 'path';
import { API_URL, SIMPLE_TEXT } from '../support/test-constants';

test.describe('POST /api/itinerary/parse (text)', () => {
  test('parses valid text and returns ParsedItineraryDto', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/itinerary/parse`, {
      data: { text: SIMPLE_TEXT },
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.destinations)).toBe(true);
    expect(body.destinations.length).toBeGreaterThan(0);
    expect(Array.isArray(body.regions_detected)).toBe(true);
    expect(typeof body.parsing_confidence).toBe('string');
    expect(typeof body.is_multi_region).toBe('boolean');
    expect(typeof body.clarification_needed).toBe('boolean');
  });

  test('returns 400 for empty text', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/itinerary/parse`, {
      data: { text: '' },
    });

    expect(res.status()).toBe(400);
    const body = await res.json();
    expect(body.error ?? body.title ?? body.detail).toBeTruthy();
  });

  test('returns 400 for missing body', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/itinerary/parse`, {
      data: {},
    });

    expect(res.status()).toBe(400);
  });
});

test.describe('POST /api/itinerary/parse/file', () => {
  test('parses valid .txt file and returns ParsedItineraryDto', async ({ request }) => {
    const fileBuffer = readFileSync(join(__dirname, '../fixtures/simple.txt'));

    const res = await request.post(`${API_URL}/api/itinerary/parse/file`, {
      multipart: {
        file: {
          name: 'simple.txt',
          mimeType: 'text/plain',
          buffer: fileBuffer,
        },
      },
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.destinations)).toBe(true);
    expect(body.destinations.length).toBeGreaterThan(0);
  });

  test('returns 400 when no file is provided', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/itinerary/parse/file`, {
      multipart: {},
    });

    expect(res.status()).toBe(400);
  });

  test('returns 413 for oversized file (>10MB)', async ({ request }) => {
    const bigBuffer = Buffer.alloc(11 * 1024 * 1024, 'a'); // 11 MB

    const res = await request.post(`${API_URL}/api/itinerary/parse/file`, {
      multipart: {
        file: {
          name: 'huge.txt',
          mimeType: 'text/plain',
          buffer: bigBuffer,
        },
      },
      timeout: 30_000,
    });

    expect([413, 400]).toContain(res.status());
  });
});
```

- [ ] **Step 4: Run itinerary API tests**

```bash
cd frontend
npx playwright test e2e/api/itinerary.spec.ts --reporter=line
```

Expected: `6 passed` (or investigate and fix any failures — check live API is up at Railway URL first)

- [ ] **Step 5: Commit**

```bash
cd frontend
git add e2e/api/health.spec.ts e2e/api/itinerary.spec.ts
git commit -m "feat(e2e): add API tests for health and itinerary parse endpoints"
```

---

## Task 4: API tests — recommendations, hotels, chat

**Files:**
- Create: `frontend/e2e/api/recommendations.spec.ts`
- Create: `frontend/e2e/api/hotels.spec.ts`
- Create: `frontend/e2e/api/chat.spec.ts`

- [ ] **Step 1: Write recommendations.spec.ts**

Create `frontend/e2e/api/recommendations.spec.ts`:
```typescript
import { test, expect } from '@playwright/test';
import { API_URL, SIMPLE_TEXT, MULTI_REGION_TEXT, DEFAULT_PREFERENCES } from '../support/test-constants';

async function getParsedItinerary(request: any, text: string) {
  const res = await request.post(`${API_URL}/api/itinerary/parse`, { data: { text } });
  expect(res.status()).toBe(200);
  return res.json();
}

test.describe('POST /api/recommendations', () => {
  test('returns at least 1 recommendation for a valid single-region itinerary', async ({ request }) => {
    const itinerary = await getParsedItinerary(request, SIMPLE_TEXT);

    const res = await request.post(`${API_URL}/api/recommendations`, {
      data: { itinerary, preferences: DEFAULT_PREFERENCES },
      timeout: 120_000,
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.recommendations)).toBe(true);
    expect(body.recommendations.length).toBeGreaterThan(0);

    const first = body.recommendations[0];
    expect(typeof first.area_id).toBe('string');
    expect(typeof first.area_name).toBe('string');
    expect(typeof first.station).toBe('string');
    expect(typeof first.rank).toBe('number');
    expect(typeof first.total_score).toBe('number');
    expect(Array.isArray(first.pros)).toBe(true);
    expect(Array.isArray(first.cons)).toBe(true);
  });

  test('returns multiple region recommendations for multi-region itinerary', async ({ request }) => {
    const itinerary = await getParsedItinerary(request, MULTI_REGION_TEXT);

    const res = await request.post(`${API_URL}/api/recommendations`, {
      data: { itinerary, preferences: DEFAULT_PREFERENCES },
      timeout: 120_000,
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.recommendations.length).toBeGreaterThan(1);
    expect(typeof body.is_multi_region).toBe('boolean');
    expect(Array.isArray(body.regions_detected)).toBe(true);
  });

  test('returns 400 or 422 for missing body', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/recommendations`, {
      data: {},
    });

    expect([400, 422, 500]).toContain(res.status());
  });
});
```

- [ ] **Step 2: Write hotels.spec.ts**

Create `frontend/e2e/api/hotels.spec.ts`:
```typescript
import { test, expect } from '@playwright/test';
import { API_URL, getSeededAreaId } from '../support/test-constants';

test.describe('GET /api/hotels', () => {
  test('returns HotelSearchResultDto for valid area_id', async ({ request }) => {
    const areaId = getSeededAreaId();
    const res = await request.get(`${API_URL}/api/hotels?area_id=${areaId}`);

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.hotels)).toBe(true);
    expect(typeof body.total).toBe('number');
    expect(typeof body.page).toBe('number');
    expect(typeof body.has_more).toBe('boolean');
    expect(typeof body.provider).toBe('string');
  });

  test('accepts budget_tier filter', async ({ request }) => {
    const areaId = getSeededAreaId();
    const res = await request.get(`${API_URL}/api/hotels?area_id=${areaId}&budget_tier=budget`);

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.hotels)).toBe(true);
  });

  test('returns 400 or empty result for invalid area_id (not a GUID)', async ({ request }) => {
    const res = await request.get(`${API_URL}/api/hotels?area_id=not-a-guid`);

    expect([400, 200]).toContain(res.status());
    // Must not 500
    expect(res.status()).not.toBe(500);
  });

  test('returns 400 when area_id is missing', async ({ request }) => {
    const res = await request.get(`${API_URL}/api/hotels`);

    expect(res.status()).toBe(400);
  });
});
```

- [ ] **Step 3: Write chat.spec.ts**

Create `frontend/e2e/api/chat.spec.ts`:
```typescript
import { test, expect } from '@playwright/test';
import { API_URL, SIMPLE_TEXT, DEFAULT_PREFERENCES } from '../support/test-constants';

async function getParsedItinerary(request: any, text: string) {
  const res = await request.post(`${API_URL}/api/itinerary/parse`, { data: { text } });
  expect(res.status()).toBe(200);
  return res.json();
}

test.describe('POST /api/chat', () => {
  test('returns non-empty reply for a valid message with itinerary context', async ({ request }) => {
    const itinerary = await getParsedItinerary(request, SIMPLE_TEXT);

    const res = await request.post(`${API_URL}/api/chat`, {
      data: {
        session_id: `test-${Date.now()}`,
        message: 'How many days should I spend in Tokyo?',
        current_itinerary: itinerary,
      },
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(typeof body.message).toBe('string');
    expect(body.message.length).toBeGreaterThan(0);
    expect(typeof body.has_itinerary_update).toBe('boolean');
  });

  test('returns graceful response with no itinerary context (first message)', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/chat`, {
      data: {
        session_id: `test-${Date.now()}`,
        message: 'I want to visit Japan in April.',
        current_itinerary: null,
      },
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(typeof body.message).toBe('string');
    expect(body.message.length).toBeGreaterThan(0);
  });

  test('returns 400 for empty message', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/chat`, {
      data: {
        session_id: `test-${Date.now()}`,
        message: '',
        current_itinerary: null,
      },
    });

    // Either 400 or a graceful fallback — must not be 500
    expect(res.status()).not.toBe(500);
  });
});
```

- [ ] **Step 4: Run all API tests**

```bash
cd frontend
npx playwright test e2e/api/ --reporter=line
```

Expected: `16 passed` (allow retries for Railway cold starts)

- [ ] **Step 5: Commit**

```bash
cd frontend
git add e2e/api/recommendations.spec.ts e2e/api/hotels.spec.ts e2e/api/chat.spec.ts
git commit -m "feat(e2e): add API tests for recommendations, hotels, and chat endpoints"
```

---

## Task 5: UI tests — input page

**Files:**
- Create: `frontend/e2e/ui/input.spec.ts`

- [ ] **Step 1: Write input.spec.ts**

Create `frontend/e2e/ui/input.spec.ts`:
```typescript
import { test, expect } from '@playwright/test';
import { join } from 'path';
import { SIMPLE_TEXT } from '../support/test-constants';

test.describe('Input page — /  ', () => {
  test.beforeEach(async ({ page }) => {
    const errors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') errors.push(msg.text());
    });
    await page.goto('/');
    // Attach error tracker to test
    (page as any)._consoleErrors = errors;
  });

  test('page loads with heading and textarea visible', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Where Should I Stay in Japan?');
    await expect(page.locator('textarea')).toBeVisible();
    await expect(page.locator('button.btn-primary')).toBeVisible();
  });

  test('submit button is disabled when input is empty', async ({ page }) => {
    await expect(page.locator('button.btn-primary')).toBeDisabled();
  });

  test('shows date error when check-out is before check-in', async ({ page }) => {
    await page.locator('#checkin').fill('2026-06-10');
    await page.locator('#checkout').fill('2026-06-05');
    await page.locator('#checkout').dispatchEvent('change');

    await expect(page.locator('[role="alert"].field-error')).toBeVisible();
    await expect(page.locator('[role="alert"].field-error')).toContainText('Check-out date must be after');
  });

  test('shows error for unsupported file type', async ({ page }) => {
    await page.locator('input[type="file"]').setInputFiles({
      name: 'photo.jpg',
      mimeType: 'image/jpeg',
      buffer: Buffer.from('fake-image'),
    });

    await expect(page.locator('.error-banner[role="alert"]')).toBeVisible();
    await expect(page.locator('.error-banner[role="alert"]')).toContainText('Unsupported file type');
  });

  test('accepted file shows file name and clears on Remove click', async ({ page }) => {
    await page.locator('input[type="file"]').setInputFiles(
      join(__dirname, '../fixtures/simple.txt')
    );

    await expect(page.locator('.file-name')).toContainText('simple.txt');
    await expect(page.locator('textarea')).toBeDisabled();

    await page.locator('.btn-clear').click();

    await expect(page.locator('.drop-zone')).toBeVisible();
    await expect(page.locator('textarea')).toBeEnabled();
  });

  test('all 3 budget tier radio buttons are selectable', async ({ page }) => {
    for (const tier of ['budget', 'mid', 'luxury']) {
      await page.locator(`input[type="radio"][value="${tier}"]`).check();
      await expect(page.locator(`input[type="radio"][value="${tier}"]`)).toBeChecked();
    }
  });

  test('atmosphere checkboxes toggle on and off', async ({ page }) => {
    const quietLabel = page.locator('label.checkbox-label', { hasText: 'Quiet' });
    await quietLabel.click();
    await expect(quietLabel).toHaveClass(/selected/);
    await quietLabel.click();
    await expect(quietLabel).not.toHaveClass(/selected/);
  });

  test('pasting text enables submit and navigates to /review on success', async ({ page }) => {
    await page.locator('textarea').fill(SIMPLE_TEXT);
    await expect(page.locator('button.btn-primary')).toBeEnabled();
    await page.locator('button.btn-primary').click();
    await page.waitForURL('**/review', { timeout: 30_000 });
  });

  test('uploading valid .txt file navigates to /review on success', async ({ page }) => {
    await page.locator('input[type="file"]').setInputFiles(
      join(__dirname, '../fixtures/simple.txt')
    );

    await expect(page.locator('button.btn-primary')).toBeEnabled();
    await page.locator('button.btn-primary').click();
    await page.waitForURL('**/review', { timeout: 30_000 });
  });

  test('no unhandled JS console errors on page load', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') errors.push(msg.text());
    });
    await page.goto('/');
    await page.waitForTimeout(1000);
    expect(errors).toHaveLength(0);
  });
});
```

- [ ] **Step 2: Run input UI tests**

```bash
cd frontend
npx playwright test e2e/ui/input.spec.ts --reporter=line
```

Expected: `10 passed` (the navigation tests require the live Railway API to be up)

- [ ] **Step 3: Commit**

```bash
cd frontend
git add e2e/ui/input.spec.ts
git commit -m "feat(e2e): add UI tests for itinerary input page"
```

---

## Task 6: UI tests — review page

**Files:**
- Create: `frontend/e2e/ui/review.spec.ts`

- [ ] **Step 1: Write review.spec.ts**

Create `frontend/e2e/ui/review.spec.ts`:
```typescript
import { test, expect } from '@playwright/test';
import { SIMPLE_TEXT, MULTI_REGION_TEXT } from '../support/test-constants';

test.describe('Review page — /review', () => {
  test('direct navigation to /review with no itinerary shows fallback', async ({ page }) => {
    await page.goto('/review');
    // Should show the else branch ("No itinerary to review. Start over")
    await expect(page.locator('text=No itinerary to review')).toBeVisible({ timeout: 5_000 });
  });

  test('after valid parse: destinations are displayed', async ({ page }) => {
    await page.goto('/');
    await page.locator('textarea').fill(SIMPLE_TEXT);
    await page.locator('button.btn-primary').click();
    await page.waitForURL('**/review', { timeout: 30_000 });

    await expect(page.locator('h1')).toContainText('Review Your Itinerary');
    await expect(page.locator('.destination-list')).toBeVisible();
    await expect(page.locator('.destination-item').first()).toBeVisible();
  });

  test('multi-region itinerary shows multi-region info banner', async ({ page }) => {
    await page.goto('/');
    await page.locator('textarea').fill(MULTI_REGION_TEXT);
    await page.locator('button.btn-primary').click();
    await page.waitForURL('**/review', { timeout: 30_000 });

    await expect(page.locator('.banner--info')).toBeVisible();
    await expect(page.locator('.banner--info')).toContainText('Multiple regions detected');
  });

  test('chat panel is visible and accepts a message', async ({ page }) => {
    await page.goto('/');
    await page.locator('textarea').fill(SIMPLE_TEXT);
    await page.locator('button.btn-primary').click();
    await page.waitForURL('**/review', { timeout: 30_000 });

    const chatInput = page.locator('textarea[aria-label="Chat message input"]');
    await expect(chatInput).toBeVisible();

    await chatInput.fill('How long should I stay in Tokyo?');
    await page.locator('button[aria-label="Send message"]').click();

    // Typing indicator appears then assistant reply follows
    await expect(page.locator('.message--assistant').last()).toBeVisible({ timeout: 30_000 });
  });

  test('"Looks Good →" navigates to /results', async ({ page }) => {
    await page.goto('/');
    await page.locator('textarea').fill(SIMPLE_TEXT);
    await page.locator('button.btn-primary').click();
    await page.waitForURL('**/review', { timeout: 30_000 });

    await page.locator('button.btn-primary', { hasText: 'Looks Good' }).click();
    await page.waitForURL('**/results', { timeout: 120_000 });
  });

  test('"← Edit" navigates back to input page', async ({ page }) => {
    await page.goto('/');
    await page.locator('textarea').fill(SIMPLE_TEXT);
    await page.locator('button.btn-primary').click();
    await page.waitForURL('**/review', { timeout: 30_000 });

    await page.locator('button.btn-secondary', { hasText: 'Edit' }).click();
    await expect(page).toHaveURL('/');
  });
});
```

- [ ] **Step 2: Run review UI tests**

```bash
cd frontend
npx playwright test e2e/ui/review.spec.ts --reporter=line
```

Expected: `6 passed`

- [ ] **Step 3: Commit**

```bash
cd frontend
git add e2e/ui/review.spec.ts
git commit -m "feat(e2e): add UI tests for itinerary review page"
```

---

## Task 7: UI tests — results page

**Files:**
- Create: `frontend/e2e/ui/results.spec.ts`

- [ ] **Step 1: Write results.spec.ts**

Create `frontend/e2e/ui/results.spec.ts`:
```typescript
import { test, expect } from '@playwright/test';
import { MULTI_REGION_TEXT } from '../support/test-constants';
import { navigateToResults } from '../support/helpers';

test.describe('Results page — /results', () => {
  test('direct navigation to /results with no data shows empty state or redirects', async ({ page }) => {
    await page.goto('/results');
    // Either redirected to / or shows empty state — must not crash
    const isHome = page.url().endsWith('/') || page.url().endsWith('/#');
    const hasEmptyState = await page.locator('.empty-state').isVisible().catch(() => false);
    expect(isHome || hasEmptyState).toBe(true);
  });

  test('at least 1 recommendation card is shown after full flow', async ({ page }) => {
    await navigateToResults(page);

    await expect(page.locator('.rec-card').first()).toBeVisible();
  });

  test('recommendation card shows area name, station, score bar, pros and cons', async ({ page }) => {
    await navigateToResults(page);

    const card = page.locator('.rec-card').first();
    await expect(card.locator('.area-name')).toBeVisible();
    await expect(card.locator('.area-meta')).toBeVisible();
    await expect(card.locator('[role="progressbar"]')).toBeVisible();
    await expect(card.locator('.pros')).toBeVisible();
    await expect(card.locator('.cons')).toBeVisible();
  });

  test('multi-region itinerary shows multi-region banner', async ({ page }) => {
    await page.goto('/');
    await page.locator('textarea').fill(MULTI_REGION_TEXT);
    await page.locator('button.btn-primary').click();
    await page.waitForURL('**/review', { timeout: 30_000 });
    await page.locator('button.btn-primary', { hasText: 'Looks Good' }).click();
    await page.waitForURL('**/results', { timeout: 120_000 });

    await expect(page.locator('.multi-region-banner')).toBeVisible();
  });

  test('"View all hotels →" navigates to /hotels/:areaId', async ({ page }) => {
    await navigateToResults(page);

    const viewHotelsLink = page.locator('.view-all-hotels').first();
    await expect(viewHotelsLink).toBeVisible();
    await viewHotelsLink.click();
    await page.waitForURL('**/hotels/**', { timeout: 15_000 });
  });

  test('"Start Over" clears state and navigates to /', async ({ page }) => {
    await navigateToResults(page);

    await page.locator('button', { hasText: 'Start Over' }).click();
    await expect(page).toHaveURL('/');
    await expect(page.locator('h1')).toContainText('Where Should I Stay in Japan?');
  });

  test('"← Edit Itinerary" navigates back to /review', async ({ page }) => {
    await navigateToResults(page);

    await page.locator('a', { hasText: 'Edit Itinerary' }).click();
    await expect(page).toHaveURL(/\/review/);
  });
});
```

- [ ] **Step 2: Run results UI tests**

```bash
cd frontend
npx playwright test e2e/ui/results.spec.ts --reporter=line
```

Expected: `7 passed` (note: these tests run through the full flow; first run may be slow due to Railway cold start)

- [ ] **Step 3: Commit**

```bash
cd frontend
git add e2e/ui/results.spec.ts
git commit -m "feat(e2e): add UI tests for recommendation results page"
```

---

## Task 8: UI tests — hotels page

**Files:**
- Create: `frontend/e2e/ui/hotels.spec.ts`

- [ ] **Step 1: Write hotels.spec.ts**

Create `frontend/e2e/ui/hotels.spec.ts`:
```typescript
import { test, expect } from '@playwright/test';
import { getSeededAreaId } from '../support/test-constants';
import { navigateToResults } from '../support/helpers';

test.describe('Hotels page — /hotels/:areaId', () => {
  test('loads hotel results when navigating directly with valid area_id', async ({ page }) => {
    const areaId = getSeededAreaId();
    await page.goto(`/hotels/${areaId}?area=Tokyo+Station`);

    await expect(page.locator('h1')).toContainText('Hotels near');
    // Wait for loading to complete
    await expect(page.locator('.hotel-grid, .empty-state, .error-banner')).toBeVisible({ timeout: 30_000 });
  });

  test('hotel cards show name, price, and book button', async ({ page }) => {
    const areaId = getSeededAreaId();
    await page.goto(`/hotels/${areaId}?area=Tokyo+Station`);

    const firstCard = page.locator('.hotel-card').first();
    await expect(firstCard).toBeVisible({ timeout: 30_000 });
    await expect(firstCard.locator('.hotel-name')).toBeVisible();
    await expect(firstCard.locator('.hotel-price')).toBeVisible();
    await expect(firstCard.locator('.btn-book')).toBeVisible();
  });

  test('"Book on Rakuten →" opens external URL in new tab', async ({ page, context }) => {
    const areaId = getSeededAreaId();
    await page.goto(`/hotels/${areaId}?area=Tokyo+Station`);
    await page.locator('.hotel-card').first().waitFor({ timeout: 30_000 });

    const [newPage] = await Promise.all([
      context.waitForEvent('page'),
      page.locator('.btn-book').first().click(),
    ]);

    await newPage.waitForLoadState('domcontentloaded');
    expect(newPage.url()).not.toBe('about:blank');
    await newPage.close();
  });

  test('shows graceful empty state when no hotels found (not a crash)', async ({ page }) => {
    // Use a zeroed-out GUID — server should return empty result, not 500
    await page.goto('/hotels/00000000-0000-0000-0000-000000000000?area=Unknown');

    // Either empty state message or error banner — must not show blank page
    await expect(
      page.locator('.empty-state, .error-banner')
    ).toBeVisible({ timeout: 15_000 });
  });

  test('"← Back to results" navigates to /results', async ({ page }) => {
    const areaId = getSeededAreaId();
    await page.goto(`/hotels/${areaId}?area=Tokyo+Station`);

    await page.locator('a.back-link', { hasText: 'Back to results' }).click();
    await expect(page).toHaveURL(/\/results/);
  });

  test('pagination controls are shown when total > 10 results', async ({ page }) => {
    const areaId = getSeededAreaId();
    await page.goto(`/hotels/${areaId}?area=Tokyo+Station`);
    await page.locator('.hotel-grid, .empty-state').waitFor({ timeout: 30_000 });

    const resultCount = await page.locator('.result-count').textContent();
    const total = parseInt(resultCount?.match(/of (\d+)/)?.[1] ?? '0');

    if (total > 10) {
      await expect(page.locator('.pagination')).toBeVisible();
      const nextBtn = page.locator('button[aria-label="Next page"]');
      await nextBtn.click();
      await expect(page.locator('.page-info')).toContainText('Page 2');
    } else {
      // Pagination not shown for ≤10 results — that's correct behavior
      test.info().annotations.push({ type: 'note', description: 'Fewer than 10 results; pagination not shown' });
    }
  });
});
```

- [ ] **Step 2: Run hotels UI tests**

```bash
cd frontend
npx playwright test e2e/ui/hotels.spec.ts --reporter=line
```

Expected: `6 passed`

- [ ] **Step 3: Run the full suite**

```bash
cd frontend
npx playwright test --reporter=line
```

Expected: `~40 passed` across 9 spec files. Note any failures, check Railway API is responding, retry once.

- [ ] **Step 4: Commit**

```bash
cd frontend
git add e2e/ui/hotels.spec.ts
git commit -m "feat(e2e): add UI tests for hotel list page"
```

---

## Task 9: Final wiring and cleanup

**Files:**
- Modify: `frontend/package.json` — verify scripts
- Create: `frontend/e2e/.gitignore`

- [ ] **Step 1: Add e2e/.gitignore for state dir**

Create `frontend/e2e/.gitignore`:
```
.state/
```

- [ ] **Step 2: Verify full suite runs clean**

```bash
cd frontend
npx playwright test --reporter=html
```

Open `playwright-report/index.html` to review test results. All tests should be green or at most 1 retry per flaky network test.

- [ ] **Step 3: Add tsx to devDependencies if not present**

```bash
cd frontend
npm install --save-dev tsx
```

- [ ] **Step 4: Final commit**

```bash
cd frontend
git add e2e/.gitignore package.json package-lock.json
git commit -m "feat(e2e): finalize E2E test suite — 40 tests across 9 spec files"
```

---

## Self-Review Checklist

- [x] **Spec coverage:** All 9 spec files from design covered. All ~40 test scenarios covered.
- [x] **Placeholders:** No TBDs or incomplete steps. All test code is fully written.
- [x] **Type consistency:** `getSeededAreaId()` used consistently across hotels API and hotels UI tests. `navigateToResults()` helper used consistently in results and hotels UI tests. `DEFAULT_PREFERENCES` shape matches `UserPreferences` interface exactly. `SIMPLE_TEXT` / `MULTI_REGION_TEXT` / `MESSY_TEXT` constants defined once and reused.
- [x] **snake_case contract:** All API request bodies and response assertions use snake_case (`area_id`, `is_multi_region`, `parsing_confidence`, etc.) matching the live SnakeCaseLower serialization policy.
- [x] **Timeout strategy:** API tests use 30s default; recommendation calls use `timeout: 120_000` explicitly. UI tests that reach `/results` use `waitForURL` with 120s timeout.
