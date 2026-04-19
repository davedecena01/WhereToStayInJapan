import { test, expect, Page } from '@playwright/test';

const AREA_ID = 'a1000001-0000-0000-0000-000000000006';

const MOCK_PARSE = {
  destinations: [
    { name: 'Shinjuku', city: 'Tokyo', region: 'Kanto', day_number: 1, activity_type: null },
    { name: 'Shibuya', city: 'Tokyo', region: 'Kanto', day_number: 2, activity_type: null },
  ],
  regions_detected: ['Kanto'],
  is_multi_region: false,
  start_date: null,
  end_date: null,
  parsing_confidence: 'high',
  clarification_needed: false,
  raw_text: 'Day 1-3: Tokyo (Shinjuku, Shibuya).',
};

const MOCK_RECOMMENDATIONS = {
  recommendations: [
    {
      area_id: AREA_ID,
      area_name: 'Shinjuku',
      station: 'Shinjuku Station',
      city: 'Tokyo',
      region: 'Kanto',
      rank: 1,
      total_score: 0.85,
      score_breakdown: { travel_time: 0.9, cost: 0.8, station_proximity: 0.95, food_access: 0.85 },
      avg_travel_time_minutes: 25,
      explanation: 'Shinjuku is an excellent base with superb transit access.',
      pros: ['Major transit hub', 'Vibrant nightlife'],
      cons: ['Can be crowded'],
      hotels_available: true,
      hotel_preview: [
        { hotel_id: 'h1', name: 'Shinjuku Grand Hotel', price_per_night_jpy: 12000, review_score: 4.2, deep_link_url: 'https://travel.rakuten.com' },
      ],
      featured_food: [
        { name: 'Omoide Yokocho', cuisine_type: 'izakaya' },
      ],
    },
  ],
  regions_detected: ['Kanto'],
  is_multi_region: false,
  multi_region_warning: null,
};

const MOCK_HOTELS = {
  hotels: [
    { hotel_id: 'h1', name: 'Shinjuku Grand Hotel', price_per_night_jpy: 12000, review_score: 4.2, deep_link_url: 'https://travel.rakuten.com', area_id: AREA_ID },
  ],
  total: 1,
  page: 1,
  has_more: false,
};

async function setupMocks(page: Page): Promise<void> {
  await page.route('**/api/itinerary/parse', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(MOCK_PARSE) });
  });
  await page.route('**/api/recommendations', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(MOCK_RECOMMENDATIONS) });
  });
  await page.route('**/api/hotels**', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(MOCK_HOTELS) });
  });
}

async function navigateToResults(page: Page): Promise<void> {
  await setupMocks(page);
  await page.goto('/');
  await page.locator('textarea').fill('Some itinerary text');
  await expect(page.locator('button.btn-primary')).toBeEnabled({ timeout: 5_000 });
  await page.locator('button.btn-primary').click();
  await page.waitForURL('**/review', { timeout: 15_000 });
  await expect(page.locator('button.btn-primary')).toBeEnabled({ timeout: 5_000 });
  await page.locator('button.btn-primary').click(); // "Looks Good →"
  await page.waitForURL('**/results', { timeout: 10_000 });
}

test.describe('Results page — /results', () => {
  test.beforeEach(async ({ page }) => {
    await navigateToResults(page);
  });

  test('shows "Where to Stay" heading once recommendations load', async ({ page }) => {
    await expect(page.locator('h1.results-title')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('h1.results-title')).toContainText('Where to Stay');
  });

  test('shows at least one recommendation card', async ({ page }) => {
    await expect(page.locator('.rec-card').first()).toBeVisible({ timeout: 10_000 });
  });

  test('top recommendation card shows area name and station', async ({ page }) => {
    const card = page.locator('.rec-card--top');
    await expect(card).toBeVisible({ timeout: 10_000 });
    await expect(card.locator('.area-name')).toContainText('Shinjuku');
    await expect(card.locator('.area-meta')).toContainText('Shinjuku Station');
  });

  test('rank badge is visible on top card', async ({ page }) => {
    await expect(page.locator('.rec-card--top .rank-badge')).toBeVisible({ timeout: 10_000 });
  });

  test('score bar is rendered', async ({ page }) => {
    await page.locator('.rec-card').first().waitFor({ timeout: 10_000 });
    await expect(page.locator('.score-bar-fill').first()).toBeVisible();
  });

  test('explanation text is shown', async ({ page }) => {
    await page.locator('.rec-card').first().waitFor({ timeout: 10_000 });
    await expect(page.locator('.explanation').first()).toContainText('Shinjuku');
  });

  test('pros and cons lists are shown', async ({ page }) => {
    await page.locator('.rec-card').first().waitFor({ timeout: 10_000 });
    await expect(page.locator('.pros').first()).toBeVisible();
    await expect(page.locator('.cons').first()).toBeVisible();
  });

  test('"← Edit Itinerary" link is visible', async ({ page }) => {
    await page.locator('.rec-card').first().waitFor({ timeout: 10_000 });
    await expect(page.locator('a', { hasText: /edit itinerary/i })).toBeVisible();
  });

  test('"View all hotels" link navigates to hotel list', async ({ page }) => {
    await page.locator('.view-all-hotels').first().waitFor({ timeout: 10_000 });
    await page.locator('.view-all-hotels').first().click();
    await page.waitForURL('**/hotels/**', { timeout: 10_000 });
    await expect(page.locator('h1')).toContainText('Hotels near');
  });
});
