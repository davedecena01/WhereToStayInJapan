# AI Strategy — Where To Stay In Japan

The core recommendation engine is deterministic. AI handles subjective, language-rich, or creative tasks. This separation ensures the app degrades gracefully when AI is unavailable.

---

## Responsibility Boundary

### AI Handles

| Task | Why AI | When Called |
|---|---|---|
| Itinerary text parsing | Unstructured natural language input; regex alone cannot reliably extract destinations from varied text formats | `POST /api/itinerary/parse` |
| Place name disambiguation | "Shibuya" could be the crossing, the ward, or the station. Context required | During parsing |
| Recommendation explanation | Human-readable rationale requires natural language generation | `POST /api/recommendations` (async) |
| Food suggestion generation | Requires contextual knowledge of cuisine in specific neighborhoods | `GET /api/areas/:id/food` (when curated < 5) |
| Nearby attraction suggestions | Requires contextual understanding of user interests and area character | `GET /api/areas/:id/attractions` (when curated < 3) |
| Chat refinement | Conversational intent understanding and state updates | `POST /api/chat` |

### Deterministic Logic Handles

| Task | Why Deterministic | Where |
|---|---|---|
| Destination grouping by region | Lookup table: city → region. No ambiguity for known cities | `RegionGroupingService` |
| Multi-region detection | Haversine distance formula. Mathematical, repeatable | `ItineraryNormalizer` |
| Candidate area selection | SQL query on `station_areas` by region | `StationAreaRepository` |
| Travel time calculation | OSRM API call + caching. Same input = same output | `CachedRoutingProvider` |
| Cost scoring | Seeded `avg_hotel_price_jpy` or live hotel API value. Numeric | `ScoringService` |
| Station proximity scoring | Haversine(area.lat/lng, station.lat/lng). Pure math | `ScoringService` |
| Food/shopping access scoring | Seeded `food_access_score` / `shopping_score`. Admin-set | `ScoringService` |
| Final weighted ranking | `CandidateScore` formula. Deterministic given same inputs | `ScoringService` |
| Hotel filtering by budget/rating | SQL/numeric comparison | `RakutenHotelAdapter` |
| Cache lookup/write | Key-based DB access | All cached providers |

**Critical rule:** The app must return valid recommendations even with zero AI calls. AI failure ≠ app failure.

---

## `IAIProvider` Interface

```csharp
public interface IAIProvider
{
    // Parse unstructured itinerary text → structured object
    Task<ParsedItinerary> ParseItineraryAsync(
        string rawText,
        CancellationToken ct = default);

    // Generate human-readable explanation for a recommendation
    Task<string> GenerateExplanationAsync(
        ExplanationContext context,
        CancellationToken ct = default);

    // Suggest food items for an area (called when curated < 5)
    Task<List<FoodSuggestion>> SuggestFoodAsync(
        StationArea area,
        int targetCount,
        CancellationToken ct = default);

    // Suggest attractions for an area
    Task<List<AttractionSuggestion>> SuggestAttractionsAsync(
        StationArea area,
        string[] userInterests,
        CancellationToken ct = default);

    // Chat turn — returns reply + optional itinerary update
    Task<ChatReply> ChatAsync(
        ChatMessage[] history,
        string userMessage,
        ParsedItinerary? currentItinerary,
        CancellationToken ct = default);
}

public record ExplanationContext(
    StationArea Area,
    ScoreBreakdown Score,
    TravelTimeSummary[] TravelTimes,
    string[] UserAtmospherePreferences
);

public record ChatReply(
    string Message,
    ParsedItinerary? SuggestedItineraryUpdate
);
```

---

## Adapter Implementations

### `GeminiAdapter` (Primary)

Provider: Google Gemini Flash 1.5
Auth: API key via `AI:ApiKey` config
Endpoint: `https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent`

**Key implementation decisions:**
- Always request JSON output via `response_mime_type: "application/json"` in request
- Include JSON schema in system prompt for all structured outputs
- Validate response against expected schema; throw `AIParseException` on schema mismatch
- Retry once on `AIParseException` (model occasionally produces malformed JSON)
- Retry with exponential backoff on 429 (rate limit): 1s, 2s, 4s (max 3 attempts)
- Log token usage from response metadata for cost monitoring

