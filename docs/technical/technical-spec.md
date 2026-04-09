# Technical Specification — Where To Stay In Japan

---

## Input Formats

| Format | Max Size | Extraction Method | Library |
|---|---|---|---|
| PDF | 10 MB | Text layer extraction | `PdfPig` (NuGet: `UglyToad.PdfPig`) |
| DOCX | 10 MB | OpenXML text extraction | `DocumentFormat.OpenXml` |
| TXT | 50 KB | Direct string | None |
| Pasted text | 50 KB | Direct string | None |

**File validation steps (in order):**
1. Check `Content-Length` header (pre-upload, client-side for DX)
2. Validate MIME type: `application/pdf`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`, `text/plain`
3. Validate magic bytes (first 4 bytes): `%PDF` for PDF, `PK\x03\x04` for DOCX (ZIP-based)
4. Store to system temp directory with UUID filename
5. Extract text
6. Delete temp file (always, including on error — use `try/finally`)

**Why PdfPig over iTextSharp:** PdfPig is pure-managed code, no native dependencies (iTextSharp LGPL version is outdated; iText7 requires license for commercial use). PdfPig handles standard PDF text extraction reliably.

---

## Itinerary Parsing Pipeline

### Step 1: Text Extraction
```
IItineraryExtractor (interface)
  PdfExtractor      → PdfPig
  DocxExtractor     → DocumentFormat.OpenXml
  PlainTextExtractor → pass-through string normalization

Returns: string (raw extracted text)
Throws: TextExtractionException if text is empty after extraction
```

### Step 2: AI Normalization
```
IAIProvider.ParseItineraryAsync(rawText) → ParsedItinerary

Prompt instructs Gemini to:
  - Extract a flat list of named destinations
  - For each: infer city, region, day number (if dates present), activity type
  - Extract travel date range if present
  - Detect multi-region trip
  - Return structured JSON matching ParsedItinerary schema
  - Flag clarification_needed=true if >20% of destinations are ambiguous

Fallback (RulesOnlyAdapter):
  - Regex: extract lines with date patterns (Day \d+, \d{4}-\d{2}-\d{2}, etc.)
  - Regex: extract capitalized proper nouns after location keywords (Visit, Go to, At, In)
  - Region inference: match extracted names against seeded station_areas city/area_name
  - parsing_confidence always 'low'; clarification_needed always true
```

### Step 3: Post-Processing (deterministic, always runs)
```
ItineraryNormalizer.Normalize(ParsedItinerary) → ParsedItinerary

Rules applied:
  1. Deduplicate: merge destinations with same normalized name (case-insensitive, strip common suffixes)
  2. Sort: if day_number present, sort by day_number
  3. Geocode: for each destination without geo_point, call IGeocodeProvider.GeocodeAsync()
     - Failed geocodes: geo_point stays null, destination kept (not dropped)
  4. Region inference: for destinations with city but no region, lookup region from station_areas
  5. Multi-region detection:
     - Group destinations by region
     - For each pair of regions, calculate haversine distance between representative stations
     - is_multi_region = true if any pair has distance > 100km
```

---

## Recommendation Engine Pipeline

All steps are deterministic except explanation generation. Scoring produces identical output for identical inputs.

### Step 1: Candidate Selection
```
Input: ParsedItinerary.regions_detected[]
Query: SELECT * FROM station_areas WHERE region = ANY(@regions) AND is_active = true
Result: StationArea[] (the candidate pool)

If must_be_near_station is set in preferences:
  Filter candidates to those where station ILIKE '%{must_be_near_station}%'
  If no match: ignore filter and log warning

Minimum candidates: 3. If fewer than 3 in detected regions, expand to neighboring regions
(e.g., Tokyo not found → include all Kanto).
```

### Step 2: Travel Time Matrix
```
For each candidate StationArea:
  For each Destination in itinerary (where geo_point is not null):
    1. origin = candidate.station_geopoint
    2. dest = destination.geo_point
    3. result = IRoutingProvider.GetTravelTimeAsync(origin, dest, TravelMode.Driving)
    4. adjusted_minutes = result.duration_mins × 1.3  (transit adjustment factor)
    5. Store in TravelTimeMatrix[candidate.id][destination.name]

