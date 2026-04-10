using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Application.Services.Interfaces;
using WhereToStayInJapan.Domain.Services;

namespace WhereToStayInJapan.Application.Services;

public class ItineraryParsingService(
    IAIProvider ai,
    ItineraryNormalizer normalizer,
    IEnumerable<IItineraryExtractor> extractors) : IItineraryParsingService
{
    public async Task<ParsedItineraryDto> ParseTextAsync(string rawText, CancellationToken ct = default)
    {
        var parsed = await ai.ParseItineraryAsync(rawText, ct);
        var normalized = normalizer.Normalize(parsed);

        return new ParsedItineraryDto(
            Destinations: normalized.Destinations.Select(d => new DestinationDto(
                d.Name, d.City, d.Region, d.DayNumber, d.ActivityType,
                d.Lat, d.Lng, d.IsAmbiguous)).ToList(),
            RegionsDetected: normalized.RegionsDetected,
            IsMultiRegion: normalized.IsMultiRegion,
            StartDate: normalized.StartDate,
            EndDate: normalized.EndDate,
            ParsingConfidence: normalized.ParsingConfidence,
            ClarificationNeeded: normalized.ClarificationNeeded,
            RawText: rawText);
    }

    public async Task<ParsedItineraryDto> ParseFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var extractor = extractors.FirstOrDefault(e => e.CanHandle(fileName));
        if (extractor == null)
            return new ParsedItineraryDto([], [], false, null, null, "low", true, null);

        var text = await extractor.ExtractTextAsync(fileStream, ct);
        return await ParseTextAsync(text, ct);
    }
}
