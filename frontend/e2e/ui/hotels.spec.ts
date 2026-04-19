import { test, expect } from '@playwright/test';
import { getSeededAreaId } from '../support/test-constants';

const MOCK_HOTELS = {
  hotels: [
    {
      hotel_id: 'h1',
      name: 'Shinjuku Grand Hotel',
      price_per_night_jpy: 12000,
      review_score: 4.2,
      deep_link_url: 'https://travel.rakuten.com',
    },
    {
      hotel_id: 'h2',
      name: 'Tokyo City Inn',
      price_per_night_jpy: 8500,
      review_score: 3.9,
      deep_link_url: 'https://travel.rakuten.com',
    },
  ],
  total: 2,
  page: 1,
  has_more: false,
};

test.describe('Hotels page — /hotels/:id', () => {
  test.beforeEach(async ({ page }) => {
    const areaId = getSeededAreaId();
    await page.route('**/api/hotels**', async route => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(MOCK_HOTELS) });
    });
    await page.goto(`/hotels/${areaId}?area=Shinjuku`);
  });

  test('shows "Hotels near" heading', async ({ page }) => {
    await expect(page.locator('h1')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('h1')).toContainText(/hotels near/i);
  });

  test('"← Back to results" link is visible', async ({ page }) => {
    await expect(page.locator('a.back-link')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('a.back-link')).toContainText(/back to results/i);
  });

  test('shows hotel cards', async ({ page }) => {
    await expect(page.locator('app-hotel-card').first()).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('app-hotel-card')).toHaveCount(2);
  });

  test('result-count shows correct number', async ({ page }) => {
    await expect(page.locator('.result-count')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('.result-count')).toContainText('2 of 2');
  });

  test('"← Back to results" navigates to /results', async ({ page }) => {
    await page.locator('a.back-link').click();
    await page.waitForURL('**/results', { timeout: 10_000 });
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
