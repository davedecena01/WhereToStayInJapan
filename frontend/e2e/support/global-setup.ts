import { request } from '@playwright/test';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';
import { API_URL, SIMPLE_TEXT, DEFAULT_PREFERENCES } from './test-constants';

export default async function globalSetup(): Promise<void> {
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

    if (areaId) {
      const stateDir = join(__dirname, '../.state');
      mkdirSync(stateDir, { recursive: true });
      writeFileSync(join(stateDir, 'area-id.json'), JSON.stringify({ area_id: areaId }));
      console.log(`[global-setup] Seeded area_id: ${areaId}`);
    }
  } catch (err) {
    console.warn('[global-setup] Error fetching area_id:', err);
  } finally {
    await context.dispose();
  }
}
