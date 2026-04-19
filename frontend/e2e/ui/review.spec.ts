import { test, expect, Page } from '@playwright/test';

const MOCK_PARSE_SINGLE = {
  destinations: [
    { name: 'Shinjuku', city: 'Tokyo', region: 'Kanto', day_number: 1, activity_type: null },
    { name: 'Shibuya', city: 'Tokyo', region: 'Kanto', day_number: 2, activity_type: null },
    { name: 'Nikko', city: 'Nikko', region: 'Kanto', day_number: 4, activity_type: null },
  ],
  regions_detected: ['Kanto'],
  is_multi_region: false,
  start_date: null,
  end_date: null,
  parsing_confidence: 'high',
  clarification_needed: false,
  raw_text: 'Day 1-3: Tokyo (Shinjuku, Shibuya). Day 4: Nikko.',
};

const MOCK_PARSE_MULTI = {
  destinations: [
    { name: 'Shinjuku', city: 'Tokyo', region: 'Kanto', day_number: 1, activity_type: null },
    { name: 'Gion', city: 'Kyoto', region: 'Kansai', day_number: 4, activity_type: null },
    { name: 'Dotonbori', city: 'Osaka', region: 'Kansai', day_number: 7, activity_type: null },
  ],
  regions_detected: ['Kanto', 'Kansai'],
  is_multi_region: true,
  start_date: null,
  end_date: null,
  parsing_confidence: 'high',
  clarification_needed: false,
  raw_text: 'Days 1-3: Tokyo. Days 4-6: Kyoto. Day 7: Osaka.',
};

async function navigateToReview(page: Page, mockResponse: object): Promise<void> {
  await page.route('**/api/itinerary/parse', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(mockResponse) });
  });
  await page.goto('/');
  await page.locator('textarea').fill('Some itinerary text');
  await expect(page.locator('button.btn-primary')).toBeEnabled({ timeout: 5_000 });
  await page.locator('button.btn-primary').click();
  await page.waitForURL('**/review', { timeout: 15_000 });
}

test.describe('Review page — /review', () => {
  test.beforeEach(async ({ page }) => {
    await navigateToReview(page, MOCK_PARSE_SINGLE);
  });

  test('shows heading and destination count', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Review Your Itinerary');
    await expect(page.locator('.subtitle')).toContainText('3 destination(s)');
  });

  test('lists all destination items', async ({ page }) => {
    await expect(page.locator('.destination-item')).toHaveCount(3);
  });

  test('destination item shows name and region tags', async ({ page }) => {
    const first = page.locator('.destination-item').first();
    await expect(first.locator('.dest-name')).toContainText('Shinjuku');
    await expect(first.locator('.tag--city')).toContainText('Tokyo');
    await expect(first.locator('.tag--region')).toContainText('Kanto');
  });

  test('destination with day number shows day tag', async ({ page }) => {
    const first = page.locator('.destination-item').first();
    await expect(first.locator('.tag--day')).toContainText('Day 1');
  });

  test('"← Edit" button navigates back to /', async ({ page }) => {
    await page.locator('button.btn-secondary', { hasText: /edit/i }).click();
    await page.waitForURL('**/', { timeout: 10_000 });
    await expect(page).toHaveURL(/\/$/);
  });

  test('"Looks Good" button is enabled when destinations exist', async ({ page }) => {
    await expect(page.locator('button.btn-primary')).toBeEnabled();
  });

  test('no low-confidence warning for high-confidence parse', async ({ page }) => {
    await expect(page.locator('.banner--warning')).not.toBeVisible();
  });

  test('no unhandled JS console errors', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') errors.push(msg.text());
    });
    await page.reload();
    await page.waitForTimeout(1_000);
    expect(errors).toHaveLength(0);
  });
});

test.describe('Review page — multi-region itinerary', () => {
  test.beforeEach(async ({ page }) => {
    await navigateToReview(page, MOCK_PARSE_MULTI);
  });

  test('shows multi-region info banner', async ({ page }) => {
    await expect(page.locator('.banner--info')).toBeVisible();
    await expect(page.locator('.banner--info')).toContainText(/region/i);
  });

  test('lists destinations from multiple regions', async ({ page }) => {
    await expect(page.locator('.destination-item')).toHaveCount(3);
    await expect(page.locator('.tag--region').filter({ hasText: 'Kanto' }).first()).toBeVisible();
    await expect(page.locator('.tag--region').filter({ hasText: 'Kansai' }).first()).toBeVisible();
  });
});
