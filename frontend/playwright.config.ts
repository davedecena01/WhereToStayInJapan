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
