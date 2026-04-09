# API Contracts — Where To Stay In Japan

Base URL: `https://api.wheretostayinjapan.com` (production) / `http://localhost:5000` (local)
All requests/responses: `Content-Type: application/json` unless noted.
All dates: ISO 8601 strings (`YYYY-MM-DD`).
All prices: integers in JPY (¥).
Error format: RFC 7807 `ProblemDetails`.

---

## Shared Type Definitions

```typescript
// Core domain types — matches C# DTOs exactly

interface DateRange {
  start: string;       // "YYYY-MM-DD"
  end: string;         // "YYYY-MM-DD"
}

interface GeoPoint {
  lat: number;
  lng: number;
}

interface Destination {
  name: string;             // normalized name, e.g., "Senso-ji Temple"
  raw_name: string;         // as extracted from itinerary, e.g., "sensoji"
  city: string | null;      // e.g., "Tokyo"
  region: string | null;    // e.g., "Kanto"
  day_number: number | null;  // null if not date-grouped
  activity_type: string | null; // "sightseeing" | "food" | "shopping" | "transport" | null
  geo_point: GeoPoint | null;   // null if geocoding failed
}

interface ParsedItinerary {
  destinations: Destination[];
  travel_dates: DateRange | null;   // null if no dates found in itinerary
  raw_text_preview: string;         // first 500 chars of input text
  parsing_confidence: 'high' | 'low';
  clarification_needed: boolean;
  is_multi_region: boolean;
  regions_detected: string[];       // e.g., ["Kanto", "Kansai"]
  parsed_by: 'ai' | 'rules_only';   // which adapter was used
}

interface UserPreferences {
  checkin: string;              // "YYYY-MM-DD"
  checkout: string;             // "YYYY-MM-DD"
  travelers: number;            // 1–10
  budget_tier: 'budget' | 'mid' | 'luxury';
  hotel_types: HotelType[];
  avoid_long_walking: boolean;
  must_be_near_station: string | null;
  preferred_atmosphere: AtmosphereType[];
}

type HotelType = 'business' | 'ryokan' | 'capsule' | 'boutique' | 'resort' | 'hostel';
type AtmosphereType = 'nightlife' | 'family_friendly' | 'quiet' | 'shopping' | 'historic' | 'modern';

interface TravelTimeSummary {
  destination_name: string;
  destination_raw: string;
  estimated_minutes: number | null;  // null if routing failed
  is_estimated: boolean;             // true if based on seeded fallback, not live OSRM
}

interface ScoreBreakdown {
  total_score: number;             // 0.0–1.0
  travel_time_score: number;       // 0.0–1.0 (weight: 0.40)
  cost_score: number;              // 0.0–1.0 (weight: 0.30)
  station_proximity_score: number; // 0.0–1.0 (weight: 0.15)
  food_access_score: number;       // 0.0–1.0 (weight: 0.10)
  shopping_score: number;          // 0.0–1.0 (weight: 0.05)
  avg_travel_time_minutes: number | null;
  avg_hotel_price_jpy: number | null;
}

interface FoodItem {
  name: string;
  cuisine_type: string;
  address: string | null;
  geo_point: GeoPoint | null;
  notes: string | null;
  source: 'curated' | 'ai_generated';
  is_featured: boolean;
}

interface AttractionItem {
  name: string;
  category: string;
  address: string | null;
  geo_point: GeoPoint | null;
  walk_minutes: number | null;
  notes: string | null;
}

interface HotelItem {
  provider_id: string;           // Rakuten hotel ID
  name: string;
  area_name: string;
  nearest_station: string;
  geo_point: GeoPoint;
  price_per_night_jpy: number;
  rating: number;                // 1.0–5.0
  review_count: number;
  image_url: string | null;
  deep_link_url: string;         // Rakuten booking URL with dates pre-filled
  amenities: string[];
  provider: string;              // "rakuten" | "mock"
}

interface RecommendationResult {
  area_id: string;               // UUID
  area_name: string;             // e.g., "Shinjuku"
  station: string;               // e.g., "Shinjuku Station"
  city: string;
  region: string;
  rank: number;                  // 1 = best
  score_breakdown: ScoreBreakdown;
  explanation: string | null;    // AI-generated, null if AI unavailable
  travel_time_summary: TravelTimeSummary[];
  pros: string[];                // 2–4 items
  cons: string[];                // 1–3 items
  food_suggestions: FoodItem[];  // 5+ items
  nearby_attractions: AttractionItem[];
  hotel_preview: HotelItem[];    // 3 items max (preview), empty if hotels unavailable
  hotels_available: boolean;     // false if hotel API failed
  ai_used: boolean;
}

interface ApiError {
  type: string;
  title: string;
  status: number;
  detail: string;
  code: string;           // machine-readable: "INVALID_FILE", "AI_UNAVAILABLE", etc.
  retryable: boolean;
}
```

