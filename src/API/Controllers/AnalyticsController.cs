using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WhereToStayInJapan.Infrastructure.Persistence;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/analytics")]
[EnableRateLimiting("analytics")]
public class AnalyticsController(
    ApplicationDbContext db,
    ILogger<AnalyticsController> logger) : ControllerBase
{
    [HttpPost("hotel-click")]
    public async Task<IActionResult> TrackHotelClick([FromBody] HotelClickRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
            return BadRequest(new { error = "SessionId is required." });

        if (request.SessionId.Length > 128 || request.HotelId?.Length > 128)
            return BadRequest(new { error = "SessionId and HotelId must not exceed 128 characters." });

        try
        {
            db.HotelClickLogs.Add(new Domain.Entities.HotelClickLog
            {
                SessionId = request.SessionId,
                HotelId   = request.HotelId,
                AreaId    = request.AreaId
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Hotel click log write failed (non-critical)");
        }

        return NoContent();
    }
}

public record HotelClickRequest(string SessionId, string? HotelId, Guid? AreaId);
