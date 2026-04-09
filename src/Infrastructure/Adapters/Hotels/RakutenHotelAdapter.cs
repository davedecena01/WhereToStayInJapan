// Placeholder — implemented in Phase 4
namespace WhereToStayInJapan.Infrastructure.Adapters.Hotels;

public class RakutenHotelAdapter(HttpClient http) : IHotelProvider
{
    public Task<IReadOnlyList<HotelItem>> SearchAsync(HotelSearchParams searchParams, CancellationToken ct = default)
        => throw new NotImplementedException("RakutenHotelAdapter not yet implemented. Set Hotels:Provider to 'mock'.");
}
