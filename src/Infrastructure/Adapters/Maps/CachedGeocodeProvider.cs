using WhereToStayInJapan.Infrastructure.Cache;
using WhereToStayInJapan.Shared.Extensions;

namespace WhereToStayInJapan.Infrastructure.Adapters.Maps;

public class CachedGeocodeProvider(IGeocodeProvider inner, ICacheService cache) : IGeocodeProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(90);

    public async Task<GeoPoint?> GeocodeAsync(string placeName, CancellationToken ct = default)
    {
        var key = $"geocode:{placeName.NormalizeKey()}";
        return await cache.GetOrSetAsync<GeoPoint?>(
            key,
            async c => await inner.GeocodeAsync(placeName, c),
            Ttl,
            ct);
    }
}
