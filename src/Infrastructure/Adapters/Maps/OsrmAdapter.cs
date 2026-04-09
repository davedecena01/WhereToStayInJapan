// Placeholder — implemented in Phase 2
namespace WhereToStayInJapan.Infrastructure.Adapters.Maps;

public class OsrmAdapter(HttpClient http) : IRoutingProvider
{
    public Task<RoutingResult?> GetTravelTimeAsync(GeoPoint origin, GeoPoint destination, string travelMode = "driving", CancellationToken ct = default)
        => throw new NotImplementedException("OsrmAdapter not yet implemented. Set Maps:RoutingProvider to 'mock'.");
}
