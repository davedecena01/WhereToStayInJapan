import { test, expect } from '@playwright/test';

const MOCK_GENERATE_SUCCESS = {
  destinations: [
    { name: 'Shinjuku', city: 'Tokyo', region: 'Kanto', day_number: 1, activity_type: null },
    { name: 'Asakusa', city: 'Tokyo', region: 'Kanto', day_number: 2, activity_type: null },
    { name: 'Fushimi Inari', city: 'Kyoto', region: 'Kansai', day_number: 3, activity_type: null },
  ],
  regions_detected: ['Kanto', 'Kansai'],
  is_multi_region: true,
  start_date: null,
  end_date: null,
  parsing_confidence: 'high',
  clarification_needed: false,
  raw_text: null,
};

test.describe('Create page — /create', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/create');
  });

  // ── Page structure ─────────────────────────────────────────────────────────

  test('page loads with both sections visible', async ({ page }) => {
    await expect(page.locator('.standard-section h2')).toContainText('Build My Itinerary');
    await expect(page.locator('.challenge-section h2')).toContainText('Ready for a Challenge?');
  });

  test('challenge description text is visible', async ({ page }) => {
    await expect(page.locator('.challenge-desc')).toContainText("Skip the crowds");
  });

  test('no unhandled JS console errors on load', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') errors.push(msg.text());
    });
    await page.goto('/create');
    await page.waitForTimeout(1_500);
    expect(errors).toHaveLength(0);
  });

  // ── Step indicator ─────────────────────────────────────────────────────────

  test('step indicator starts at Step 1 of 4', async ({ page }) => {
    await expect(page.locator('.step-indicator')).toContainText('Step 1 of 4');
  });

  test('step indicator advances to Step 2 after duration is selected', async ({ page }) => {
    await page.locator('.standard-section .option-btn', { hasText: '3–5 days' }).click();
    await expect(page.locator('.step-indicator')).toContainText('Step 2 of 4');
  });

  test('step indicator advances to Step 3 after region is selected', async ({ page }) => {
    await page.locator('.standard-section .option-btn', { hasText: '3–5 days' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kanto' }).click();
    await expect(page.locator('.step-indicator')).toContainText('Step 3 of 4');
  });

  test('step indicator advances to Step 4 after style is selected', async ({ page }) => {
    await page.locator('.standard-section .option-btn', { hasText: '3–5 days' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kanto' }).click();
    await page.locator('.style-card', { hasText: 'Cultural' }).click();
    await expect(page.locator('.step-indicator')).toContainText('Step 4 of 4');
  });

  test('step indicator shows "Ready to generate" when all 4 steps complete', async ({ page }) => {
    await page.locator('.standard-section .option-btn', { hasText: '5–7 days' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kansai' }).click();
    await page.locator('.style-card', { hasText: 'Foodie' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Relaxed' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Mid-range' }).click();
    await expect(page.locator('.step-indicator')).toContainText('Ready to generate');
  });

  test('step indicator shows pace & budget prompt at step 4 before they are set', async ({ page }) => {
    await page.locator('.standard-section .option-btn', { hasText: '7–10 days' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kanto' }).click();
    await page.locator('.style-card', { hasText: 'Nature' }).click();
    await expect(page.locator('.step-indicator')).toContainText('Select pace & budget to continue');
  });

  // ── Standard section selections ────────────────────────────────────────────

  test('all 4 duration options are clickable and mark as selected', async ({ page }) => {
    const labels = ['3–5 days', '5–7 days', '7–10 days', '10+ days'];
    for (const label of labels) {
      const btn = page.locator('.standard-section .option-btn', { hasText: label });
      await btn.click();
      await expect(btn).toHaveClass(/selected/);
    }
  });

  test('selecting a new duration deselects the previous one', async ({ page }) => {
    await page.locator('.standard-section .option-btn', { hasText: '3–5 days' }).click();
    await page.locator('.standard-section .option-btn', { hasText: '7–10 days' }).click();
    await expect(page.locator('.standard-section .option-btn', { hasText: '3–5 days' })).not.toHaveClass(/selected/);
    await expect(page.locator('.standard-section .option-btn', { hasText: '7–10 days' })).toHaveClass(/selected/);
  });

  test('region chips toggle on and off in standard section', async ({ page }) => {
    const chip = page.locator('.standard-section .chip', { hasText: 'Kanto' });
    await chip.click();
    await expect(chip).toHaveClass(/selected/);
    await chip.click();
    await expect(chip).not.toHaveClass(/selected/);
  });

  test('multiple regions can be selected simultaneously', async ({ page }) => {
    await page.locator('.standard-section .chip', { hasText: 'Kanto' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kansai' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kyushu' }).click();
    await expect(page.locator('.standard-section .chip.selected')).toHaveCount(3);
  });

  test('all 5 style cards are selectable', async ({ page }) => {
    const styles = ['Cultural', 'Foodie', 'Nature', 'Urban', 'Mix'];
    for (const style of styles) {
      const card = page.locator('.style-card', { hasText: style });
      await card.click();
      await expect(card).toHaveClass(/selected/);
    }
  });

  test('pace and budget options are selectable', async ({ page }) => {
    await page.locator('.standard-section .option-btn', { hasText: 'Moderate' }).click();
    await expect(page.locator('.standard-section .option-btn', { hasText: 'Moderate' })).toHaveClass(/selected/);
    await page.locator('.standard-section .option-btn', { hasText: 'Luxury' }).click();
    await expect(page.locator('.standard-section .option-btn', { hasText: 'Luxury' })).toHaveClass(/selected/);
  });

  // ── Standard generate button ───────────────────────────────────────────────

  test('standard generate button is disabled initially', async ({ page }) => {
    await expect(page.locator('.standard-section .btn-primary')).toBeDisabled();
  });

  test('standard generate button remains disabled with partial selections', async ({ page }) => {
    await page.locator('.standard-section .option-btn', { hasText: '5–7 days' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kansai' }).click();
    // missing style, pace, budget
    await expect(page.locator('.standard-section .btn-primary')).toBeDisabled();
  });

  test('standard generate button enables when all 4 steps complete', async ({ page }) => {
    await page.locator('.standard-section .option-btn', { hasText: '3–5 days' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kanto' }).click();
    await page.locator('.style-card', { hasText: 'Urban' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Packed' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Budget' }).click();
    await expect(page.locator('.standard-section .btn-primary')).toBeEnabled();
  });

  test('standard generate success navigates to /review', async ({ page }) => {
    await page.route('**/api/itinerary/generate', async route =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(MOCK_GENERATE_SUCCESS) })
    );
    await page.locator('.standard-section .option-btn', { hasText: '3–5 days' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kanto' }).click();
    await page.locator('.style-card', { hasText: 'Mix' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Relaxed' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Mid-range' }).click();
    await page.locator('.standard-section .btn-primary').click();
    await page.waitForURL('**/review', { timeout: 15_000 });
  });

  test('standard generate shows spinner while loading', async ({ page }) => {
    let resolve!: () => void;
    const hold = new Promise<void>(r => { resolve = r; });

    await page.route('**/api/itinerary/generate', async route => {
      await hold;
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(MOCK_GENERATE_SUCCESS) });
    });

    await page.locator('.standard-section .option-btn', { hasText: '3–5 days' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kanto' }).click();
    await page.locator('.style-card', { hasText: 'Cultural' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Moderate' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Mid-range' }).click();
    await page.locator('.standard-section .btn-primary').click();

    await expect(page.locator('.standard-section .spinner')).toBeVisible({ timeout: 3_000 });
    await expect(page.locator('.standard-section .btn-primary')).toBeDisabled();
    resolve();
    await page.waitForURL('**/review', { timeout: 10_000 });
  });

  test('standard generate shows error banner on API failure and dismiss clears it', async ({ page }) => {
    await page.route('**/api/itinerary/generate', async route =>
      route.fulfill({ status: 500, contentType: 'application/json', body: JSON.stringify({ error: 'Internal error' }) })
    );
    await page.locator('.standard-section .option-btn', { hasText: '3–5 days' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kanto' }).click();
    await page.locator('.style-card', { hasText: 'Foodie' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Packed' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Luxury' }).click();
    await page.locator('.standard-section .btn-primary').click();

    const banner = page.locator('.standard-section .error-banner[role="alert"]');
    await expect(banner).toBeVisible({ timeout: 8_000 });
    await expect(banner).toContainText(/generation failed|try again/i);

    // Dismiss clears only the standard section error
    await banner.locator('.dismiss-btn').click();
    await expect(banner).not.toBeVisible();
    // Challenge section must NOT show any error banner
    await expect(page.locator('.challenge-section .error-banner')).not.toBeVisible();
  });

  // ── Challenge section ──────────────────────────────────────────────────────

  test('challenge generate button is disabled initially', async ({ page }) => {
    await expect(page.locator('.btn-challenge')).toBeDisabled();
  });

  test('challenge generate button is disabled with only duration selected', async ({ page }) => {
    await page.locator('.challenge-section .option-btn--dark', { hasText: '7–10 days' }).click();
    await expect(page.locator('.btn-challenge')).toBeDisabled();
  });

  test('challenge generate button enables with duration + region selected', async ({ page }) => {
    await page.locator('.challenge-section .option-btn--dark', { hasText: '5–7 days' }).click();
    await page.locator('.challenge-section .chip--dark', { hasText: 'Tohoku' }).click();
    await expect(page.locator('.btn-challenge')).toBeEnabled();
  });

  test('challenge region chips toggle on and off', async ({ page }) => {
    const chip = page.locator('.challenge-section .chip--dark', { hasText: 'Hokkaido' });
    await chip.click();
    await expect(chip).toHaveClass(/selected/);
    await chip.click();
    await expect(chip).not.toHaveClass(/selected/);
  });

  test('challenge generate success navigates to /review', async ({ page }) => {
    await page.route('**/api/itinerary/generate', async route =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(MOCK_GENERATE_SUCCESS) })
    );
    await page.locator('.challenge-section .option-btn--dark', { hasText: '7–10 days' }).click();
    await page.locator('.challenge-section .chip--dark', { hasText: 'Kansai' }).click();
    await page.locator('.btn-challenge').click();
    await page.waitForURL('**/review', { timeout: 15_000 });
  });

  test('challenge generate shows spinner while loading', async ({ page }) => {
    let resolve!: () => void;
    const hold = new Promise<void>(r => { resolve = r; });

    await page.route('**/api/itinerary/generate', async route => {
      await hold;
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(MOCK_GENERATE_SUCCESS) });
    });

    await page.locator('.challenge-section .option-btn--dark', { hasText: '10+ days' }).click();
    await page.locator('.challenge-section .chip--dark', { hasText: 'Kyushu' }).click();
    await page.locator('.btn-challenge').click();

    await expect(page.locator('.spinner--light')).toBeVisible({ timeout: 3_000 });
    await expect(page.locator('.btn-challenge')).toBeDisabled();
    resolve();
    await page.waitForURL('**/review', { timeout: 10_000 });
  });

  test('challenge generate shows error banner on API failure', async ({ page }) => {
    await page.route('**/api/itinerary/generate', async route =>
      route.fulfill({ status: 500, contentType: 'application/json', body: JSON.stringify({ error: 'error' }) })
    );
    await page.locator('.challenge-section .option-btn--dark', { hasText: '3–5 days' }).click();
    await page.locator('.challenge-section .chip--dark', { hasText: 'Okinawa' }).click();
    await page.locator('.btn-challenge').click();

    await expect(page.locator('.challenge-section .error-banner[role="alert"]')).toBeVisible({ timeout: 8_000 });
    // Standard section must NOT show the challenge error
    await expect(page.locator('.standard-section .error-banner')).not.toBeVisible();
  });

  // ── Mutual loading lock ────────────────────────────────────────────────────

  test('both buttons are disabled while standard generation is loading', async ({ page }) => {
    let resolve!: () => void;
    const hold = new Promise<void>(r => { resolve = r; });

    await page.route('**/api/itinerary/generate', async route => {
      await hold;
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(MOCK_GENERATE_SUCCESS) });
    });

    await page.locator('.standard-section .option-btn', { hasText: '3–5 days' }).click();
    await page.locator('.standard-section .chip', { hasText: 'Kanto' }).click();
    await page.locator('.style-card', { hasText: 'Cultural' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Moderate' }).click();
    await page.locator('.standard-section .option-btn', { hasText: 'Mid-range' }).click();
    await page.locator('.standard-section .btn-primary').click();

    await expect(page.locator('.standard-section .btn-primary')).toBeDisabled({ timeout: 3_000 });
    await expect(page.locator('.btn-challenge')).toBeDisabled();
    resolve();
  });
});

// ── "Build one →" link from input page ──────────────────────────────────────
test.describe('Input page → create link', () => {
  test('"Build one →" link navigates to /create', async ({ page }) => {
    await page.goto('/');
    const link = page.locator('.create-link a', { hasText: /build one/i });
    await expect(link).toBeVisible();
    await link.click();
    await page.waitForURL('**/create', { timeout: 10_000 });
    await expect(page.locator('.standard-section h2')).toContainText('Build My Itinerary');
  });
});
