using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhereToStayInJapan.Infrastructure.Persistence;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken ct)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            return Ok(new { status = "healthy", db = "connected" });
        }
        catch
        {
            return StatusCode(503, new { status = "unhealthy", db = "unreachable" });
        }
    }
}
