using Microsoft.AspNetCore.Mvc;
using WhereToStayInJapan.Infrastructure.Persistence;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost("hotel-click")]
    public async Task<IActionResult> TrackHotelClickAsync(
        [FromBody] HotelClickRequest request,
        CancellationToken ct)
    {
        db.HotelClickLogs.Add(new Domain.Entities.HotelClickLog
        {
            SessionId = request.SessionId,
            HotelId = request.HotelId,
            AreaId = request.AreaId
        });

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record HotelClickRequest(string SessionId, string HotelId, Guid? AreaId);
