import { Page } from '@playwright/test';
import { SIMPLE_TEXT } from './test-constants';

export async function navigateToResults(page: Page): Promise<void> {
  await page.goto('/');
  await page.locator('textarea').fill(SIMPLE_TEXT);
  await page.locator('button.btn-primary').click();
  await page.waitForURL('**/review', { timeout: 30_000 });
  await page.locator('button.btn-primary', { hasText: 'Looks Good' }).click();
  await page.waitForURL('**/results', { timeout: 120_000 });
}
