# Hotel Integration — Where To Stay In Japan

---

## V1 Behavior

1. Recommendation engine scores candidate areas (no hotel data required)
2. For the top 3–5 candidates, hotel search runs in parallel (preview: 3 hotels per area)
3. Full hotel list available on area detail page (paginated, 10 per page)
4. User clicks "Book on Rakuten" → opens Rakuten Travel in new tab with hotel pre-selected
5. No in-app booking. No payment processing. No reservation data stored.

---

## Provider Abstraction

```csharp
// src/Infrastructure/Adapters/Hotels/IHotelProvider.cs
public interface IHotelProvider
{
    Task<HotelSearchResult> SearchAsync(
        HotelSearchParams searchParams,
        CancellationToken ct = default);
}

public record HotelSearchParams(
    decimal Lat,
    decimal Lng,
    int RadiusKm,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Travelers,
    BudgetTier BudgetTier,
    int Page = 1,
    int PageSize = 10
);

public record HotelSearchResult(
    IReadOnlyList<HotelItem> Hotels,
    int Total,
    string Provider
);

public enum BudgetTier { Budget, Mid, Luxury }
```

**Registered via DI with cache decorator:**
```
IHotelProvider
  └── CachedHotelProvider (decorator)
        └── RakutenHotelAdapter | MockHotelAdapter
```

---

## Rakuten Travel API

**Registration:** Free for registered developers at `https://webservice.rakuten.co.jp/`
**API Key:** Application ID issued on registration
**Coverage:** 100,000+ hotels in Japan, extensive English metadata

### API Endpoint Used

`https://app.rakuten.co.jp/services/api/Travel/HotelSearch/20170426`

**Key request parameters:**

| Parameter | Value | Notes |
|---|---|---|
| `applicationId` | `{API_KEY}` | Required |
| `format` | `json` | |
| `latitude` | `{lat}` | WGS84 decimal |
| `longitude` | `{lng}` | WGS84 decimal |
| `searchRadius` | `{RadiusKm}` | Default: 1, max: 3 |
| `checkinDate` | `YYYY-MM-DD` | |
| `checkoutDate` | `YYYY-MM-DD` | |
| `adultNum` | `{Travelers}` | |
| `minCharge` | Budget tier minimum | See budget mapping |
| `maxCharge` | Budget tier maximum | See budget mapping |
| `hotelThumbnailSize` | `3` | Size 3 = medium thumbnail |
| `responseType` | `small` | Reduces response size |
| `page` | `{Page}` | |
| `hits` | `{PageSize}` | Max 30 per request |

### Budget Tier to Price Range

| Tier | minCharge (JPY/night) | maxCharge (JPY/night) |
|---|---|---|
| budget | 0 | 8,000 |
| mid | 8,001 | 25,000 |
| luxury | 25,001 | 9,999,999 |

### `RakutenHotelAdapter`

