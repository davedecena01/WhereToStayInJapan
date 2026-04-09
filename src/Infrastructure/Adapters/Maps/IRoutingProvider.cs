namespace WhereToStayInJapan.Infrastructure.Adapters.Maps;

public record RoutingResult(int DurationMins, decimal DistanceKm);

public interface IRoutingProvider
{
    Task<RoutingResult?> GetTravelTimeAsync(GeoPoint origin, GeoPoint destination, string travelMode = "driving", CancellationToken ct = default);
}