---

## Endpoints

---

### `POST /api/itinerary/parse`

Parses an uploaded file or pasted text into a structured `ParsedItinerary`.

**Request (multipart/form-data — file upload):**
```
Content-Type: multipart/form-data

file: <binary>         -- PDF, DOCX, or TXT file, max 10MB
```

**Request (application/json — pasted text):**
```json
{
  "text": "Day 1: Arrive Tokyo. Visit Senso-ji temple. Lunch in Asakusa.\nDay 2: Shibuya crossing, Harajuku, Meiji Shrine..."
}
```

**Response `200 OK`:**
```json
{
  "destinations": [
    {
      "name": "Senso-ji Temple",
      "raw_name": "Senso-ji temple",
      "city": "Tokyo",
      "region": "Kanto",
      "day_number": 1,
      "activity_type": "sightseeing",
      "geo_point": { "lat": 35.7148, "lng": 139.7967 }
    },
    {
      "name": "Asakusa",
      "raw_name": "Asakusa",
      "city": "Tokyo",
      "region": "Kanto",
      "day_number": 1,
      "activity_type": "food",
      "geo_point": { "lat": 35.7116, "lng": 139.7967 }
    }
  ],
  "travel_dates": { "start": "2025-10-01", "end": "2025-10-08" },
  "raw_text_preview": "Day 1: Arrive Tokyo. Visit Senso-ji temple...",
  "parsing_confidence": "high",
  "clarification_needed": false,
  "is_multi_region": false,
  "regions_detected": ["Kanto"],
  "parsed_by": "ai"
}
```

**Response (AI unavailable, fallback used) `200 OK` with degraded data:**
```json
{
  "destinations": [...],
  "parsing_confidence": "low",
  "clarification_needed": true,
  "parsed_by": "rules_only"
}
```

**Error responses:**

| Status | Code | Reason |
|---|---|---|
| 400 | `INVALID_REQUEST` | Neither file nor text provided |
| 413 | `FILE_TOO_LARGE` | File exceeds 10MB |
| 415 | `UNSUPPORTED_FILE_TYPE` | File is not PDF, DOCX, or TXT |
| 422 | `TEXT_EXTRACTION_FAILED` | File had no extractable text (image-only PDF, corrupt file) |
| 503 | `AI_UNAVAILABLE` | This should not happen — fallback always runs; this code reserved |

---

### `POST /api/recommendations`

Runs the recommendation engine on a parsed itinerary. Core endpoint.

**Request:**
```json
{
  "itinerary": { /* ParsedItinerary from /api/itinerary/parse */ },
  "preferences": {
    "checkin": "2025-10-01",
    "checkout": "2025-10-08",
    "travelers": 2,
    "budget_tier": "mid",
    "hotel_types": ["business", "boutique"],
    "avoid_long_walking": false,
    "must_be_near_station": null,
    "preferred_atmosphere": ["quiet", "historic"]
  }
}
```

