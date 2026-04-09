using Microsoft.AspNetCore.Mvc;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/hotels")]
public class HotelController(IHotelSearchService hotelSearchService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HotelSearchResultDto>> SearchAsync(
        [FromQuery] Guid areaId,
        [FromQuery] string budgetTier = "mid",
        [FromQuery] int travelers = 1,
        [FromQuery] string? checkIn = null,
        [FromQuery] string? checkOut = null,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var preferences = new UserPreferencesDto(
            CheckIn: DateOnly.TryParse(checkIn, out var ci) ? ci : null,
            CheckOut: DateOnly.TryParse(checkOut, out var co) ? co : null,
            Travelers: travelers,
            BudgetTier: budgetTier,
            PreferredAtmosphere: [],
            AvoidLongWalking: false,
            MustBeNearStation: false);

        var result = await hotelSearchService.SearchAsync(areaId, preferences, page, ct);
        return Ok(result);
    }
}