```csharp
public class RakutenHotelAdapter(
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<RakutenHotelAdapter> logger
) : IHotelProvider
{
    private readonly string _apiKey = config["Hotels:ApiKey"]!;
    private readonly double _minRating = double.Parse(config["Hotels:MinReviewRating"] ?? "3.5");
    private readonly int _radiusKm = int.Parse(config["Hotels:SearchRadiusKm"] ?? "2");

    public async Task<HotelSearchResult> SearchAsync(HotelSearchParams p, CancellationToken ct)
    {
        var (minCharge, maxCharge) = GetPriceRange(p.BudgetTier);
        var url = BuildUrl(p, minCharge, maxCharge);

        try
        {
            var client = httpFactory.CreateClient("rakuten");
            var response = await client.GetFromJsonAsync<RakutenResponse>(url, ct);
            if (response is null) return new HotelSearchResult([], 0, "rakuten");

            var hotels = response.Hotels
                .Where(h => h.HotelBasicInfo.ReviewAverage >= _minRating)
                .Select(MapToHotelItem)
                .ToList();

            return new HotelSearchResult(hotels, response.PagingInfo?.RecordCount ?? hotels.Count, "rakuten");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rakuten hotel search failed for area ({Lat},{Lng})", p.Lat, p.Lng);
            throw new HotelProviderException("Rakuten Travel API is unavailable", ex);
        }
    }

    private HotelItem MapToHotelItem(RakutenHotel h)
    {
        var info = h.HotelBasicInfo;
        return new HotelItem(
            ProviderId:       info.HotelNo.ToString(),
            Name:             info.HotelName,
            AreaName:         info.Address1,
            NearestStation:   info.NearestStation ?? "",
            GeoPoint:         new GeoPoint((decimal)info.Latitude, (decimal)info.Longitude),
            PricePerNightJpy: h.DailyCharge?.StayDate?.FirstOrDefault()?.MinCharge ?? 0,
            Rating:           (decimal)info.ReviewAverage,
            ReviewCount:      info.ReviewCount,
            ImageUrl:         info.HotelThumbnailUrl,
            DeepLinkUrl:      BuildDeepLink(info.HotelNo, info.HotelInformationUrl),
            Amenities:        ExtractAmenities(h),
            Provider:         "rakuten"
        );
    }

    private string BuildDeepLink(int hotelNo, string hotelUrl)
    {
        // Use Rakuten affiliate deep link when affiliate ID is configured
        // Otherwise use direct hotel URL
        var affiliateId = config["Hotels:AffiliateId"];
        if (!string.IsNullOrEmpty(affiliateId))
            return $"https://hb.afl.rakuten.co.jp/hgc/{affiliateId}/?pc={Uri.EscapeDataString(hotelUrl)}";
        return hotelUrl;
    }
}
```

### Rakuten Response Mapping

Key Rakuten API response fields → `HotelItem` DTO:

| Rakuten field | Our field | Notes |
|---|---|---|
| `hotelBasicInfo.hotelNo` | `provider_id` | Integer, convert to string |
| `hotelBasicInfo.hotelName` | `name` | |
| `hotelBasicInfo.address1` + `address2` | `area_name` | Concatenated |
| `hotelBasicInfo.nearestStation` | `nearest_station` | May be null |
| `hotelBasicInfo.latitude` | `geo_point.lat` | |
| `hotelBasicInfo.longitude` | `geo_point.lng` | |
| `dailyCharge.stayDate[0].minCharge` | `price_per_night_jpy` | Lowest available room rate |
| `hotelBasicInfo.reviewAverage` | `rating` | 1.0–5.0 |
| `hotelBasicInfo.reviewCount` | `review_count` | |
| `hotelBasicInfo.hotelThumbnailUrl` | `image_url` | |
| `hotelBasicInfo.hotelInformationUrl` | `deep_link_url` (base) | See deep link section |

---

## Deep Link Format

Direct hotel URL (no affiliate):
```
https://travel.rakuten.co.jp/hotel/{hotel_id}/
```

With check-in/check-out pre-filled (Rakuten URL parameters):
```
https://travel.rakuten.co.jp/hotel/{hotel_id}/?f_tedate={checkin_YYYYMMDD}&f_sydate={checkout_YYYYMMDD}&f_otona_su={travelers}
```

Example:
```
https://travel.rakuten.co.jp/hotel/136197/?f_tedate=20251001&f_sydate=20251008&f_otona_su=2
```

**Note:** The `hotelInformationUrl` from Rakuten API response already contains the full URL. Append date parameters to it rather than constructing from `hotelNo` alone, since Rakuten URLs can vary.

---

## `CachedHotelProvider`

```csharp
public class CachedHotelProvider(IHotelProvider inner, ICacheService cache) : IHotelProvider
{
    public Task<HotelSearchResult> SearchAsync(HotelSearchParams p, CancellationToken ct)
    {
        var key = ComputeCacheKey(p);
        return cache.GetOrSetAsync<HotelSearchResult>(
            key,
            _ => inner.SearchAsync(p, ct),
            TimeSpan.FromMinutes(30),
            ct
        );
    }

    private static string ComputeCacheKey(HotelSearchParams p)
    {
        var raw = $"{p.Lat:F6},{p.Lng:F6}:{p.CheckIn:yyyy-MM-dd}:{p.CheckOut:yyyy-MM-dd}:{p.BudgetTier}:{p.Page}:{p.PageSize}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
```

