using WhereToStayInJapan.Domain.Models;
using WhereToStayInJapan.Shared.Constants;
using WhereToStayInJapan.Shared.Extensions;

namespace WhereToStayInJapan.Domain.Services;

public class ItineraryNormalizer(RegionGroupingService regionGrouping)
{
    public ParsedItinerary Normalize(ParsedItinerary itinerary)
    {
        var deduplicated = Deduplicate(itinerary.Destinations);
        var sorted = deduplicated.Any(d => d.DayNumber.HasValue)
            ? deduplicated.OrderBy(d => d.DayNumber ?? int.MaxValue).ToList()
            : deduplicated;

        foreach (var dest in sorted)
        {
            if (dest.Region == null && dest.City != null)
            {
                if (RegionMappings.CityToRegion.TryGetValue(dest.City, out var region))
                    dest.Region = region;
            }
        }

        var regions = sorted
            .Where(d => d.Region != null)
            .Select(d => d.Region!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var isMultiRegion = regionGrouping.IsMultiRegion(sorted) || regions.Count > 1;

        return new ParsedItinerary
        {
            Destinations = sorted,
            RegionsDetected = regions,
            IsMultiRegion = isMultiRegion,
            StartDate = itinerary.StartDate,
            EndDate = itinerary.EndDate,
            ParsingConfidence = itinerary.ParsingConfidence,
            ClarificationNeeded = itinerary.ClarificationNeeded,
            RawText = itinerary.RawText
        };
    }

    private static List<Destination> Deduplicate(List<Destination> destinations)
    {
        var seen = new List<Destination>();
        foreach (var dest in destinations)
        {
            var key = dest.Name.NormalizeKey();
            var isDuplicate = seen.Any(s =>
            {
                var sKey = s.Name.NormalizeKey();
                return sKey == key
                    || (LevenshteinDistance(sKey, key) <= 2
                        && string.Equals(s.City, dest.City, StringComparison.OrdinalIgnoreCase));
            });

            if (!isDuplicate)
                seen.Add(dest);
        }
        return seen;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var dp = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
                dp[i, j] = a[i - 1] == b[j - 1]
                    ? dp[i - 1, j - 1]
                    : 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));

        return dp[a.Length, b.Length];
    }
}
