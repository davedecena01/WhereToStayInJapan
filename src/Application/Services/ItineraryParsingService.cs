using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.Application.Services;

// Stub — full implementation in Phase 1
public class ItineraryParsingService : IItineraryParsingService
{
    public Task<ParsedItineraryDto> ParseTextAsync(string rawText, CancellationToken ct = default)
        => Task.FromResult(new ParsedItineraryDto(
            Destinations: [],
            RegionsDetected: [],
            IsMultiRegion: false,
            StartDate: null,
            EndDate: null,
            ParsingConfidence: "low",
            ClarificationNeeded: true,
            RawText: rawText));

    public Task<ParsedItineraryDto> ParseFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
        => Task.FromResult(new ParsedItineraryDto(
            Destinations: [],
            RegionsDetected: [],
            IsMultiRegion: false,
            StartDate: null,
            EndDate: null,
            ParsingConfidence: "low",
            ClarificationNeeded: true,
            RawText: null));
}
