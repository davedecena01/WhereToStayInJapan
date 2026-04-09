// Placeholder — implemented in Phase 2
namespace WhereToStayInJapan.Infrastructure.Adapters.Maps;

public class NominatimAdapter(HttpClient http) : IGeocodeProvider
{
    public Task<GeoPoint?> GeocodeAsync(string placeName, CancellationToken ct = default)
        => throw new NotImplementedException("NominatimAdapter not yet implemented. Set Maps:GeocodeProvider to 'mock'.");
}
