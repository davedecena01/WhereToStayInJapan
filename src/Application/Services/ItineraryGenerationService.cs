using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.Application.Services;

public class ItineraryGenerationService(IAIProvider ai) : IItineraryGenerationService
{
    public async Task<ParsedItineraryDto> GenerateItineraryAsync(
        ItineraryGenerationRequestDto request, CancellationToken ct = default)
    {
        var parsed = await ai.GenerateItineraryAsync(request, ct);
        return new ParsedItineraryDto(
            Destinations: parsed.Destinations.Select(d => new DestinationDto(
                d.Name, d.City, d.Region, d.DayNumber, d.ActivityType,
                d.Lat, d.Lng, d.IsAmbiguous)).ToList(),
            RegionsDetected: parsed.RegionsDetected,
            IsMultiRegion: parsed.IsMultiRegion,
            StartDate: parsed.StartDate,
            EndDate: parsed.EndDate,
            ParsingConfidence: parsed.ParsingConfidence,
            ClarificationNeeded: parsed.ClarificationNeeded,
            RawText: null);
    }
}
