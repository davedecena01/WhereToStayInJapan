# Maps and Routing — Where To Stay In Japan

---

## Provider Strategy

| Concern | V1 Provider | Free Tier | Swap-in for Phase 2+ |
|---|---|---|---|
| Geocoding (place name → lat/lng) | Nominatim (OpenStreetMap) | Yes — 1 req/sec, no auth | Google Geocoding API |
| Routing / travel time | OSRM public demo server | Yes — no auth, no SLA | Google Directions API |
| Map display | None in V1 | n/a | Leaflet + OSM tiles |

All providers are isolated behind interfaces. Swapping Google Maps in = implement 2 new adapters, update DI config. Zero changes to business logic.

---

## Provider Interfaces

```csharp
// src/Infrastructure/Adapters/Maps/IGeocodeProvider.cs
public interface IGeocodeProvider
{
    // Returns null if geocoding fails (not an exception for unknown places)
    Task<GeoPoint?> GeocodeAsync(
        string placeName,
        string? cityHint = null,
        CancellationToken ct = default);
}

// src/Infrastructure/Adapters/Maps/IRoutingProvider.cs
public interface IRoutingProvider
{
    Task<RouteResult?> GetTravelTimeAsync(
        GeoPoint origin,
        GeoPoint destination,
        TravelMode mode,
        CancellationToken ct = default);
}

public record GeoPoint(decimal Lat, decimal Lng);
public record RouteResult(int DurationMinutes, decimal DistanceKm, string Provider);
public enum TravelMode { Driving, Walking }
```

---

## `NominatimAdapter`

Calls the Nominatim public API for geocoding.

**Base URL:** `https://nominatim.openstreetmap.org/search`
**Rate limit:** 1 request/second — **hard limit enforced by OSM policy**
**Required:** `User-Agent` header identifying your app (OSM ToS requirement)

```csharp
public class NominatimAdapter(IHttpClientFactory httpFactory, IConfiguration config, ILogger<NominatimAdapter> logger) : IGeocodeProvider
{
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);

    public async Task<GeoPoint?> GeocodeAsync(string placeName, string? cityHint, CancellationToken ct)
    {
        var query = cityHint != null ? $"{placeName}, {cityHint}, Japan" : $"{placeName}, Japan";
        var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit=1&countrycodes=jp";

        // Rate limiting: wait for permit, then enforce 1s between requests
        await _rateLimiter.WaitAsync(ct);
        try
        {
            var client = httpFactory.CreateClient("nominatim");
            var response = await client.GetFromJsonAsync<NominatimResult[]>(url, ct);
            if (response is null || response.Length == 0)
            {
                logger.LogWarning("Nominatim returned no results for: {Query}", query);
                return null;
            }
            return new GeoPoint(decimal.Parse(response[0].Lat), decimal.Parse(response[0].Lon));
        }
        finally
        {
            // Enforce 1 second between releases
            _ = Task.Delay(1000, CancellationToken.None).ContinueWith(_ => _rateLimiter.Release());
        }
    }
}
```

**HttpClient registration:**
```csharp
builder.Services.AddHttpClient("nominatim", client => {
    client.DefaultRequestHeaders.Add("User-Agent", config["Maps:UserAgent"]);
    client.Timeout = TimeSpan.FromSeconds(10);
});
```

**OSM attribution requirement:** If displaying OSM map tiles in the UI, include: `© OpenStreetMap contributors` with a link to `https://www.openstreetmap.org/copyright`. This is a legal requirement, not optional.

---

## `OsrmAdapter`

Calls the OSRM public demo server for routing.

**Base URL:** `http://router.project-osrm.org/route/v1/driving/{lng1},{lat1};{lng2},{lat2}?overview=false`
**Auth:** None required
**SLA:** None. Public demo server. May be unavailable. Cache aggressively.

**Transit time approximation:** OSRM only provides driving routes. Japan transit is significantly different from driving. V1 workaround:
- Multiply OSRM driving time by **1.3** to approximate transit time
- Document this assumption clearly in recommendation UI: "Estimated travel time (approximate)"
- This is a known limitation; hardcoded seed travel times are preferred for seeded areas (see below)

