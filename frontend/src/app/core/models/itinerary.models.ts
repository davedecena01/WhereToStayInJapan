// TypeScript interfaces matching backend DTOs exactly (see docs/technical/api-contracts.md)

export interface GeoPoint {
  lat: number;
  lng: number;
}

export interface DateRange {
  start: string; // "YYYY-MM-DD"
  end: string;
}

export interface Destination {
  name: string;
  raw_name: string;
  city: string | null;
  region: string | null;
  day_number: number | null;
  activity_type: string | null;
  geo_point: GeoPoint | null;
}

export interface ParsedItinerary {
  destinations: Destination[];
  travel_dates: DateRange | null;
  raw_text_preview: string;
  parsing_confidence: 'high' | 'low';
  clarification_needed: boolean;
  is_multi_region: boolean;
  regions_detected: string[];
  parsed_by: 'ai' | 'rules_only';
}

export type BudgetTier = 'budget' | 'mid' | 'luxury';
export type HotelType = 'business' | 'ryokan' | 'capsule' | 'boutique' | 'resort' | 'hostel';
export type AtmosphereType = 'nightlife' | 'family_friendly' | 'quiet' | 'shopping' | 'historic' | 'modern';

export interface UserPreferences {
  checkin: string;
  checkout: string;
  travelers: number;
  budget_tier: BudgetTier;
  hotel_types: HotelType[];
  avoid_long_walking: boolean;
  must_be_near_station: string | null;
  preferred_atmosphere: AtmosphereType[];
}

export interface ApiError {
  type: string;
  title: string;
  status: number;
  detail: string;
  code: string;
  retryable: boolean;
}