```csharp
public class GeminiAdapter(string apiKey, string modelId, ILogger<GeminiAdapter> logger) : IAIProvider
{
    private readonly HttpClient _http = new();
    private readonly string _baseUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";

    public async Task<ParsedItinerary> ParseItineraryAsync(string rawText, CancellationToken ct)
    {
        var prompt = BuildParsePrompt(rawText);
        var response = await CallGeminiWithRetry(prompt, ct);
        return DeserializeAndValidate<ParsedItinerary>(response);
    }
    // ...
}
```

### `MockAIAdapter` (Development / CI)

Returns deterministic, hardcoded responses. Activated via `AI:Mode = "mock"`.

```csharp
public class MockAIAdapter : IAIProvider
{
    public Task<ParsedItinerary> ParseItineraryAsync(string rawText, CancellationToken ct)
    {
        return Task.FromResult(new ParsedItinerary {
            Destinations = new List<Destination> {
                new() { Name = "Senso-ji Temple", City = "Tokyo", Region = "Kanto", DayNumber = 1 },
                new() { Name = "Shibuya Crossing", City = "Tokyo", Region = "Kanto", DayNumber = 2 },
                new() { Name = "Fushimi Inari", City = "Kyoto", Region = "Kansai", DayNumber = 5 }
            },
            TravelDates = new DateRange(DateOnly.Parse("2025-10-01"), DateOnly.Parse("2025-10-08")),
            ParsingConfidence = "high",
            ClarificationNeeded = false,
            IsMultiRegion = true,
            RegionsDetected = new[] { "Kanto", "Kansai" },
            ParsedBy = "mock"
        });
    }

    public Task<string> GenerateExplanationAsync(ExplanationContext ctx, CancellationToken ct) =>
        Task.FromResult($"[Mock] {ctx.Area.AreaName} is recommended because it has the shortest average travel time to your destinations.");

    public Task<List<FoodSuggestion>> SuggestFoodAsync(StationArea area, int count, CancellationToken ct) =>
        Task.FromResult(Enumerable.Range(1, count).Select(i => new FoodSuggestion {
            Name = $"Mock Restaurant {i}", CuisineType = "ramen", Address = $"{area.AreaName}, Tokyo"
        }).ToList());
    // ...
}
```

### `RulesOnlyAdapter` (Fallback when AI is down)

Returns best-effort results using regex and lookup tables. No API calls.

```csharp
public class RulesOnlyAdapter : IAIProvider
{
    // Regex-based place extraction; confidence always 'low'
    public Task<ParsedItinerary> ParseItineraryAsync(string rawText, CancellationToken ct)
    {
        var destinations = ExtractWithRegex(rawText);
        return Task.FromResult(new ParsedItinerary {
            Destinations = destinations,
            ParsingConfidence = "low",
            ClarificationNeeded = true,
            ParsedBy = "rules_only"
        });
    }

    // No explanation generation — return null
    public Task<string> GenerateExplanationAsync(ExplanationContext ctx, CancellationToken ct) =>
        Task.FromResult<string>(null!);

    // No AI food suggestions — return empty (caller uses curated data only)
    public Task<List<FoodSuggestion>> SuggestFoodAsync(StationArea area, int count, CancellationToken ct) =>
        Task.FromResult(new List<FoodSuggestion>());

    public Task<ChatReply> ChatAsync(ChatMessage[] history, string msg, ParsedItinerary? ctx, CancellationToken ct) =>
        Task.FromResult(new ChatReply("AI chat is currently unavailable. Please edit your itinerary manually.", null));
}
```

### `CachedAIProvider` (Cache Decorator)

Wraps any `IAIProvider`. Checks `ai_response_cache` before calling inner provider.

```csharp
public class CachedAIProvider(IAIProvider inner, ICacheService cache) : IAIProvider
{
    public async Task<ParsedItinerary> ParseItineraryAsync(string rawText, CancellationToken ct)
    {
        var key = ComputeHash("parse_itinerary", rawText);
        return await cache.GetOrSetAsync<ParsedItinerary>(
            key,
            _ => inner.ParseItineraryAsync(rawText, ct),
            TimeSpan.FromHours(24),
            ct
        );
    }
    // Same pattern for all methods
}
```

---

## Fallback Chain

