using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WhereToStayInJapan.Infrastructure.Adapters.Maps;

public class NominatimAdapter(HttpClient http) : IGeocodeProvider
{
    public async Task<GeoPoint?> GeocodeAsync(string placeName, CancellationToken ct = default)
    {
        try
        {
            var encoded = Uri.EscapeDataString($"{placeName} Japan");
            var url = $"search?q={encoded}&format=json&limit=1&countrycodes=jp";

            var results = await http.GetFromJsonAsync<NominatimResult[]>(url, ct);
            var first = results?.FirstOrDefault();
            if (first is null) return null;

            return double.TryParse(first.Lat, out var lat) && double.TryParse(first.Lon, out var lon)
                ? new GeoPoint(lat, lon)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private sealed record NominatimResult(
        [property: JsonPropertyName("lat")] string Lat,
        [property: JsonPropertyName("lon")] string Lon);
}
