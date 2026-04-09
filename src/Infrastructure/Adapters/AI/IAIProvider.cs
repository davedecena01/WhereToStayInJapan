using WhereToStayInJapan.Domain.Models;

namespace WhereToStayInJapan.Infrastructure.Adapters.AI;

public interface IAIProvider
{
    Task<ParsedItinerary> ParseItineraryAsync(string rawText, CancellationToken ct = default);
    Task<string> GenerateExplanationAsync(string areaName, string city, IEnumerable<string> destinations, CancellationToken ct = default);
    Task<IReadOnlyList<string>> SuggestFoodAsync(string areaName, string city, int count, CancellationToken ct = default);
    Task<IReadOnlyList<string>> SuggestAttractionsAsync(string areaName, string city, int count, CancellationToken ct = default);
}
