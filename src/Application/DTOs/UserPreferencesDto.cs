namespace WhereToStayInJapan.Application.DTOs;

public record UserPreferencesDto(
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int Travelers,
    string BudgetTier,
    List<string> PreferredAtmosphere,
    bool AvoidLongWalking,
    bool? MustBeNearStation
);
