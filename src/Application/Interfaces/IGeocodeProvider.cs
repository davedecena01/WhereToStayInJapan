namespace WhereToStayInJapan.Application.Interfaces;

public record GeoPoint(double Lat, double Lng);

public interface IGeocodeProvider
{
    Task<GeoPoint?> GeocodeAsync(string placeName, CancellationToken ct = default);
}
