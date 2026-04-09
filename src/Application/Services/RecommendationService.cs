using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.Application.Services;

// Stub — full implementation in Phase 2
public class RecommendationService : IRecommendationService
{
    public Task<RecommendationResultDto> GetRecommendationsAsync(
        ParsedItineraryDto itinerary,
        UserPreferencesDto preferences,
        CancellationToken ct = default)
        => Task.FromResult(new RecommendationResultDto(
            Recommendations: [],
            IsMultiRegion: itinerary.IsMultiRegion,
            RegionsDetected: itinerary.RegionsDetected,
            MultiRegionWarning: itinerary.IsMultiRegion
                ? "Your itinerary spans multiple regions. Consider separate stay bases for each region."
                : null));
}
