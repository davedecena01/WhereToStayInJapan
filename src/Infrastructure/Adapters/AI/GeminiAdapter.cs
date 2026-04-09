using WhereToStayInJapan.Domain.Models;

namespace WhereToStayInJapan.Infrastructure.Adapters.AI;

// Placeholder — implemented in Phase 3
public class GeminiAdapter(string apiKey, string modelId) : IAIProvider
{
    public Task<ParsedItinerary> ParseItineraryAsync(string rawText, CancellationToken ct = default)
        => throw new NotImplementedException("GeminiAdapter not yet implemented. Set AI:Mode to 'mock' or 'rules_only'.");

    public Task<string> GenerateExplanationAsync(string areaName, string city, IEnumerable<string> destinations, CancellationToken ct = default)
        => throw new NotImplementedException("GeminiAdapter not yet implemented.");

    public Task<IReadOnlyList<string>> SuggestFoodAsync(string areaName, string city, int count, CancellationToken ct = default)
        => throw new NotImplementedException("GeminiAdapter not yet implemented.");

    public Task<IReadOnlyList<string>> SuggestAttractionsAsync(string areaName, string city, int count, CancellationToken ct = default)
        => throw new NotImplementedException("GeminiAdapter not yet implemented.");
}
