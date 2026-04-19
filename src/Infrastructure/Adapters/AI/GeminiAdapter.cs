using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Polly;
using Polly.Retry;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Domain.Models;

namespace WhereToStayInJapan.Infrastructure.Adapters.AI;

public class GeminiAdapter(HttpClient http, string apiKey, string modelId) : IAIProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>(ex =>
                    ex.StatusCode == HttpStatusCode.TooManyRequests ||
                    ex.StatusCode == HttpStatusCode.ServiceUnavailable),
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(2)
        })
        .Build();

    public async Task<ParsedItinerary> ParseItineraryAsync(string rawText, CancellationToken ct = default)
    {
        var prompt = $$"""
            You are a travel itinerary parser. The input may be a structured day-by-day itinerary OR free-form narrative prose describing travel plans — both are valid input formats.

            Return ONLY a valid JSON object with this exact structure (no markdown, no explanation):
            {
              "destinations": [
                { "name": "place name", "city": "city name", "region": "Kanto|Kansai|Chubu|etc", "dayNumber": null, "activityType": "sightseeing|food|accommodation|transport|null" }
              ],
              "regionsDetected": ["Kanto"],
              "isMultiRegion": false,
              "startDate": "YYYY-MM-DD or null",
              "endDate": "YYYY-MM-DD or null",
              "parsingConfidence": "high|medium|low",
              "clarificationNeeded": false
            }

            Extraction rules:
            - Extract ALL tourist destinations, cities, districts, landmarks, and named places mentioned — even ones referenced in passing
            - For narrative prose (no explicit day structure): use dayNumber null for all entries
            - For structured itineraries (e.g. "Day 1:", "Day 2:"): use the written day number
            - Look for implicit destinations — "heading to Osaka" → extract Osaka; "see Fuji" → extract Mt. Fuji; "Kyoto temples" → extract Kyoto
            - When in doubt, include the place — over-extraction is better than missing destinations
            - Use Japanese region names: Kanto (Tokyo area), Kansai (Kyoto/Osaka/Nara), Chubu (Nagoya/Fuji), Tohoku, Kyushu, Hokkaido, Chugoku
            - parsingConfidence is "high" if specific places are clearly named, "medium" if some are vague, "low" if no identifiable places found
            - clarificationNeeded is true ONLY if the text contains no identifiable Japanese places at all

            Narrative example: "We want to visit Fushimi Inari and Arashiyama in Kyoto, then do Dotonbori in Osaka"
            → extracts: Fushimi Inari (city: Kyoto, region: Kansai), Arashiyama (city: Kyoto, region: Kansai), Dotonbori (city: Osaka, region: Kansai)

            Itinerary text:
            {{rawText}}
            """;

        var responseText = await CallGeminiAsync(prompt, ct);
        return ParseItineraryResponse(responseText, rawText);
    }

    public async Task<ParsedItinerary> EditItineraryAsync(
        string instruction, ParsedItinerary current, CancellationToken ct = default)
    {
        var currentJson = JsonSerializer.Serialize(
            current.Destinations.Select(d => new
            {
                name = d.Name,
                city = d.City,
                region = d.Region,
                dayNumber = d.DayNumber,
                activityType = d.ActivityType
            }),
            JsonOpts);

        var prompt = $$"""
            You are an itinerary editor. Apply the user's edit instruction to the destination list below.

            Current destinations (JSON):
            {{currentJson}}

            User instruction: "{{instruction}}"

            Rules:
            - Apply ONLY the requested change — do not add, remove, or reorder anything else
            - Keep all existing fields (name, city, region, dayNumber, activityType) unchanged unless the instruction targets them
            - Return ONLY a valid JSON object with this exact structure (no markdown, no explanation):
            {
              "destinations": [ { "name": "...", "city": "...", "region": "...", "dayNumber": null, "activityType": "..." } ],
              "regionsDetected": ["Kanto"],
              "isMultiRegion": false,
              "startDate": null,
              "endDate": null,
              "parsingConfidence": "high",
              "clarificationNeeded": false
            }
            """;

        var responseText = await CallGeminiAsync(prompt, ct);
        var edited = ParseItineraryResponse(responseText, current.RawText);

        // If AI returned nothing useful, return the original unchanged
        return edited.Destinations.Count > 0 ? edited : current;
    }

    public async Task<string> GenerateExplanationAsync(
        string areaName, string city, IEnumerable<string> destinations, CancellationToken ct = default)
    {
        var destList = string.Join(", ", destinations);
        var prompt = $"""
            Write a 2-3 sentence explanation of why {areaName} in {city}, Japan is a great base for a tourist visiting: {destList}.
            Focus on travel convenience, transit access, and neighbourhood character.
            Be specific and helpful. Do not use bullet points. Return only the explanation text.
            """;

        return await CallGeminiAsync(prompt, ct);
    }

    public async Task<IReadOnlyList<string>> SuggestFoodAsync(
        string areaName, string city, int count, CancellationToken ct = default)
    {
        var prompt = $"""
            List {count} specific food recommendations near {areaName} in {city}, Japan.
            Return ONLY a JSON array of strings, each being a concise restaurant or dish recommendation with location context.
            Example: ["Ichiran Ramen at Shinjuku Station", "Tsukiji Outer Market sushi breakfast"]
            No markdown, no explanation — just the JSON array.
            """;

        var json = await CallGeminiAsync(prompt, ct);
        return ParseStringList(json, count, areaName, city, "food");
    }

    public async Task<IReadOnlyList<string>> SuggestAttractionsAsync(
        string areaName, string city, int count, CancellationToken ct = default)
    {
        var prompt = $"""
            List {count} notable attractions or activities near {areaName} in {city}, Japan.
            Return ONLY a JSON array of strings, each being a concise attraction name with brief context.
            Example: ["Senso-ji Temple — historic Buddhist temple in Asakusa", "Shibuya Crossing — world's busiest scramble"]
            No markdown, no explanation — just the JSON array.
            """;

        var json = await CallGeminiAsync(prompt, ct);
        return ParseStringList(json, count, areaName, city, "attraction");
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private async Task<string> CallGeminiAsync(string promptText, CancellationToken ct)
    {
        var url = $"v1beta/models/{modelId}:generateContent?key={apiKey}";
        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = promptText } } }
            },
            generationConfig = new { temperature = 0.2, maxOutputTokens = 2048 }
        };

        return await _pipeline.ExecuteAsync(async token =>
        {
            using var response = await http.PostAsJsonAsync(url, requestBody, JsonOpts, token);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new HttpRequestException("Gemini rate limited", null, HttpStatusCode.TooManyRequests);

            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOpts, token)
                ?? throw new InvalidOperationException("Gemini returned empty response");

            var text = geminiResponse.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                ?? throw new InvalidOperationException("Gemini response has no text content");

            return text.Trim();
        }, ct);
    }

    private static ParsedItinerary ParseItineraryResponse(string responseText, string rawText)
    {
        try
        {
            var cleaned = StripMarkdownJson(responseText);
            var dto = JsonSerializer.Deserialize<GeminiParsedItinerary>(cleaned, JsonOpts);

            if (dto is null) return FallbackItinerary(rawText);

            return new ParsedItinerary
            {
                RawText = rawText,
                ParsingConfidence = dto.ParsingConfidence ?? "low",
                ClarificationNeeded = dto.ClarificationNeeded,
                IsMultiRegion = dto.IsMultiRegion,
                RegionsDetected = dto.RegionsDetected ?? [],
                StartDate = ParseDateOnly(dto.StartDate),
                EndDate = ParseDateOnly(dto.EndDate),
                Destinations = (dto.Destinations ?? []).Select(d => new Destination
                {
                    Name = d.Name ?? string.Empty,
                    City = d.City,
                    Region = d.Region,
                    DayNumber = d.DayNumber,
                    ActivityType = d.ActivityType
                }).ToList()
            };
        }
        catch
        {
            return FallbackItinerary(rawText);
        }
    }

    private static IReadOnlyList<string> ParseStringList(
        string responseText, int expectedCount, string areaName, string city, string kind)
    {
        try
        {
            var cleaned = StripMarkdownJson(responseText);
            var items = JsonSerializer.Deserialize<List<string>>(cleaned, JsonOpts);
            if (items is { Count: > 0 }) return items;
        }
        catch { /* fall through to defaults */ }

        return kind == "food"
            ? [$"Local restaurant near {areaName} Station", $"Ramen shop in {city}", $"Izakaya near {areaName}"]
            : [$"Local park near {areaName}", $"Temple in {city}", $"Market near {areaName} Station"];
    }

    private static string StripMarkdownJson(string text)
    {
        var t = text.Trim();
        if (t.StartsWith("```json")) t = t[7..];
        else if (t.StartsWith("```")) t = t[3..];
        if (t.EndsWith("```")) t = t[..^3];
        return t.Trim();
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "null") return null;
        return DateOnly.TryParse(value, out var d) ? d : null;
    }

    private static ParsedItinerary FallbackItinerary(string rawText) => new()
    {
        RawText = rawText,
        ParsingConfidence = "low",
        ClarificationNeeded = true,
        Destinations = [],
        RegionsDetected = []
    };

    // ── Gemini response models ─────────────────────────────────────────────────

    private sealed record GeminiResponse(
        [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content);

    private sealed record GeminiContent(
        [property: JsonPropertyName("parts")] List<GeminiPart>? Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string? Text);

    // ── Parse response DTO ────────────────────────────────────────────────────

    private sealed class GeminiParsedItinerary
    {
        [JsonPropertyName("destinations")] public List<GeminiDestination>? Destinations { get; init; }
        [JsonPropertyName("regionsDetected")] public List<string>? RegionsDetected { get; init; }
        [JsonPropertyName("isMultiRegion")] public bool IsMultiRegion { get; init; }
        [JsonPropertyName("startDate")] public string? StartDate { get; init; }
        [JsonPropertyName("endDate")] public string? EndDate { get; init; }
        [JsonPropertyName("parsingConfidence")] public string? ParsingConfidence { get; init; }
        [JsonPropertyName("clarificationNeeded")] public bool ClarificationNeeded { get; init; }
    }

    private sealed class GeminiDestination
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("city")] public string? City { get; init; }
        [JsonPropertyName("region")] public string? Region { get; init; }
        [JsonPropertyName("dayNumber")] public int? DayNumber { get; init; }
        [JsonPropertyName("activityType")] public string? ActivityType { get; init; }
    }
}
