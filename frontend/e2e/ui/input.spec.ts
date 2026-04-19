import { test, expect } from '@playwright/test';
import { join } from 'path';
import { SIMPLE_TEXT } from '../support/test-constants';

test.describe('Input page — /', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
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

    // Error text: "Check-out date must be after check-in date."
    await expect(
      page.locator('.field-error[role="alert"]').filter({ hasText: /check-out/i })
    ).toBeVisible({ timeout: 5_000 });
  });

  test('shows error for unsupported file type', async ({ page }) => {
    await page.locator('input[type="file"]').setInputFiles({
      name: 'photo.jpg',
      mimeType: 'image/jpeg',
      buffer: Buffer.from('fake-image'),
    });

    // Error surfaces in .error-banner[role="alert"] via store.setError()
    await expect(
      page.locator('.error-banner[role="alert"]').filter({ hasText: /unsupported|invalid|not supported/i })
    ).toBeVisible({ timeout: 5_000 });
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
    // The radio inputs are visually hidden by CSS; click the wrapping label to select,
    // then verify the hidden input is checked (toBeChecked does not require visibility).
    const labelTexts = ['Budget', 'Mid-range', 'Luxury'];
    for (const text of labelTexts) {
      const label = page.locator('label.radio-label', { hasText: text });
      await label.click();
      await expect(label.locator('input[type="radio"]')).toBeChecked();
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
    const mockParse = { destinations: [{ name: 'Shinjuku', city: 'Tokyo', region: 'Kanto', day_number: 1, activity_type: null }], regions_detected: ['Kanto'], is_multi_region: false, parsing_confidence: 'high', clarification_needed: false, raw_text: SIMPLE_TEXT };
    await page.route('**/api/itinerary/parse', async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(mockParse) }));
    await page.locator('textarea').fill(SIMPLE_TEXT);
    await expect(page.locator('button.btn-primary')).toBeEnabled();
    await page.locator('button.btn-primary').click();
    await page.waitForURL('**/review', { timeout: 15_000 });
  });

  test('uploading valid .txt file enables submit and navigates to /review', async ({ page }) => {
    const mockParse = { destinations: [{ name: 'Tokyo', city: 'Tokyo', region: 'Kanto', day_number: 1, activity_type: null }], regions_detected: ['Kanto'], is_multi_region: false, parsing_confidence: 'high', clarification_needed: false, raw_text: 'simple text' };
    await page.route('**/api/itinerary/parse**', async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(mockParse) }));
    await page.locator('input[type="file"]').setInputFiles(
      join(__dirname, '../fixtures/simple.txt')
    );

    await expect(page.locator('button.btn-primary')).toBeEnabled();
    await page.locator('button.btn-primary').click();
    await page.waitForURL('**/review', { timeout: 15_000 });
  });

  test('no unhandled JS console errors on page load', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') errors.push(msg.text());
    });
    await page.goto('/');
    await page.waitForTimeout(2_000);
    expect(errors).toHaveLength(0);
  });
});
