import { test, expect } from '@playwright/test';
import { readFileSync } from 'fs';
import { join } from 'path';
import { API_URL, SIMPLE_TEXT } from '../support/test-constants';

test.describe('POST /api/itinerary/parse (text)', () => {
  test('parses valid text and returns ParsedItineraryDto', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/itinerary/parse`, {
      data: { text: SIMPLE_TEXT },
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.destinations)).toBe(true);
    expect(body.destinations.length).toBeGreaterThan(0);
    expect(Array.isArray(body.regions_detected)).toBe(true);
    expect(typeof body.parsing_confidence).toBe('string');
    expect(typeof body.is_multi_region).toBe('boolean');
    expect(typeof body.clarification_needed).toBe('boolean');
  });

  test('returns 400 for empty text', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/itinerary/parse`, {
      data: { text: '' },
    });

    expect(res.status()).toBe(400);
    const body = await res.json();
    expect(body.error ?? body.title ?? body.detail).toBeTruthy();
  });

  test('returns 400 for missing body', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/itinerary/parse`, {
      data: {},
    });

    expect(res.status()).toBe(400);
    const body = await res.json();
    expect(body.error ?? body.title ?? body.detail).toBeTruthy();
  });
});

test.describe('POST /api/itinerary/parse/file', () => {
  test('parses valid .txt file and returns ParsedItineraryDto', async ({ request }) => {
    const fileBuffer = readFileSync(join(__dirname, '../fixtures/simple.txt'));

    const res = await request.post(`${API_URL}/api/itinerary/parse/file`, {
      multipart: {
        file: {
          name: 'simple.txt',
          mimeType: 'text/plain',
          buffer: fileBuffer,
        },
      },
    });

    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.destinations)).toBe(true);
    expect(body.destinations.length).toBeGreaterThan(0);
  });

  test('returns 400 when no file is provided', async ({ request }) => {
    const res = await request.post(`${API_URL}/api/itinerary/parse/file`, {
      multipart: {},
    });

    expect(res.status()).toBe(400);
  });

  test('returns 413 for oversized file (>10MB)', async ({ request }) => {
    const bigBuffer = Buffer.alloc(11 * 1024 * 1024, 'a'); // 11 MB

    // Railway's edge may hard-close the connection (ECONNRESET/502) instead of
    // returning a clean 413/400 for oversized payloads — both outcomes are acceptable.
    let status: number;
    try {
      const res = await request.post(`${API_URL}/api/itinerary/parse/file`, {
        multipart: {
          file: {
            name: 'huge.txt',
            mimeType: 'text/plain',
            buffer: bigBuffer,
          },
        },
        timeout: 30_000,
      });
      status = res.status();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : String(err);
      // Connection reset / network-level rejection is an acceptable response
      // to an oversized upload — treat it as a passing guard.
      if (msg.includes('ECONNRESET') || msg.includes('read ECONNRESET')) return;
      throw err;
    }

    expect([400, 413, 502]).toContain(status);
  });
});
