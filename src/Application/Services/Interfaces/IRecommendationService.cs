using WhereToStayInJapan.Application.DTOs;

namespace WhereToStayInJapan.Application.Services.Interfaces;

public interface IRecommendationService
{
    Task<RecommendationResultDto> GetRecommendationsAsync(
        ParsedItineraryDto itinerary,
        UserPreferencesDto preferences,
        CancellationToken ct = default);
}
