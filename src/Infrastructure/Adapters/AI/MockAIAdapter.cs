using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Domain.Models;

namespace WhereToStayInJapan.Infrastructure.Adapters.AI;

public class MockAIAdapter : IAIProvider
{
    public Task<ParsedItinerary> ParseItineraryAsync(string rawText, CancellationToken ct = default)
    {
        var result = new ParsedItinerary
        {
            ParsingConfidence = "low",
            ClarificationNeeded = true,
            RawText = rawText,
            Destinations =
            [
                new Destination { Name = "Shinjuku", City = "Tokyo", Region = "Kanto", DayNumber = 1 },
                new Destination { Name = "Asakusa", City = "Tokyo", Region = "Kanto", DayNumber = 2 },
                new Destination { Name = "Gion", City = "Kyoto", Region = "Kansai", DayNumber = 3 }
            ],
            RegionsDetected = ["Kanto", "Kansai"],
            IsMultiRegion = true
        };
        return Task.FromResult(result);
    }

    public Task<ParsedItinerary> EditItineraryAsync(string instruction, ParsedItinerary current, CancellationToken ct = default)
        => Task.FromResult(current);

    public Task<ParsedItinerary> GenerateItineraryAsync(ItineraryGenerationRequestDto request, CancellationToken ct = default)
    {
        var result = new ParsedItinerary
        {
            ParsingConfidence = "high",
            ClarificationNeeded = false,
            Destinations =
            [
                new Destination { Name = "Shinjuku", City = "Tokyo", Region = "Kanto", DayNumber = 1 },
                new Destination { Name = "Asakusa", City = "Tokyo", Region = "Kanto", DayNumber = 2 },
                new Destination { Name = "Fushimi Inari", City = "Kyoto", Region = "Kansai", DayNumber = 3 },
                new Destination { Name = "Arashiyama", City = "Kyoto", Region = "Kansai", DayNumber = 4 },
                new Destination { Name = "Dotonbori", City = "Osaka", Region = "Kansai", DayNumber = 5 }
            ],
            RegionsDetected = ["Kanto", "Kansai"],
            IsMultiRegion = true
        };
        return Task.FromResult(result);
    }

    public Task<string> ChatAsync(string message, IEnumerable<string> destinations, CancellationToken ct = default)
        => Task.FromResult("I can help you plan your Japan trip! Feel free to ask about transportation, areas to stay, or anything about your itinerary.");

    public Task<string> GenerateExplanationAsync(string areaName, string city, IEnumerable<string> destinations, CancellationToken ct = default)
        => Task.FromResult($"{areaName} in {city} is an excellent base for your itinerary, offering convenient access to major transit hubs and a wide range of accommodation options.");

    public Task<IReadOnlyList<string>> SuggestFoodAsync(string areaName, string city, int count, CancellationToken ct = default)
    {
        IReadOnlyList<string> suggestions = [$"Ramen shop near {areaName} Station", $"Sushi restaurant in {areaName}", $"Izakaya in {city}"];
        return Task.FromResult(suggestions);
    }

    public Task<IReadOnlyList<string>> SuggestAttractionsAsync(string areaName, string city, int count, CancellationToken ct = default)
    {
        IReadOnlyList<string> suggestions = [$"Local market near {areaName}", $"Park in {city}", $"Temple near {areaName} Station"];
        return Task.FromResult(suggestions);
    }
}
