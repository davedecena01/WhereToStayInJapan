using WhereToStayInJapan.Domain.Models;
using WhereToStayInJapan.Shared.Constants;
using WhereToStayInJapan.Shared.Extensions;

namespace WhereToStayInJapan.Domain.Services;

public class RegionGroupingService
{
    private const double MultiRegionThresholdKm = 100.0;

    public Dictionary<string, List<Destination>> GroupByRegion(IEnumerable<Destination> destinations)
    {
        var groups = new Dictionary<string, List<Destination>>(StringComparer.OrdinalIgnoreCase);
        foreach (var dest in destinations)
        {
            var region = dest.Region
                ?? (dest.City != null && RegionMappings.CityToRegion.TryGetValue(dest.City, out var r) ? r : "Unknown");
            if (!groups.ContainsKey(region))
                groups[region] = [];
            groups[region].Add(dest);
        }
        return groups;
    }

    public bool IsMultiRegion(IEnumerable<Destination> destinations)
    {
        var withCoords = destinations
            .Where(d => d.Lat.HasValue && d.Lng.HasValue)
            .ToList();

        if (withCoords.Count < 2)
            return false;

        for (var i = 0; i < withCoords.Count; i++)
        {
            for (var j = i + 1; j < withCoords.Count; j++)
            {
                var dist = GeoExtensions.HaversineDistance(
                    withCoords[i].Lat!.Value, withCoords[i].Lng!.Value,
                    withCoords[j].Lat!.Value, withCoords[j].Lng!.Value);

                if (dist > MultiRegionThresholdKm)
                    return true;
            }
        }

        return false;
    }

    public static double HaversineDistance(double lat1, double lng1, double lat2, double lng2)
        => GeoExtensions.HaversineDistance(lat1, lng1, lat2, lng2);
}
