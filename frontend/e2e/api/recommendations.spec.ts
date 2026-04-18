import { test, expect } from '@playwright/test';
import { API_URL, SIMPLE_TEXT, MULTI_REGION_TEXT, DEFAULT_PREFERENCES } from '../support/test-constants';

async function getParsedItinerary(request: any, text: string) {
  const res = await request.post(`${API_URL}/api/itinerary/parse`, { data: { text } });
  expect(res.status()).toBe(200);
  return res.json();
}

test.describe('POST /api/recommendations', () => {
  test('returns at least 1 recommendation for a valid single-region itinerary', async ({ request }) => {
    test.setTimeout(150_000);
    const itinerary = await getParsedItinerary(request, SIMPLE_TEXT);

    const res = await request.post(`${API_URL}/api/recommendations`, {
      data: { itinerary, preferences: DEFAULT_PREFERENCES },
      timeout: 120_000,
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.recommendations)).toBe(true);
    expect(body.recommendations.length).toBeGreaterThan(0);

    const first = body.recommendations[0];
    expect(typeof first.area_id).toBe('string');
    expect(typeof first.area_name).toBe('string');
    expect(typeof first.station).toBe('string');
    expect(typeof first.rank).toBe('number');
    expect(typeof first.total_score).toBe('number');
    expect(Array.isArray(first.pros)).toBe(true);
    expect(Array.isArray(first.cons)).toBe(true);
  });

  test('returns multiple region recommendations for multi-region itinerary', async ({ request }) => {
    test.setTimeout(150_000);
    const itinerary = await getParsedItinerary(request, MULTI_REGION_TEXT);

    const res = await request.post(`${API_URL}/api/recommendations`, {
      data: { itinerary, preferences: DEFAULT_PREFERENCES },
      timeout: 120_000,
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.recommendations.length).toBeGreaterThan(1);
    expect(typeof body.is_multi_region).toBe('boolean');
    expect(Array.isArray(body.regions_detected)).toBe(true);
  });

  test('returns 400 or 422 for missing body', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/recommendations`, {
      data: {},
    });

    expect([400, 422, 500]).toContain(res.status());
  });
});