**Response `200 OK`:**
```json
[
  {
    "area_id": "550e8400-e29b-41d4-a716-446655440000",
    "area_name": "Asakusa",
    "station": "Asakusa Station",
    "city": "Tokyo",
    "region": "Kanto",
    "rank": 1,
    "score_breakdown": {
      "total_score": 0.82,
      "travel_time_score": 0.91,
      "cost_score": 0.78,
      "station_proximity_score": 0.85,
      "food_access_score": 0.90,
      "shopping_score": 0.70,
      "avg_travel_time_minutes": 18,
      "avg_hotel_price_jpy": 11500
    },
    "explanation": "Asakusa offers the shortest average travel time to your itinerary destinations at 18 minutes. Its historic shitamachi atmosphere matches your preference for quiet and historic areas. Budget-friendly hotels are plentiful near Asakusa Station.",
    "travel_time_summary": [
      { "destination_name": "Senso-ji Temple", "destination_raw": "Senso-ji temple", "estimated_minutes": 5, "is_estimated": false },
      { "destination_name": "Shibuya", "destination_raw": "Shibuya", "estimated_minutes": 28, "is_estimated": false }
    ],
    "pros": ["Shortest travel time to most of your destinations", "Rich in street food and local dining", "Historic atmosphere with Nakamise shopping street"],
    "cons": ["Less nightlife compared to Shinjuku or Shibuya", "Fewer large department stores"],
    "food_suggestions": [
      { "name": "Asakusa Imahan", "cuisine_type": "sukiyaki", "address": "1-3-4 Nishi-Asakusa, Taito, Tokyo", "geo_point": {"lat": 35.7122, "lng": 139.7934}, "notes": "Historic beef hot pot restaurant, established 1895", "source": "curated", "is_featured": true }
    ],
    "nearby_attractions": [
      { "name": "Senso-ji Temple", "category": "temple", "address": "2-3-1 Asakusa, Taito, Tokyo", "geo_point": {"lat": 35.7148, "lng": 139.7967}, "walk_minutes": 7, "notes": "Tokyo's oldest and most famous temple" }
    ],
    "hotel_preview": [
      { "provider_id": "136197", "name": "Asakusa View Hotel", "area_name": "Asakusa", "nearest_station": "Asakusa Station", "geo_point": {"lat": 35.7141, "lng": 139.7981}, "price_per_night_jpy": 13200, "rating": 4.1, "review_count": 3420, "image_url": "https://...", "deep_link_url": "https://travel.rakuten.co.jp/hotel/136197/?f_tedate=2025-10-01&f_sydate=2025-10-08", "amenities": ["wifi", "restaurant", "concierge"], "provider": "rakuten" }
    ],
    "hotels_available": true,
    "ai_used": true
  }
]
```

**Partial success (hotels unavailable) `206 Partial Content`:**
```json
[
  {
    ...
    "hotel_preview": [],
    "hotels_available": false,
    ...
  }
]
```

**Error responses:**

| Status | Code | Reason |
|---|---|---|
| 400 | `INVALID_ITINERARY` | `itinerary.destinations` is empty |
| 400 | `INVALID_PREFERENCES` | Missing required fields (checkin, checkout, travelers, budget_tier) |
| 400 | `INVALID_DATES` | checkout <= checkin, or dates in the past |
| 422 | `NO_COVERAGE` | No seeded station areas match the detected regions |

---

### `GET /api/hotels`

Fetches hotels for a specific area. Used for the area detail page (paginated).

**Query parameters:**
```
area_id      UUID      required
checkin      string    required  "YYYY-MM-DD"
checkout     string    required  "YYYY-MM-DD"
budget_tier  string    required  "budget" | "mid" | "luxury"
page         integer   optional  default: 1
page_size    integer   optional  default: 10, max: 50
```

**Response `200 OK`:**
```json
{
  "hotels": [ /* HotelItem[] */ ],
  "total": 48,
  "page": 1,
  "page_size": 10,
  "provider": "rakuten",
  "deep_link_base": "https://travel.rakuten.co.jp"
}
```

