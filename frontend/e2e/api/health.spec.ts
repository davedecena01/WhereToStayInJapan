import { test, expect } from '@playwright/test';
import { API_URL } from '../support/test-constants';

test.describe('GET /api/health', () => {
  test('returns 200 with healthy status', async ({ request }) => {
    const res = await request.get(`${API_URL}/api/health`);

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.status).toBe('healthy');
    expect(body.db).toBe('connected');
  });
});