```csharp
public class OsrmAdapter(IHttpClientFactory httpFactory, ILogger<OsrmAdapter> logger) : IRoutingProvider
{
    public async Task<RouteResult?> GetTravelTimeAsync(GeoPoint origin, GeoPoint dest, TravelMode mode, CancellationToken ct)
    {
        // OSRM uses lng,lat order (not lat,lng)
        var url = $"http://router.project-osrm.org/route/v1/driving/{origin.Lng},{origin.Lat};{dest.Lng},{dest.Lat}?overview=false";
        try
        {
            var client = httpFactory.CreateClient("osrm");
            var response = await client.GetFromJsonAsync<OsrmResponse>(url, ct);
            if (response?.Code != "Ok" || response.Routes.Length == 0) return null;

            var drivingSeconds = response.Routes[0].Duration;
            var drivingMinutes = (int)Math.Ceiling(drivingSeconds / 60.0);
            var adjustedMinutes = (int)Math.Ceiling(drivingMinutes * 1.3);  // transit approximation

            return new RouteResult(adjustedMinutes, (decimal)(response.Routes[0].Distance / 1000.0), "osrm");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OSRM routing failed for {Origin} → {Dest}", origin, dest);
            return null;
        }
    }
}
```

---

## `SeededFallbackRoutingProvider`

When OSRM fails or returns null, and no cache hit exists, fall back to precomputed seed data.

The seed script pre-populates `routing_cache` with travel times between all seeded station areas and their common destination points. For seeded areas, this fallback provides reasonable estimates without any API call.

```csharp
public class SeededFallbackRoutingProvider(ICacheService cache) : IRoutingProvider
{
    public async Task<RouteResult?> GetTravelTimeAsync(GeoPoint origin, GeoPoint dest, TravelMode mode, CancellationToken ct)
    {
        // This provider only returns cached/seeded data — never makes external calls
        var key = ComputeCacheKey(origin, dest, mode);
        var cached = await cache.GetAsync<RouteResult>(key, ct);
        if (cached is not null)
        {
            return cached with { Provider = "seeded_fallback" };
        }
        // No seed data for this pair — return null, caller logs "travel time unknown"
        return null;
    }
}
```

---

## `CachedGeocodeProvider` and `CachedRoutingProvider`

Both are decorator classes wrapping the real provider with cache-first lookup.

```csharp
public class CachedGeocodeProvider(IGeocodeProvider inner, ICacheService cache) : IGeocodeProvider
{
    public async Task<GeoPoint?> GeocodeAsync(string placeName, string? cityHint, CancellationToken ct)
    {
        var key = NormalizeKey($"{placeName}{cityHint}");
        return await cache.GetOrSetAsync<GeoPoint?>(
            key,
            _ => inner.GeocodeAsync(placeName, cityHint, ct),
            TimeSpan.FromDays(90),
            ct
        );
    }

    private static string NormalizeKey(string input) =>
        input.ToLowerInvariant().Trim().Replace(" ", "-").Replace("'", "").Replace(".", "");
}

public class CachedRoutingProvider(IRoutingProvider inner, ICacheService cache) : IRoutingProvider
{
    public async Task<RouteResult?> GetTravelTimeAsync(GeoPoint origin, GeoPoint dest, TravelMode mode, CancellationToken ct)
    {
        var key = ComputeKey(origin, dest, mode);
        return await cache.GetOrSetAsync<RouteResult?>(
            key,
            _ => inner.GetTravelTimeAsync(origin, dest, mode, ct),
            TimeSpan.FromDays(7),
            ct
        );
    }

    private static string ComputeKey(GeoPoint o, GeoPoint d, TravelMode m) =>
        Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes($"{o.Lat:F6},{o.Lng:F6}:{d.Lat:F6},{d.Lng:F6}:{m}")));
}
```

---

## Travel Time Matrix Construction

Used in `RecommendationService` to build the scoring input:

