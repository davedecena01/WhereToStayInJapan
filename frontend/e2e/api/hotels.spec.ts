import { test, expect } from '@playwright/test';
import { API_URL, getSeededAreaId } from '../support/test-constants';

test.describe('GET /api/hotels', () => {
  test('returns HotelSearchResultDto for valid area_id', async ({ request }) => {
    const areaId = getSeededAreaId();
    const res = await request.get(`${API_URL}/api/hotels?area_id=${areaId}`);

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.hotels)).toBe(true);
    expect(typeof body.total).toBe('number');
    expect(typeof body.page).toBe('number');
    expect(typeof body.has_more).toBe('boolean');
    expect(typeof body.provider).toBe('string');
  });

  test('accepts budget_tier filter', async ({ request }) => {
    const areaId = getSeededAreaId();
    const res = await request.get(`${API_URL}/api/hotels?area_id=${areaId}&budget_tier=budget`);

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.hotels)).toBe(true);
  });

  test('returns 400 for invalid area_id (not a GUID)', async ({ request }) => {
    const res = await request.get(`${API_URL}/api/hotels?area_id=not-a-guid`);

    expect(res.status()).toBe(400);
    // Must not 500
    expect(res.status()).not.toBe(500);
  });

  test('returns 200 with empty results when area_id is missing', async ({ request }) => {
    // Live API returns 200 + empty hotel list when no area_id is provided
    const res = await request.get(`${API_URL}/api/hotels`);

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.hotels)).toBe(true);
  });
});