```
Request arrives
    │
    ▼
CachedAIProvider.GetFromCache()
    │
    ├── CACHE HIT → return cached response (< 100ms)
    │
    └── CACHE MISS
            │
            ▼
        GeminiAdapter.Call()
            │
            ├── 200 OK → cache response → return
            │
            ├── 429 Rate Limited → retry with backoff (max 3 attempts)
            │       │
            │       └── All retries fail → fall through to RulesOnlyAdapter
            │
            └── 500/Network Error → fall through to RulesOnlyAdapter
                        │
                        ▼
                    RulesOnlyAdapter (no API call, always succeeds)
                    Returns: parsing_confidence='low', no explanations
```

**Config override:** `AI:Mode = "rules_only"` bypasses GeminiAdapter entirely. Useful for cost-zero local development.

---

## Prompt Engineering Guidelines

### Principle 1: Always request JSON output
```
System: "You are a Japan travel planning assistant. Always respond with valid JSON matching the schema provided. Do not include any text outside the JSON object."
```

### Principle 2: Include schema in every prompt
```
Schema:
{
  "destinations": [{ "name": string, "city": string|null, "region": string|null, "day_number": number|null, "activity_type": string|null }],
  "travel_dates": { "start": "YYYY-MM-DD", "end": "YYYY-MM-DD" } | null,
  "parsing_confidence": "high" | "low",
  "clarification_needed": boolean,
  "is_multi_region": boolean,
  "regions_detected": string[]
}
```

### Principle 3: Retry once on parse failure
If `JsonSerializer.Deserialize<T>()` throws, retry the API call once. Log both attempts.
If second attempt also fails, fall through to `RulesOnlyAdapter`.

### Principle 4: Never hallucinate place data
For food/attraction suggestions, prompt explicitly: "Only suggest real places that exist in {area_name}. Do not invent place names."

### Token budgets (approximate)

| Operation | Prompt tokens | Response tokens | Total |
|---|---|---|---|
| ParseItinerary | ~600 | ~400 | ~1,000 |
| GenerateExplanation | ~500 | ~200 | ~700 |
| SuggestFood (5 items) | ~300 | ~500 | ~800 |
| SuggestAttractions | ~300 | ~400 | ~700 |
| Chat turn | ~800 (history) | ~300 | ~1,100 |

**Daily free tier budget (Gemini Flash 1.5): 1,000,000 tokens/day**
At 1,000 tokens per recommendation (5 candidates × ~700 avg), the free tier supports ~1,400 recommendations/day — adequate for MVP.

---

## 429 Rate Limit Handling

```csharp
// Polly retry policy in GeminiAdapter
private static readonly IAsyncPolicy<HttpResponseMessage> RetryPolicy =
    Policy<HttpResponseMessage>
        .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),  // 2s, 4s, 8s
            onRetry: (result, wait, attempt, ctx) =>
                Log.Warning("Gemini 429 — retry {Attempt} after {Wait}s", attempt, wait.TotalSeconds)
        );
```

If all retries exhausted: `GeminiAdapter` throws `AIUnavailableException` → caller falls through to `RulesOnlyAdapter`.

---

## Free Tier Reality Check

| Concern | Reality | Mitigation |
|---|---|---|
| Gemini Flash 1.5 free tier is 1M tokens/day | Sufficient for ~1,400 full recommendations/day | AI response cache (24hr TTL) reduces repeat calls by 80%+ |
| Free tier may have concurrency limits | Unknown limit; could cause 429 under burst traffic | Cache reduces concurrency exposure |
| Free tier "forever" guarantee | Google could change pricing | `IAIProvider` abstraction means swapping to paid provider = one new adapter |
| Gemini API requires Google account | No credit card required for free tier | Provide setup instructions in README |

**Honest assessment:** For a portfolio project with single-digit concurrent users, Gemini Flash free tier is genuinely sufficient. For production traffic (100+ concurrent users), paid tier would be needed.

---

## AI-Off Operating Mode

When `AI:Mode = "rules_only"`, the app:
- Parses itineraries via regex (low confidence, clarification suggested)
- Scores candidates fully (deterministic, unaffected)
- Returns recommendations without text explanations
- Shows `"Explanation unavailable"` in the UI
- Food suggestions use curated data only (no AI supplement)
- Chat returns a static unavailable message

The core value proposition (area recommendations with travel times) is fully functional without AI.
