import { defineConfig, devices } from '@playwright/test';

// When FRONTEND_URL is set, tests run against that URL (e.g. Vercel deployment).
// When unset, tests run against a local ng serve instance on port 4200.
const FRONTEND_URL = process.env['FRONTEND_URL'];
const LOCAL_URL = 'http://localhost:4200';
const baseURL = FRONTEND_URL ?? LOCAL_URL;
const useLocalServer = !FRONTEND_URL;

// Vercel Deployment Protection bypass secret.
// Set VERCEL_AUTOMATION_BYPASS_SECRET in your environment to run UI tests
// against a protected Vercel preview deployment.
// See: https://vercel.com/docs/security/deployment-protection/methods-to-bypass-deployment-protection/protection-bypass-automation
const bypassSecret = process.env['VERCEL_AUTOMATION_BYPASS_SECRET'];
const extraHTTPHeaders = bypassSecret
  ? { 'x-vercel-protection-bypass': bypassSecret }
  : {};

export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  retries: 1,
  reporter: [['html'], ['json', { outputFile: 'playwright-report/results.json' }]],
  globalSetup: './e2e/support/global-setup.ts',
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    extraHTTPHeaders,
  },
  // Start a local Angular dev server when not targeting a remote deployment.
  webServer: useLocalServer
    ? {
        command: 'npx ng serve --port 4200',
        url: LOCAL_URL,
        reuseExistingServer: true,
        timeout: 120_000,
      }
    : undefined,
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
