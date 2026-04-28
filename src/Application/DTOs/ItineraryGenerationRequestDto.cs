namespace WhereToStayInJapan.Application.DTOs;

public record ItineraryGenerationRequestDto(
    string Mode,
    int DurationDays,
    List<string> Regions,
    string? TravelStyle,
    string? BudgetTier,
    string? Pace
);
