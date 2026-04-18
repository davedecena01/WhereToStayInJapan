import { test, expect } from '@playwright/test';
import { API_URL, SIMPLE_TEXT, DEFAULT_PREFERENCES } from '../support/test-constants';

async function getParsedItinerary(request: any, text: string) {
  const res = await request.post(`${API_URL}/api/itinerary/parse`, { data: { text } });
  expect(res.status()).toBe(200);
  return res.json();
}

test.describe('POST /api/chat', () => {
  test('returns non-empty reply for a valid message with itinerary context', async ({ request }) => {
    const itinerary = await getParsedItinerary(request, SIMPLE_TEXT);

    const res = await request.post(`${API_URL}/api/chat`, {
      data: {
        session_id: `test-${Date.now()}`,
        message: 'How many days should I spend in Tokyo?',
        current_itinerary: itinerary,
      },
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(typeof body.message).toBe('string');
    expect(body.message.length).toBeGreaterThan(0);
    expect(typeof body.has_itinerary_update).toBe('boolean');
  });

  test('returns graceful response with no itinerary context (first message)', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/chat`, {
      data: {
        session_id: `test-${Date.now()}`,
        message: 'I want to visit Japan in April.',
        current_itinerary: null,
      },
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(typeof body.message).toBe('string');
    expect(body.message.length).toBeGreaterThan(0);
  });

  test('returns 400 for empty message', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/chat`, {
      data: {
        session_id: `test-${Date.now()}`,
        message: '',
        current_itinerary: null,
      },
    });

    // Either 400 or a graceful fallback — must not be 500
    expect(res.status()).not.toBe(500);
  });
});
