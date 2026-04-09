namespace WhereToStayInJapan.Infrastructure.Adapters.Maps;

public class MockGeocodeAdapter : IGeocodeProvider
{
    private static readonly Dictionary<string, GeoPoint> KnownPoints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Shinjuku"]    = new(35.6938, 139.7034),
        ["Shibuya"]     = new(35.6580, 139.7016),
        ["Asakusa"]     = new(35.7147, 139.7966),
        ["Akihabara"]   = new(35.7022, 139.7741),
        ["Ueno"]        = new(35.7141, 139.7774),
        ["Ginza"]       = new(35.6717, 139.7650),
        ["Namba"]       = new(34.6687, 135.5019),
        ["Umeda"]       = new(34.7024, 135.4959),
        ["Gion"]        = new(35.0036, 135.7750),
        ["Kyoto"]       = new(34.9854, 135.7590),
        ["Hiroshima"]   = new(34.3853, 132.4553),
    };

    public Task<GeoPoint?> GeocodeAsync(string placeName, CancellationToken ct = default)
    {
        var key = KnownPoints.Keys.FirstOrDefault(k =>
            placeName.Contains(k, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(key != null ? KnownPoints[key] : (GeoPoint?)null);
    }
}