```csharp
private async Task<TravelTimeMatrix> BuildTravelTimeMatrix(
    IReadOnlyList<StationArea> candidates,
    IReadOnlyList<Destination> destinations,
    CancellationToken ct)
{
    var matrix = new TravelTimeMatrix();

    // Geocode all destinations first (parallel, cache-friendly)
    var geocodeTasks = destinations
        .Where(d => d.GeoPoint is null)
        .Select(async d => {
            d.GeoPoint = await _geocodeProvider.GeocodeAsync(d.Name, d.City, ct);
        });
    await Task.WhenAll(geocodeTasks);

    // Build travel times (parallel per candidate)
    var candidateTasks = candidates.Select(async candidate => {
        var stationPoint = new GeoPoint(candidate.StationLat, candidate.StationLng);
        var times = new Dictionary<string, int?>();

        var destTasks = destinations
            .Where(d => d.GeoPoint is not null)
            .Select(async dest => {
                var route = await _routingProvider.GetTravelTimeAsync(stationPoint, d.GeoPoint!, TravelMode.Driving, ct);
                times[dest.Name] = route?.DurationMinutes;
            });
        await Task.WhenAll(destTasks);

        matrix[candidate.Id] = times;
    });
    await Task.WhenAll(candidateTasks);

    return matrix;
}
```

**Failure behavior:**
- Single destination geocoding fails → `geo_point = null` → excluded from travel time avg
- Single OSRM call fails → `duration_minutes = null` → excluded from avg
- All destinations fail → avg travel time = null → travel_time_score defaults to 0.5 (neutral) for that candidate

---

## Cache Warm-Up

Run during initial seed (`DataSeeder`) to pre-populate routing data for seeded areas:

```
For each pair (StationArea A, StationArea B) where A ≠ B:
    Call OsrmAdapter.GetTravelTimeAsync(A.station_geopoint, B.station_geopoint)
    Store in routing_cache with 7-day TTL
    (CachedRoutingProvider handles this automatically — just iterate and call)

For each StationArea and each known major destination in that city:
    (e.g., Shinjuku Station → Senso-ji, Shinjuku → Tsukiji Market, etc.)
    Call geocode + routing, store to cache
```

This warm-up makes initial recommendations fast even without prior user traffic.

---

## Google Maps Swap Path (Phase 2)

When ready to swap:

1. Implement `GoogleGeocodeAdapter : IGeocodeProvider`
   - Calls `https://maps.googleapis.com/maps/api/geocode/json?address={query}&key={key}`

2. Implement `GoogleRoutingAdapter : IRoutingProvider`
   - Calls Directions API with `mode=transit` for accurate Japan transit times
   - Return `duration_in_traffic` for more realistic estimates

3. Update DI config:
   ```
   Maps:GeocodeProvider = "google"
   Maps:RoutingProvider = "google"
   Maps:GoogleApiKey = "..."
   ```

4. No changes to `IScoringService`, `RecommendationService`, or any other code.

**Why this is clean:** Every routing call in the codebase goes through `IRoutingProvider`. Swapping the implementation is a 1-line config change + 1 new adapter class.

---

## V1 Map Display

No interactive map in V1. The frontend displays a static decorative Japan region image (SVG or PNG with region labels). Latitude/longitude data is stored for all areas and ready for Phase 2 interactive maps.

**Phase 2 map plan:**
- Leaflet.js + OpenStreetMap tiles (free, attribution required)
- Show recommended area pins on a Japan map
- Show itinerary destination pins
- OSM tile URL: `https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png`
- Attribution: `Map data © OpenStreetMap contributors`

---

## Nominatim Rate Limit Impact Analysis

**Worst case (cold cache, 5 destination itinerary):**
- 5 destinations × 1s delay = 5 seconds of geocoding
- 5 candidates × 5 destinations = 25 routing calls (OSRM has no rate limit)

**With warm cache (typical return visitor):**
- 0 Nominatim calls (all geocodes cached)
- 0–5 OSRM calls (most routes cached)
- Total: < 500ms for routing phase

**Why this is acceptable for MVP:**
- The 90-day geocode cache means a place is only geocoded once per 90 days
- Seeded station areas have pre-geocoded lat/lng — never hit Nominatim for them
- Only user-submitted itinerary destinations hit Nominatim
- 1 req/sec means 5 destinations takes 5 seconds max — within the 8s total target
