// TypeScript interfaces matching backend DTOs exactly (see docs/technical/api-contracts.md)
// Backend uses SnakeCaseLower serialization — all fields are snake_case on the wire.

export interface Destination {
  name: string;
  city: string | null;
  region: string | null;
  day_number: number | null;
  activity_type: string | null;
  lat: number | null;
  lng: number | null;
  is_ambiguous: boolean;
}

export interface ParsedItinerary {
  destinations: Destination[];
  start_date: string | null;
  end_date: string | null;
  raw_text: string | null;
  parsing_confidence: 'high' | 'low';
  clarification_needed: boolean;
  is_multi_region: boolean;
  regions_detected: string[];
}

export type BudgetTier = 'budget' | 'mid' | 'luxury';
export type AtmosphereType = 'nightlife' | 'family_friendly' | 'quiet' | 'shopping' | 'historic' | 'modern';

export interface UserPreferences {
  check_in: string;
  check_out: string;
  travelers: number;
  budget_tier: BudgetTier;
  preferred_atmosphere: AtmosphereType[];
  avoid_long_walking: boolean;
  must_be_near_station: boolean | null;
}

// ── Recommendation models ──────────────────────────────────────────────────

export interface ScoreBreakdown {
  travel_time: number;
  cost: number;
  station_proximity: number;
  food_access: number;
  shopping: number;
}

export interface FoodItem {
  name: string;
  cuisine_type: string;
  address: string | null;
  lat: number | null;
  lng: number | null;
  notes: string | null;
  is_featured: boolean;
}

export interface AttractionItem {
  name: string;
  category: string;
  walk_minutes: number | null;
  notes: string | null;
}

export interface HotelItem {
  hotel_id: string;
  name: string;
  thumbnail_url: string | null;
  price_per_night_jpy: number;
  review_score: number | null;
  deep_link_url: string;
  distance_to_station_km: number | null;
}

export interface StayAreaRecommendation {
  area_id: string;
  area_name: string;
  city: string;
  region: string;
  station: string;
  rank: number;
  total_score: number;
  score_breakdown: ScoreBreakdown;
  avg_travel_time_minutes: number | null;
  avg_hotel_price_jpy: number;
  explanation: string | null;
  pros: string[];
  cons: string[];
  featured_food: FoodItem[];
  featured_attractions: AttractionItem[];
  hotel_preview: HotelItem[];
  hotels_available: boolean;
}

export interface RecommendationResult {
  recommendations: StayAreaRecommendation[];
  is_multi_region: boolean;
  regions_detected: string[];
  multi_region_warning: string | null;
}

export interface RecommendationRequest {
  itinerary: ParsedItinerary;
  preferences: UserPreferences;
}

export interface ApiError {
  type: string;
  title: string;
  status: number;
  detail: string;
  code: string;
  retryable: boolean;
}

// ── Chat models ────────────────────────────────────────────────────────────

export interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
  timestamp: Date;
  updatedItinerary?: ParsedItinerary;
}

/** Matches the backend ChatResponseDto (snake_case on the wire via SnakeCaseLower policy) */
export interface ChatResponse {
  message: string;
  updated_itinerary: ParsedItinerary | null;
  has_itinerary_update: boolean;
}

/**
 * Chat sends/receives the same ParsedItineraryDto shape as the parse endpoint.
 * Type alias avoids conversion layers between chat and store.
 */
export type ChatItinerary = ParsedItinerary;

// ── Hotel search models ────────────────────────────────────────────────────

export interface HotelSearchResult {
  hotels: HotelItem[];
  total: number;
  page: number;
  page_size: number;
  has_more: boolean;
  provider: string;
}

export interface HotelClickRequest {
  session_id: string;
  hotel_id: string;
  area_id: string;
}