**Response (provider unavailable) `503 Service Unavailable`:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Hotel provider unavailable",
  "status": 503,
  "detail": "Rakuten Travel API is temporarily unavailable. Please try again later.",
  "code": "HOTEL_PROVIDER_UNAVAILABLE",
  "retryable": true
}
```

---

### `GET /api/areas/{id}/food`

Returns food suggestions for an area (curated first, AI-supplemented).

**Path parameter:** `id` — UUID of station area

**Response `200 OK`:**
```json
{
  "food": [ /* FoodItem[] — minimum 5 items */ ],
  "source": "mixed",  // "curated" | "ai" | "mixed" | "unavailable"
  "area_name": "Asakusa"
}
```

---

### `GET /api/areas/{id}/attractions`

Returns nearby attractions for an area.

**Response `200 OK`:**
```json
{
  "attractions": [ /* AttractionItem[] */ ],
  "area_name": "Asakusa"
}
```

---

### `POST /api/chat`

AI chat for itinerary refinement or recommendation questions.

**Request:**
```json
{
  "session_id": "uuid-v4-from-localstorage",
  "message": "Actually I want to visit Nara on day 3, not stay in Osaka",
  "context": {
    "itinerary": { /* ParsedItinerary — optional, provides context */ }
  }
}
```

**Response `200 OK`:**
```json
{
  "reply": "Got it! I've updated day 3 to include Nara (Kintetsu-Nara Station area). Would you like me to also check if Nara fits well with your Kyoto plans on day 4?",
  "suggested_itinerary_update": { /* ParsedItinerary — only present if AI suggests changes */ }
}
```

**Response (AI unavailable) `503`:**
```json
{
  "code": "AI_UNAVAILABLE",
  "title": "AI chat is temporarily unavailable",
  "retryable": true
}
```

---

### `POST /api/analytics/hotel-click`

Fire-and-forget hotel click tracking.

**Request:**
```json
{
  "session_id": "uuid-v4",
  "hotel_id": "136197",
  "area_id": "550e8400-e29b-41d4-a716-446655440000",
  "area_name": "Asakusa"
}
```

**Response `204 No Content`**

No error response on failure — client does not wait for this.

---

### `GET /api/health`

Provider availability check. Used by Render/Railway health check probes.

**Response `200 OK`:**
```json
{
  "status": "healthy",
  "providers": {
    "database": true,
    "ai": true,
    "maps": true,
    "hotels": true
  },
  "mode": {
    "ai": "gemini",        // "gemini" | "mock" | "rules_only"
    "hotels": "rakuten",   // "rakuten" | "mock"
    "maps": "nominatim"    // "nominatim" | "mock"
  }
}
```

**Response (degraded) `200 OK`:**
```json
{
  "status": "degraded",
  "providers": {
    "database": true,
    "ai": false,
    "maps": true,
    "hotels": false
  }
}
```

Health returns `200` even when degraded (prevents Render from restarting a healthy instance). Return `503` only if `database: false`.

---

## Error Response Format (RFC 7807 ProblemDetails)

All error responses follow this structure:
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Human-readable error title",
  "status": 400,
  "detail": "Specific explanation of what went wrong",
  "code": "MACHINE_READABLE_CODE",
  "retryable": false
}
```

**All error codes:**

| Code | HTTP Status | Description |
|---|---|---|
| `INVALID_REQUEST` | 400 | Malformed request body |
| `INVALID_ITINERARY` | 400 | Empty destinations list |
| `INVALID_PREFERENCES` | 400 | Missing required preference fields |
| `INVALID_DATES` | 400 | Check-in/check-out date validation failure |
| `FILE_TOO_LARGE` | 413 | File exceeds 10MB |
| `UNSUPPORTED_FILE_TYPE` | 415 | MIME type not accepted |
| `TEXT_EXTRACTION_FAILED` | 422 | No text extractable from file |
| `NO_COVERAGE` | 422 | No seeded areas for detected regions |
| `AI_UNAVAILABLE` | 503 | AI provider down (chat endpoint only) |
| `HOTEL_PROVIDER_UNAVAILABLE` | 503 | Hotel API down (hotels endpoint) |
| `INTERNAL_ERROR` | 500 | Unhandled server error |

---

## CORS Configuration

```
Allowed Origins: configured via CORS__ALLOWEDORIGINS env var (Vercel frontend URL)
Allowed Methods: GET, POST, OPTIONS
Allowed Headers: Content-Type, Authorization (for future auth)
Max Age: 600 seconds
```

---

## Rate Limiting

Applied at the API gateway level (or in-memory middleware for V1):
- `POST /api/recommendations`: 10 requests per IP per hour
- `POST /api/itinerary/parse`: 20 requests per IP per hour
- `POST /api/chat`: 30 messages per session per hour
- All other endpoints: no limit in V1

Rate limit exceeded: `429 Too Many Requests` with `Retry-After` header.
