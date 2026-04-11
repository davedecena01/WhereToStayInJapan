using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WhereToStayInJapan.Infrastructure.Persistence;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController(
    ApplicationDbContext db,
    ILogger<AnalyticsController> logger) : ControllerBase
{
    [HttpPost("hotel-click")]
    public IActionResult TrackHotelClick([FromBody] HotelClickRequest request)
    {
        // Fire-and-forget: do not block or fail the response on DB errors
        _ = Task.Run(async () =>
        {
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
        });

        return NoContent();
    }
}

public record HotelClickRequest(string SessionId, string HotelId, Guid? AreaId);