---

## `MockHotelAdapter`

Returns 5 hardcoded hotels for any query. Used in development and CI.

```csharp
public class MockHotelAdapter : IHotelProvider
{
    public Task<HotelSearchResult> SearchAsync(HotelSearchParams p, CancellationToken ct)
    {
        var hotels = Enumerable.Range(1, 5).Select(i => new HotelItem(
            ProviderId:       $"mock_{i}",
            Name:             $"Mock Hotel {i}",
            AreaName:         "Shinjuku",
            NearestStation:   "Shinjuku Station",
            GeoPoint:         new GeoPoint(35.6938m + i * 0.001m, 139.7034m),
            PricePerNightJpy: 8000 + (i * 2000),
            Rating:           3.5m + (i * 0.2m),
            ReviewCount:      100 * i,
            ImageUrl:         null,
            DeepLinkUrl:      "https://travel.rakuten.co.jp",
            Amenities:        ["wifi", "breakfast"],
            Provider:         "mock"
        )).ToList();
        return Task.FromResult(new HotelSearchResult(hotels, 5, "mock"));
    }
}
```

---

## Failure Handling

**When Rakuten API returns error or throws:**
1. `RakutenHotelAdapter` throws `HotelProviderException`
2. `HotelSearchService` catches it, logs the error
3. Returns `HotelSearchResult([], 0, "unavailable")` — not an exception propagated to controller
4. `RecommendationResultDto.HotelsAvailable = false`
5. HTTP response: `206 Partial Content` with `hotels_available: false` in body
6. Frontend: shows "Hotel results temporarily unavailable" error state in hotel section
7. Recommendation cards still fully rendered (area, score, explanation, food, attractions)

**Never fail the entire recommendation response because of a hotel API failure.**

---

## Adding a Second Hotel Provider

To add Booking.com (or any other provider) in Phase 3:

1. Implement `BookingComAdapter : IHotelProvider`
2. Register in DI:
   ```csharp
   IHotelProvider inner = config["Hotels:Provider"] switch {
       "mock"      => new MockHotelAdapter(),
       "bookingcom"=> sp.GetRequiredService<BookingComAdapter>(),
       _           => sp.GetRequiredService<RakutenHotelAdapter>()
   };
   ```
3. No changes to `IHotelSearchService`, `RecommendationService`, or controllers.

**Multi-provider (future):** If showing results from multiple providers simultaneously, `IHotelSearchService` can fan-out to multiple `IHotelProvider` instances and merge/deduplicate results. This is a Phase 3 concern.

---

## Affiliate Setup (Phase 3)

Rakuten has an affiliate program. When the affiliate ID is configured:
- Deep links are wrapped in Rakuten affiliate URL format
- Hotel clicks logged in `hotel_click_logs` table
- Affiliate commission tracked through Rakuten dashboard (not in-app)

V1: Affiliate ID not required. Direct hotel URLs work for demo purposes.
Phase 3: Register at `https://affiliate.rakuten.co.jp/`, set `Hotels:AffiliateId` env var.

---

## Rakuten API Setup (Developer Registration)

1. Register at `https://webservice.rakuten.co.jp/`
2. Create an application → receive `applicationId`
3. Set `HOTELS__APIKEY` environment variable
4. Test: `GET https://app.rakuten.co.jp/services/api/Travel/HotelSearch/20170426?applicationId={KEY}&latitude=35.6938&longitude=139.7034&searchRadius=1&format=json`

**If Rakuten approval is pending before demo:** Set `Hotels:Provider = "mock"` — all hotel searches return `MockHotelAdapter` results. Change to `rakuten` when API key is ready.
