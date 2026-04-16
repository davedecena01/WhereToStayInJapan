using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WhereToStayInJapan.Infrastructure.Adapters.Hotels;

public class RakutenHotelAdapter(
    HttpClient http,
    IConfiguration config,
    ILogger<RakutenHotelAdapter> logger) : IHotelProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private string ApiKey      => config["Hotels:ApiKey"]      ?? string.Empty;
    private string AffiliateId => config["Hotels:AffiliateId"] ?? config["RAKUTEN_AFFILIATE_ID"] ?? string.Empty;
    private double MinRating   => double.TryParse(config["Hotels:MinReviewRating"], out var r) ? r : 3.5;
    private int    RadiusKm    => int.TryParse(config["Hotels:SearchRadiusKm"], out var r) ? r : 2;

    public async Task<IReadOnlyList<HotelItem>> SearchAsync(HotelSearchParams p, CancellationToken ct = default)
    {
        var (minCharge, maxCharge) = GetPriceRange(p.BudgetTier);

        var url = $"services/api/Travel/VacantHotelSearch/20170426" +
                  $"?applicationId={Uri.EscapeDataString(ApiKey)}" +
                  $"&format=json" +
                  $"&latitude={p.Lat:F6}" +
                  $"&longitude={p.Lng:F6}" +
                  $"&searchRadius={RadiusKm}" +
                  $"&checkinDate={p.CheckIn:yyyy-MM-dd}" +
                  $"&checkoutDate={p.CheckOut:yyyy-MM-dd}" +
                  $"&adultNum={p.Travelers}" +
                  $"&minCharge={minCharge}" +
                  $"&maxCharge={maxCharge}" +
                  $"&hotelThumbnailSize=3" +
                  $"&responseType=small" +
                  $"&page={p.Page}" +
                  $"&hits={p.PageSize}";

        try
        {
            var json = await http.GetStringAsync(url, ct);
            var response = JsonSerializer.Deserialize<RakutenSearchResponse>(json, JsonOpts);
            if (response?.Hotels is null) return [];

            return response.Hotels
                .Where(h => h.Hotel?.FirstOrDefault()?.HotelBasicInfo?.ReviewAverage >= MinRating)
                .Select(h => MapToHotelItem(h.Hotel!.First().HotelBasicInfo!, p))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rakuten hotel search failed for ({Lat},{Lng})", p.Lat, p.Lng);
            throw new HotelProviderException("Rakuten Travel API is unavailable", ex);
        }
    }

    private HotelItem MapToHotelItem(RakutenHotelBasicInfo info, HotelSearchParams p)
    {
        var deepLink = BuildDeepLink(info.HotelInformationUrl ?? $"https://travel.rakuten.co.jp/HOTEL/{info.HotelNo}/", p, AffiliateId);

        return new HotelItem(
            HotelId:             info.HotelNo.ToString(),
            Name:                info.HotelName ?? "Unknown Hotel",
            ImageUrl:            info.HotelThumbnailUrl,
            PricePerNightJpy:    info.HotelMinCharge.HasValue ? (decimal)info.HotelMinCharge.Value : 0m,
            ReviewRating:        info.ReviewAverage ?? 0.0,
            DeepLinkUrl:         deepLink,
            DistanceToStationKm: 0.0 // Rakuten small response does not include distance
        );
    }

    private static string BuildDeepLink(string baseUrl, HotelSearchParams p, string affiliateId)
    {
        var separator = baseUrl.Contains('?') ? "&" : "?";
        var url = $"{baseUrl}{separator}f_tedate={p.CheckIn:yyyyMMdd}&f_sydate={p.CheckOut:yyyyMMdd}&f_otona_su={p.Travelers}";
        if (!string.IsNullOrEmpty(affiliateId))
            url = $"https://hb.afl.rakuten.co.jp/hgc/{affiliateId}/?pc={Uri.EscapeDataString(url)}";
        return url;
    }

    private static (int min, int max) GetPriceRange(string tier) => tier.ToLowerInvariant() switch
    {
        "budget"  => (0, 8_000),
        "luxury"  => (25_001, 9_999_999),
        _         => (8_001, 25_000)  // mid (default)
    };

    // ── Rakuten API response shape (responseType=small) ──────────────────────

    private record RakutenSearchResponse(
        [property: JsonPropertyName("hotels")] List<RakutenHotelWrapper>? Hotels);

    private record RakutenHotelWrapper(
        [property: JsonPropertyName("hotel")] List<RakutenHotelEntry>? Hotel);

    private record RakutenHotelEntry(
        [property: JsonPropertyName("hotelBasicInfo")] RakutenHotelBasicInfo? HotelBasicInfo);

    private record RakutenHotelBasicInfo(
        [property: JsonPropertyName("hotelNo")]              int    HotelNo,
        [property: JsonPropertyName("hotelName")]            string? HotelName,
        [property: JsonPropertyName("hotelThumbnailUrl")]    string? HotelThumbnailUrl,
        [property: JsonPropertyName("hotelInformationUrl")]  string? HotelInformationUrl,
        [property: JsonPropertyName("reviewAverage")]        double? ReviewAverage,
        [property: JsonPropertyName("reviewCount")]          int?    ReviewCount,
        [property: JsonPropertyName("hotelMinCharge")]       int?    HotelMinCharge
    );
}
