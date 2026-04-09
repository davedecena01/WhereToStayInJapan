using WhereToStayInJapan.Infrastructure.Cache;
using WhereToStayInJapan.Shared.Extensions;
using System.Security.Cryptography;
using System.Text;

namespace WhereToStayInJapan.Infrastructure.Adapters.Maps;

public class SeededFallbackRoutingProvider(ICacheService cache) : IRoutingProvider
{
    public async Task<RoutingResult?> GetTravelTimeAsync(GeoPoint origin, GeoPoint destination, string travelMode = "driving", CancellationToken ct = default)
    {
        var key = BuildKey(origin, destination, travelMode);
        return await cache.GetAsync<RoutingResult>(key, ct);
    }

    public static string BuildKey(GeoPoint origin, GeoPoint dest, string mode)
    {
        var raw = $"{origin.Lat},{origin.Lng}:{dest.Lat},{dest.Lng}:{mode}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
