using WhereToStayInJapan.Infrastructure.Cache;

namespace WhereToStayInJapan.Infrastructure.Adapters.Maps;

public class CachedRoutingProvider(IRoutingProvider inner, ICacheService cache) : IRoutingProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(7);

    public async Task<RoutingResult?> GetTravelTimeAsync(GeoPoint origin, GeoPoint destination, string travelMode = "driving", CancellationToken ct = default)
    {
        var key = $"routing:{SeededFallbackRoutingProvider.BuildKey(origin, destination, travelMode)}";
        return await cache.GetOrSetAsync<RoutingResult?>(
            key,
            async c => await inner.GetTravelTimeAsync(origin, destination, travelMode, c),
            Ttl,
            ct);
    }
}
