using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/recommendations")]
[EnableRateLimiting("recommend")]
public class RecommendationController(IRecommendationService recommendationService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RecommendationResultDto>> GetRecommendationsAsync(
        [FromBody] RecommendationRequest request,
        CancellationToken ct)
    {
        var result = await recommendationService.GetRecommendationsAsync(
            request.Itinerary, request.Preferences, ct);
        return Ok(result);
    }
}

public record RecommendationRequest(ParsedItineraryDto Itinerary, UserPreferencesDto Preferences);