If GetTravelTimeAsync returns null (routing failed):
  Mark as null in matrix; excluded from avg calculation
  If ALL destinations null for a candidate: use seeded avg_hotel_price fallback score
```

### Step 3: Scoring (IScoringService.ScoreCandidates — pure function)
```
For each candidate, calculate raw component values:

  avg_travel_time_minutes:
    = average of non-null values in TravelTimeMatrix[candidate.id]
    = null if all values are null

  cost_raw:
    = candidate.avg_hotel_price_jpy
    (use live hotel price sample if available from hotel API warm-up; fallback to seeded value)

  station_proximity_km:
    = haversine(candidate.lat, candidate.lng, candidate.station_lat, candidate.station_lng)

  food_access_raw: candidate.food_access_score (seeded 0.0–1.0)
  shopping_raw:    candidate.shopping_score (seeded 0.0–1.0)

Normalize each component across all candidates (min-max normalization):
  normalized = (value - min) / (max - min)
  For travel_time_score and cost_score: invert (1 - normalized) so lower is better

Apply preference modifiers:
  If avoid_long_walking=true: multiply station_proximity_score by 1.5 (up to cap of 1.0)
  If preferred_atmosphere contains 'nightlife': multiply shopping_score by 1.2 (Shibuya/Shinjuku bias)
  If preferred_atmosphere contains 'historic': bias toward areas with high curated_attractions of type 'temple'|'shrine'

Final score formula:
  CandidateScore = (travel_time_score × 0.40)
                 + (cost_score × 0.30)
                 + (station_proximity_score × 0.15)
                 + (food_access_score × 0.10)
                 + (shopping_score × 0.05)

Sort descending. Return top 5.
```

### Step 4: Async Enrichment (parallel, all candidates simultaneously)
```
For each top candidate (up to 5), in parallel:
  Task A: IHotelProvider.SearchAsync() → hotel_preview (first 3 results)
  Task B: IAIProvider.GenerateExplanationAsync() → explanation string
  Task C: IFoodRepository.GetCuratedFoodAsync() → curated FoodItem[]
           IF curated < 5: IAIProvider.SuggestFoodAsync() to fill up to 5+
  Task D: IAttractionRepository.GetCuratedAttractionsAsync()
           IF curated < 3: IAIProvider.SuggestAttractionsAsync()

