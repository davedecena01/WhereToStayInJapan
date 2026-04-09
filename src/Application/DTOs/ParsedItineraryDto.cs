namespace WhereToStayInJapan.Application.DTOs;

public record ParsedItineraryDto(
    List<DestinationDto> Destinations,
    List<string> RegionsDetected,
    bool IsMultiRegion,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string ParsingConfidence,
    bool ClarificationNeeded,
    string? RawText
);

public record DestinationDto(
    string Name,
    string? City,
    string? Region,
    int? DayNumber,
    string? ActivityType,
    double? Lat,
    double? Lng,
    bool IsAmbiguous
);
