using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WhereToStayInJapan.Infrastructure.Cache;

namespace WhereToStayInJapan.Infrastructure.Adapters.Hotels;

public class CachedHotelProvider(IHotelProvider inner, ICacheService cache) : IHotelProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    public async Task<IReadOnlyList<HotelItem>> SearchAsync(HotelSearchParams searchParams, CancellationToken ct = default)
    {
        var key = BuildKey(searchParams);
        return await cache.GetOrSetAsync<IReadOnlyList<HotelItem>>(
            key,
            async c => await inner.SearchAsync(searchParams, c),
            Ttl,
            ct) ?? [];
    }

    private static string BuildKey(HotelSearchParams p)
    {
        var raw = $"{p.Lat:F6},{p.Lng:F6}:{p.CheckIn}:{p.CheckOut}:{p.BudgetTier}:{p.Page}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"hotels:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
