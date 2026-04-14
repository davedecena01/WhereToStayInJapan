using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WhereToStayInJapan.Infrastructure.Adapters.Maps;

public class OsrmAdapter(HttpClient http) : IRoutingProvider
{
    public async Task<RoutingResult?> GetTravelTimeAsync(
        GeoPoint origin, GeoPoint destination, string travelMode = "driving", CancellationToken ct = default)
    {
        try
        {
            var url = $"route/v1/driving/{origin.Lng},{origin.Lat};{destination.Lng},{destination.Lat}?overview=false";
            var response = await http.GetFromJsonAsync<OsrmResponse>(url, ct);

            var route = response?.Routes?.FirstOrDefault();
            if (route is null) return null;

            var durationMins = (int)Math.Ceiling(route.Duration / 60.0);
            var distanceKm = Math.Round((decimal)(route.Distance / 1000.0), 2);
            return new RoutingResult(durationMins, distanceKm);
        }
        catch
        {
            return null;
        }
    }

    private sealed record OsrmResponse(
        [property: JsonPropertyName("routes")] List<OsrmRoute>? Routes);

    private sealed record OsrmRoute(
        [property: JsonPropertyName("duration")] double Duration,
        [property: JsonPropertyName("distance")] double Distance);
}
