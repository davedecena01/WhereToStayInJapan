import { request, chromium } from '@playwright/test';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';
import { API_URL, SIMPLE_TEXT, DEFAULT_PREFERENCES } from './test-constants';

const STATE_DIR = join(__dirname, '../.state');
export const VERCEL_AUTH_PATH = join(STATE_DIR, 'vercel-auth.json');

export default async function globalSetup(): Promise<void> {
  mkdirSync(STATE_DIR, { recursive: true });
  await setupVercelAuth();
  await seedAreaId();
}

async function setupVercelAuth(): Promise<void> {
  const bypassSecret = process.env['VERCEL_AUTOMATION_BYPASS_SECRET'];
  const frontendUrl = process.env['FRONTEND_URL'];

  if (bypassSecret && frontendUrl) {
    const browser = await chromium.launch();
    const ctx = await browser.newContext();
    const page = await ctx.newPage();
    await page.goto(`${frontendUrl}/?x-vercel-protection-bypass=${bypassSecret}`);
    await ctx.storageState({ path: VERCEL_AUTH_PATH });
    await browser.close();
    console.log('[global-setup] Vercel bypass cookie saved');
  } else {
    writeFileSync(VERCEL_AUTH_PATH, JSON.stringify({ cookies: [], origins: [] }));
  }
}

async function seedAreaId(): Promise<void> {
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

    if (!areaId) {
      console.warn('[global-setup] No area_id in recommendations response — hotel tests will use fallback');
      return;
    }

    writeFileSync(join(STATE_DIR, 'area-id.json'), JSON.stringify({ area_id: areaId }));
    console.log(`[global-setup] Seeded area_id: ${areaId}`);
  } catch (err) {
    const errMsg = err instanceof Error ? err.message : String(err);
    console.warn(`[global-setup] Error fetching area_id (${errMsg}) — hotel tests will use fallback`);
  } finally {
    await context.dispose();
  }
}
