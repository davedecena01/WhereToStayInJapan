namespace WhereToStayInJapan.Infrastructure.Adapters.Maps;

public record GeoPoint(double Lat, double Lng);

public interface IGeocodeProvider
{
    Task<GeoPoint?> GeocodeAsync(string placeName, CancellationToken ct = default);
}
