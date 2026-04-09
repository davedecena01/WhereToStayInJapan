# Data Model — Where To Stay In Japan

Database: PostgreSQL (Supabase free tier)
ORM: EF Core 8 with Npgsql provider
All primary keys: UUID (Guid in C#)
All timestamps: TIMESTAMPTZ (UTC)

---

## Tables

### `station_areas`
Seeded reference data. The candidate pool for the recommendation engine.
~15 records for MVP, expandable to ~100+ in Phase 2.

```sql
CREATE TABLE station_areas (
  id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  city                 VARCHAR(100) NOT NULL,        -- "Tokyo", "Osaka", "Kyoto", "Hiroshima"
  region               VARCHAR(100) NOT NULL,        -- "Kanto", "Kansai", "Chugoku"
  area_name            VARCHAR(200) NOT NULL,        -- "Shinjuku", "Namba", "Gion"
  station              VARCHAR(200) NOT NULL,        -- "Shinjuku Station", "Namba Station"
  lat                  DECIMAL(9,6) NOT NULL,        -- area centroid latitude
  lng                  DECIMAL(9,6) NOT NULL,        -- area centroid longitude
  station_lat          DECIMAL(9,6) NOT NULL,        -- station entrance latitude
  station_lng          DECIMAL(9,6) NOT NULL,        -- station entrance longitude
  description          TEXT,                         -- 2-3 sentence area summary for seeding
  avg_hotel_price_jpy  INTEGER NOT NULL DEFAULT 0,   -- seeded average nightly rate in JPY
  food_access_score    DECIMAL(3,2) NOT NULL DEFAULT 0.5,  -- 0.00–1.00
  shopping_score       DECIMAL(3,2) NOT NULL DEFAULT 0.5,  -- 0.00–1.00
  is_active            BOOLEAN NOT NULL DEFAULT true,
  created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_station_areas_region ON station_areas(region);
CREATE INDEX idx_station_areas_city ON station_areas(city);
```

**EF Core entity:** `StationArea`
**Notes:** `food_access_score` and `shopping_score` are manually seeded. `avg_hotel_price_jpy` is seeded from Rakuten data samples and updated periodically.

---

### `curated_food`
Admin-seeded food recommendations. Prioritized over AI-generated suggestions.

```sql
CREATE TABLE curated_food (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  station_area_id  UUID NOT NULL REFERENCES station_areas(id) ON DELETE CASCADE,
  name             VARCHAR(200) NOT NULL,
  cuisine_type     VARCHAR(100) NOT NULL,   -- "ramen", "sushi", "izakaya", "cafe", etc.
  address          TEXT,
  lat              DECIMAL(9,6),
  lng              DECIMAL(9,6),
  notes            TEXT,                   -- brief description or recommendation note
  source           VARCHAR(50) NOT NULL DEFAULT 'admin',  -- 'admin' | 'osm' | 'ai_generated'
  is_featured      BOOLEAN NOT NULL DEFAULT false,
  created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_curated_food_area ON curated_food(station_area_id);
CREATE INDEX idx_curated_food_featured ON curated_food(station_area_id, is_featured);
```

**EF Core entity:** `CuratedFood`
**Seed target:** 5–8 records per seeded station area (75–120 total for MVP seed).

---

### `curated_attractions`
Admin-seeded attractions and nearby destinations.

```sql
CREATE TABLE curated_attractions (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  station_area_id  UUID NOT NULL REFERENCES station_areas(id) ON DELETE CASCADE,
  name             VARCHAR(200) NOT NULL,
  category         VARCHAR(100) NOT NULL,  -- 'temple' | 'shrine' | 'shopping' | 'park' | 'museum' | 'neighborhood' | 'market'
  address          TEXT,
  lat              DECIMAL(9,6),
  lng              DECIMAL(9,6),
  walk_minutes     INTEGER,                -- approximate walking time from station (nullable = unknown)
  notes            TEXT,
  created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_curated_attractions_area ON curated_attractions(station_area_id);
CREATE INDEX idx_curated_attractions_category ON curated_attractions(station_area_id, category);
```

**EF Core entity:** `CuratedAttraction`
**Seed target:** 5–10 records per seeded station area.

---

### `geocode_cache`
Caches Nominatim geocoding responses. Avoids repeat API calls for the same place.

```sql
CREATE TABLE geocode_cache (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  normalized_key  VARCHAR(500) NOT NULL UNIQUE,  -- lowercase, stripped whitespace, e.g. "senso-ji temple tokyo"
  raw_query       TEXT NOT NULL,
  lat             DECIMAL(9,6) NOT NULL,
  lng             DECIMAL(9,6) NOT NULL,
  provider        VARCHAR(50) NOT NULL DEFAULT 'nominatim',
  cached_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  expires_at      TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX idx_geocode_cache_key ON geocode_cache(normalized_key);
```

**TTL:** 90 days.
**EF Core entity:** `GeocodeCache`
**Notes:** `normalized_key` = `place_name.ToLower().Trim().Replace(" ", "-")`. Seeded for all known station areas and their common destination stations.

---

### `routing_cache`
Caches OSRM travel time responses between two geo-points.

```sql
CREATE TABLE routing_cache (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  cache_key       VARCHAR(500) NOT NULL UNIQUE,  -- SHA256("{origin_lat},{origin_lng}:{dest_lat},{dest_lng}:{mode}")
  origin_lat      DECIMAL(9,6) NOT NULL,
  origin_lng      DECIMAL(9,6) NOT NULL,
  dest_lat        DECIMAL(9,6) NOT NULL,
  dest_lng        DECIMAL(9,6) NOT NULL,
  travel_mode     VARCHAR(20) NOT NULL DEFAULT 'driving',  -- 'driving' | 'walking'
  duration_mins   INTEGER NOT NULL,              -- rounded to nearest integer
  distance_km     DECIMAL(6,2) NOT NULL,
  provider        VARCHAR(50) NOT NULL DEFAULT 'osrm',
  cached_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  expires_at      TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX idx_routing_cache_key ON routing_cache(cache_key);
```

**TTL:** 7 days.
**EF Core entity:** `RoutingCache`
**Notes:** Seeded for all station area → common destination station pairs during initial seed run. This means typical recommendations hit cache 90%+ of the time.

---

### `ai_response_cache`
Caches AI provider responses to avoid duplicate API calls for identical inputs.

```sql
CREATE TABLE ai_response_cache (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  input_hash      VARCHAR(64) NOT NULL UNIQUE,   -- SHA256 of normalized prompt input
  prompt_type     VARCHAR(100) NOT NULL,          -- 'parse_itinerary' | 'generate_explanation' | 'suggest_food' | 'suggest_attractions'
  response_json   JSONB NOT NULL,
  provider        VARCHAR(50) NOT NULL,           -- 'gemini' | 'mock' | 'rules_only'
  cached_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  expires_at      TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX idx_ai_response_cache_hash ON ai_response_cache(input_hash);
CREATE INDEX idx_ai_response_cache_type ON ai_response_cache(prompt_type);
```

**TTL:** 24 hours for parse_itinerary; 48 hours for generate_explanation, suggest_food, suggest_attractions.
**EF Core entity:** `AiResponseCache`
**Notes:** `input_hash` = SHA256 of `{prompt_type}:{normalized_input}`. Normalization: lowercase, whitespace collapsed. Prevents duplicate AI calls for the same itinerary submitted twice.

---

### `hotel_search_cache`
Caches hotel search results from Rakuten Travel API.

```sql
CREATE TABLE hotel_search_cache (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  cache_key       VARCHAR(500) NOT NULL UNIQUE,
  area_id         UUID NOT NULL REFERENCES station_areas(id),
  checkin_date    DATE NOT NULL,
  checkout_date   DATE NOT NULL,
  budget_tier     VARCHAR(20) NOT NULL,   -- 'budget' | 'mid' | 'luxury'
  results_json    JSONB NOT NULL,         -- serialized HotelItem[]
  cached_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  expires_at      TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX idx_hotel_search_cache_key ON hotel_search_cache(cache_key);
CREATE INDEX idx_hotel_search_cache_area_dates ON hotel_search_cache(area_id, checkin_date, checkout_date);
```

**Cache key:** `SHA256("{area_id}:{checkin}:{checkout}:{budget_tier}:{page}")`
**TTL:** 30 minutes.
**EF Core entity:** `HotelSearchCache`

---

### `recommendation_logs`
Minimal analytics: one row per recommendation request.

```sql
CREATE TABLE recommendation_logs (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  session_id      VARCHAR(100) NOT NULL,   -- anonymous UUID from localStorage
  input_hash      VARCHAR(64) NOT NULL,    -- SHA256 of normalized itinerary text
  top_areas       TEXT[] NOT NULL,         -- e.g., ["Shinjuku","Shibuya","Asakusa"]
  region_count    INTEGER NOT NULL DEFAULT 1,
  ai_used         BOOLEAN NOT NULL DEFAULT false,
  hotels_fetched  BOOLEAN NOT NULL DEFAULT false,
  duration_ms     INTEGER,                 -- total server-side duration
  created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_recommendation_logs_session ON recommendation_logs(session_id);
CREATE INDEX idx_recommendation_logs_created ON recommendation_logs(created_at DESC);
```

**EF Core entity:** `RecommendationLog`
**Notes:** Written fire-and-forget after response is sent. Never block the recommendation response on this write.

---

### `hotel_click_logs`
Tracks hotel deep-link clicks for affiliate/analytics use.

```sql
CREATE TABLE hotel_click_logs (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  session_id      VARCHAR(100) NOT NULL,
  hotel_id        VARCHAR(200) NOT NULL,   -- Rakuten hotel ID
  area_id         UUID REFERENCES station_areas(id),
  area_name       VARCHAR(200),
  created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_hotel_click_logs_session ON hotel_click_logs(session_id);
CREATE INDEX idx_hotel_click_logs_created ON hotel_click_logs(created_at DESC);
```

**EF Core entity:** `HotelClickLog`
**Notes:** Written by `POST /api/analytics/hotel-click`. Fire-and-forget from frontend. Used in Phase 3 for affiliate commission reporting.

---

## Entity Relationships

```
station_areas (1) ──── (N) curated_food
station_areas (1) ──── (N) curated_attractions
station_areas (1) ──── (N) hotel_search_cache
station_areas (1) ──── (N) hotel_click_logs

-- Cache tables are standalone (no FK to station_areas except hotel_search_cache)
geocode_cache    -- keyed by place name
routing_cache    -- keyed by geo-point pair
ai_response_cache -- keyed by input hash
```

---

## EF Core Notes

**PKs:** All `Guid`, configured with `HasDefaultValueSql("gen_random_uuid()")`.

**Decimal precision:** Configure explicitly to avoid EF Core defaulting to incorrect precision:
```csharp
modelBuilder.Entity<StationArea>()
    .Property(x => x.Lat).HasPrecision(9, 6);
modelBuilder.Entity<StationArea>()
    .Property(x => x.FoodAccessScore).HasPrecision(3, 2);
```

**Dates:** Use `DateOnly` for `checkin_date` / `checkout_date` in C#. Npgsql maps `DateOnly` → PostgreSQL `date` natively.

**JSONB:** `results_json` and `response_json` columns use `JSONB`. In EF Core:
```csharp
modelBuilder.Entity<HotelSearchCache>()
    .Property(x => x.ResultsJson)
    .HasColumnType("jsonb");
```

**Timestamps:** Use `DateTime` with UTC in C# entities. Configure Npgsql to return UTC:
```csharp
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);
```

**Migrations:** In `src/Infrastructure/Migrations/`. Run via `dotnet ef migrations add` in `src/Infrastructure/` project. Applied at startup via `dbContext.Database.MigrateAsync()`.

---

## Seed Data Strategy

**Location:** `src/Infrastructure/Seed/`
```
Seed/
├── station_areas.json
├── curated_food.json
└── curated_attractions.json
```

**Trigger:** `DataSeeder` (implements `IHostedService`) runs `ExecuteAsync()` on app startup.
Check: `if (await db.StationAreas.CountAsync() >= MinimumStationAreaCount) return;`
`MinimumStationAreaCount` = 10 (configured in `appsettings.json`).

**Idempotency:** Seed is skipped if minimum count is met. Re-running a deployed instance doesn't re-seed.

**MVP seed areas (15 total):**

| Area | Station | City | Region |
|---|---|---|---|
| Shinjuku | Shinjuku Station | Tokyo | Kanto |
| Shibuya | Shibuya Station | Tokyo | Kanto |
| Asakusa | Asakusa Station | Tokyo | Kanto |
| Akihabara | Akihabara Station | Tokyo | Kanto |
| Ueno | Ueno Station | Tokyo | Kanto |
| Ginza | Ginza Station | Tokyo | Kanto |
| Namba | Namba Station | Osaka | Kansai |
| Umeda | Osaka/Umeda Station | Osaka | Kansai |
| Shinsaibashi | Shinsaibashi Station | Osaka | Kansai |
| Gion | Gion-Shijo Station | Kyoto | Kansai |
| Kyoto Station Area | Kyoto Station | Kyoto | Kansai |
| Fushimi | Fushimi-Inari Station | Kyoto | Kansai |
| Nara Park Area | Kintetsu-Nara Station | Nara | Kansai |
| Peace Memorial Area | Hiroshima Station | Hiroshima | Chugoku |
| Miyajima Area | Miyajimaguchi Station | Hiroshima | Chugoku |
