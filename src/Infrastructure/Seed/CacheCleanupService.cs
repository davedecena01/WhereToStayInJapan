using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WhereToStayInJapan.Infrastructure.Persistence;

namespace WhereToStayInJapan.Infrastructure.Seed;

public class CacheCleanupService(IServiceProvider services, ILogger<CacheCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(Interval, ct);

            try
            {
                await PurgeExpiredAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cache cleanup failed.");
            }
        }
    }

    private async Task PurgeExpiredAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        var aiDeleted = await db.AiResponseCaches
            .Where(e => e.ExpiresAt <= now)
            .ExecuteDeleteAsync(ct);

        var geocodeDeleted = await db.GeocodeCaches
            .Where(e => e.ExpiresAt <= now)
            .ExecuteDeleteAsync(ct);

        var routingDeleted = await db.RoutingCaches
            .Where(e => e.ExpiresAt <= now)
            .ExecuteDeleteAsync(ct);

        var hotelDeleted = await db.HotelSearchCaches
            .Where(e => e.ExpiresAt <= now)
            .ExecuteDeleteAsync(ct);

        var total = aiDeleted + geocodeDeleted + routingDeleted + hotelDeleted;
        if (total > 0)
            logger.LogInformation("Cache cleanup: removed {Total} expired entries (ai={Ai}, geocode={Geo}, routing={Route}, hotels={Hotels}).",
                total, aiDeleted, geocodeDeleted, routingDeleted, hotelDeleted);
    }
}
