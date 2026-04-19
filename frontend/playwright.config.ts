import { defineConfig, devices } from '@playwright/test';
import { join } from 'path';

// When FRONTEND_URL is set, tests run against that URL (e.g. Vercel deployment).
// When unset, tests run against a local ng serve instance on port 4200.
const FRONTEND_URL = process.env['FRONTEND_URL'];
const LOCAL_URL = 'http://localhost:4200';
const baseURL = FRONTEND_URL ?? LOCAL_URL;
const useLocalServer = !FRONTEND_URL;

// Vercel bypass auth is established in global-setup via cookie (storageState).
// The cookie approach is used instead of extraHTTPHeaders so that cross-origin
// requests (Railway API, Google Fonts) are not affected by the bypass header.
const VERCEL_AUTH_PATH = join(__dirname, 'e2e/.state/vercel-auth.json');

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
    storageState: VERCEL_AUTH_PATH,
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
