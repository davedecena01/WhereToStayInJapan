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
  updatedItinerary?: ChatItinerary;
}

/** Matches the backend ChatResponseDto (camelCase on the wire) */
export interface ChatResponse {
  message: string;
  updatedItinerary: ChatItinerary | null;
  hasItineraryUpdate: boolean;
}

/** Matches ParsedItineraryDto serialized to camelCase */
export interface ChatItinerary {
  destinations: ChatDestination[];
  regionsDetected: string[];
  isMultiRegion: boolean;
  startDate: string | null;
  endDate: string | null;
  parsingConfidence: string;
  clarificationNeeded: boolean;
  rawText: string | null;
}

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

export interface ChatDestination {
  name: string;
  city: string | null;
  region: string | null;
  dayNumber: number | null;
  activityType: string | null;
  lat: number | null;
  lng: number | null;
  isAmbiguous: boolean;
}
