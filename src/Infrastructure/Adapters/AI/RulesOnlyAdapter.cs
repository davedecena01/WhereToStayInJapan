using System.Text.RegularExpressions;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Domain.Models;
using WhereToStayInJapan.Shared.Constants;

namespace WhereToStayInJapan.Infrastructure.Adapters.AI;

public partial class RulesOnlyAdapter : IAIProvider
{
    [GeneratedRegex(@"(?:Day\s*\d+|Visit|Go to|At|In|Explore)\s+([A-Z][a-zA-Z\s\-]+?)(?:[,.]|$)", RegexOptions.Multiline)]
    private static partial Regex LocationPattern();

    public Task<ParsedItinerary> ParseItineraryAsync(string rawText, CancellationToken ct = default)
    {
        var destinations = new List<Destination>();
        var matches = LocationPattern().Matches(rawText);

        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value.Trim();
            if (name.Length < 3) continue;

            var city = RegionMappings.CityToRegion.Keys
                .FirstOrDefault(k => rawText.Contains(k, StringComparison.OrdinalIgnoreCase));

            destinations.Add(new Destination
            {
                Name = name,
                City = city,
                Region = city != null && RegionMappings.CityToRegion.TryGetValue(city, out var r) ? r : null,
                IsAmbiguous = true
            });
        }

        var regions = destinations
            .Where(d => d.Region != null)
            .Select(d => d.Region!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(new ParsedItinerary
        {
            Destinations = destinations,
            RegionsDetected = regions,
            IsMultiRegion = regions.Count > 1,
            ParsingConfidence = "low",
            ClarificationNeeded = true,
            RawText = rawText
        });
    }

    public Task<string> GenerateExplanationAsync(string areaName, string city, IEnumerable<string> destinations, CancellationToken ct = default)
        => Task.FromResult($"{areaName} is a central area in {city} with good transport links.");

    public Task<IReadOnlyList<string>> SuggestFoodAsync(string areaName, string city, int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> SuggestAttractionsAsync(string areaName, string city, int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>([]);
}
