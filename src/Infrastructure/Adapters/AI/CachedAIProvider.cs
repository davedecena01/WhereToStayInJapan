using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WhereToStayInJapan.Domain.Models;
using WhereToStayInJapan.Infrastructure.Cache;
using WhereToStayInJapan.Shared.Extensions;

namespace WhereToStayInJapan.Infrastructure.Adapters.AI;

public class CachedAIProvider(IAIProvider inner, ICacheService cache) : IAIProvider
{
    private static readonly TimeSpan ParseTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan ExplainTtl = TimeSpan.FromHours(48);

    public async Task<ParsedItinerary> ParseItineraryAsync(string rawText, CancellationToken ct = default)
    {
        var key = BuildHash("parse_itinerary", rawText.NormalizeKey());
        return await cache.GetOrSetAsync<ParsedItinerary>(
            key,
            async c => await inner.ParseItineraryAsync(rawText, c),
            ParseTtl,
            ct) ?? await inner.ParseItineraryAsync(rawText, ct);
    }

    public async Task<string> GenerateExplanationAsync(string areaName, string city, IEnumerable<string> destinations, CancellationToken ct = default)
    {
        var input = $"{areaName}:{city}:{string.Join(",", destinations)}";
        var key = BuildHash("generate_explanation", input.NormalizeKey());
        return await cache.GetOrSetAsync<string>(
            key,
            async c => await inner.GenerateExplanationAsync(areaName, city, destinations, c),
            ExplainTtl,
            ct) ?? await inner.GenerateExplanationAsync(areaName, city, destinations, ct);
    }

    public async Task<IReadOnlyList<string>> SuggestFoodAsync(string areaName, string city, int count, CancellationToken ct = default)
    {
        var key = BuildHash("suggest_food", $"{areaName}:{city}:{count}".NormalizeKey());
        return await cache.GetOrSetAsync<IReadOnlyList<string>>(
            key,
            async c => await inner.SuggestFoodAsync(areaName, city, count, c),
            ExplainTtl,
            ct) ?? [];
    }

    public async Task<IReadOnlyList<string>> SuggestAttractionsAsync(string areaName, string city, int count, CancellationToken ct = default)
    {
        var key = BuildHash("suggest_attractions", $"{areaName}:{city}:{count}".NormalizeKey());
        return await cache.GetOrSetAsync<IReadOnlyList<string>>(
            key,
            async c => await inner.SuggestAttractionsAsync(areaName, city, count, c),
            ExplainTtl,
            ct) ?? [];
    }

    private static string BuildHash(string promptType, string normalizedInput)
    {
        var raw = $"{promptType}:{normalizedInput}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
