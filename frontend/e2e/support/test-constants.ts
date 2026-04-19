import { readFileSync } from 'fs';
import { join } from 'path';

export const FRONTEND_URL =
  process.env['FRONTEND_URL'] ??
  'https://where-to-stay-in-japan-r03l6apz7-davedecena01s-projects.vercel.app';

export const API_URL =
  process.env['API_URL'] ??
  'https://wheretostayinjapan-production.up.railway.app';

export const SIMPLE_TEXT =
  'Day 1-3: Tokyo (Shinjuku, Shibuya). Day 4: Nikko.';

export const MULTI_REGION_TEXT =
  'Days 1-3: Tokyo (Shinjuku). Days 4-6: Kyoto (Gion, Arashiyama). Day 7: Osaka (Dotonbori).';

export const MESSY_TEXT =
  'tokyo first maybe 3 days shinjuku area then kyoto 2 nites last day osaka before flying home';

export const DEFAULT_PREFERENCES = {
  check_in: null,
  check_out: null,
  travelers: 2,
  budget_tier: 'mid',
  preferred_atmosphere: [],
  avoid_long_walking: false,
  must_be_near_station: null,
};

export function getSeededAreaId(): string {
  try {
    const statePath = join(__dirname, '../.state/area-id.json');
    const data = JSON.parse(readFileSync(statePath, 'utf-8'));
    return data.area_id as string;
  } catch {
    return '00000000-0000-0000-0000-000000000000';
  }
}
