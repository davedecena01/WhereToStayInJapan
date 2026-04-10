namespace WhereToStayInJapan.Application.Interfaces;

public record RoutingResult(int DurationMins, decimal DistanceKm);

public interface IRoutingProvider
{
    Task<RoutingResult?> GetTravelTimeAsync(GeoPoint origin, GeoPoint destination, string travelMode = "driving", CancellationToken ct = default);
}