Wait for all with timeout: 7 seconds total.
Tasks that fail or timeout: return partial result (null explanation, empty hotel_preview, etc.)
Never fail the entire recommendation if enrichment fails.
```

---

## Response Time Targets

| Operation | Target | Notes |
|---|---|---|
| Parsing (AI) | < 5s | Gemini latency; cached responses < 100ms |
| Parsing (rules-only fallback) | < 500ms | No API call |
| Candidate selection + scoring | < 2s | DB query + deterministic computation |
| Travel time matrix (cache hit) | < 200ms | PostgreSQL lookup |
| Travel time matrix (OSRM call) | 1–3s per destination | Rate-limited + cached |
| AI explanation (per candidate) | < 3s | Parallel; Gemini p90 latency |
| Hotel preview (per candidate) | < 2s | Parallel; Rakuten API + cache |
| Total recommendation response | < 8s | Worst case: cold cache, AI + hotels live |
| Total recommendation response (warm cache) | < 2s | Typical return visitor |

---

## Error Handling Contract

All errors return RFC 7807 `ProblemDetails`. See `api-contracts.md` for full error codes.

**Global exception middleware in `Program.cs`:**
```csharp
app.UseExceptionHandler(app => app.Run(async ctx => {
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var problem = ex switch {
        ValidationException ve   => new ProblemDetails { Status = 400, ... },
        NotFoundException nfe    => new ProblemDetails { Status = 404, ... },
        _                        => new ProblemDetails { Status = 500, ... }
    };
    ctx.Response.StatusCode = problem.Status ?? 500;
    await ctx.Response.WriteAsJsonAsync(problem);
}));
```

**Partial success:** `POST /api/recommendations` returns `206 Partial Content` when scoring succeeds but enrichment partially fails. Response body includes `hotels_available: false` and/or `explanation: null`. Frontend handles each flag independently.

**Retryable errors:** Hotel API 503, AI 429, maps 429 — all marked `retryable: true`. Frontend shows "Try again" button.

---

## File Upload Security

1. **MIME type check:** Validate `Content-Type` header AND magic bytes (don't trust extension alone).
2. **Size limit:** 10MB enforced by `[RequestSizeLimit(10_485_760)]` attribute AND `Kestrel.MaxRequestBodySize` in `Program.cs`.
3. **Temp file cleanup:** Wrapped in `try/finally`; delete after text extraction regardless of outcome.
4. **No persistence:** Files are never stored permanently. Extract text → discard file.
5. **No path traversal:** Temp filename is always `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())`. No user-controlled filename is ever used in file system operations.
6. **Content validation:** After extraction, reject if text is empty (`< 10 characters`) or if text appears to be binary garbage (>30% non-printable characters).

---

## Rate Limiting

V1 implementation: in-memory `Dictionary<string, RateLimitState>` keyed by IP address.
**Not distributed** — single-instance deployment on Railway/Render means in-memory is sufficient for MVP.

```csharp
// Applied via middleware
POST /api/recommendations: 10 requests / IP / hour
POST /api/itinerary/parse: 20 requests / IP / hour
POST /api/chat:            30 requests / session_id / hour
```

**429 response:**
```json
{
  "status": 429,
  "title": "Rate limit exceeded",
  "detail": "You have made too many requests. Please wait before trying again.",
  "code": "RATE_LIMIT_EXCEEDED",
  "retryable": true
}
```
Header: `Retry-After: {seconds_until_reset}`

---

## Normalization Rules (deterministic)

**Place name normalization (for geocoding cache key):**
```csharp
string NormalizeKey(string placeName) =>
    placeName
        .ToLowerInvariant()
        .Trim()
        .Replace("  ", " ")   // collapse double spaces
        .Replace("'", "")
        .Replace(".", "")
        .Replace(",", "");
```

**Region normalization:**
```csharp
// Known city → region mapping (lookup table in seeded data)
"Tokyo"    → "Kanto"
"Yokohama" → "Kanto"
"Kamakura" → "Kanto"
"Osaka"    → "Kansai"
"Kyoto"    → "Kansai"
"Nara"     → "Kansai"
"Kobe"     → "Kansai"
"Hiroshima"→ "Chugoku"
"Miyajima" → "Chugoku"
```

**Duplicate detection:**
```
Two destinations are considered duplicates if:
  NormalizeKey(dest1.name) == NormalizeKey(dest2.name)
  OR
  levenshtein(NormalizeKey(dest1.name), NormalizeKey(dest2.name)) <= 2 AND same city
```

**Multi-region detection threshold:**
```
haversine(region1_centroid, region2_centroid) > 100km
Tokyo → Kyoto: ~450km → triggers multi-region
Tokyo → Kamakura: ~50km → same region (both Kanto)
```

---

## Caching Strategy

All caches use the `ICacheService` interface backed by `PostgresCacheService`. Cache key formats:

| Cache | Key Format | TTL |
|---|---|---|
| Geocode | `normalize(place_name)` | 90 days |
| Routing | `SHA256("{lat1},{lng1}:{lat2},{lng2}:{mode}")` | 7 days |
| Hotel search | `SHA256("{area_id}:{checkin}:{checkout}:{budget_tier}:{page}")` | 30 min |
| AI response | `SHA256("{prompt_type}:{normalize(input)}")` | 24–48 hrs |

**Cache cleanup:** `CacheCleanupService : IHostedService` runs every 6 hours:
```csharp
DELETE FROM geocode_cache WHERE expires_at < NOW();
DELETE FROM routing_cache WHERE expires_at < NOW();
DELETE FROM ai_response_cache WHERE expires_at < NOW();
DELETE FROM hotel_search_cache WHERE expires_at < NOW();
```

**Cache warm-up:** On first deployment, seed script pre-populates `routing_cache` for all combinations of seeded station areas × their nearest major destination stations. This ensures cold-start performance is acceptable even with no prior traffic.
