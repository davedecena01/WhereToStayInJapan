using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhereToStayInJapan.Infrastructure.Persistence;

namespace WhereToStayInJapan.Infrastructure.Cache;

public class PostgresCacheService(ApplicationDbContext db, ILogger<PostgresCacheService> logger) : ICacheService
{
    // Generic key-value cache using ai_response_cache table as backing store
    // Each entry is stored as JSON with a synthetic prompt_type of "generic"

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var entry = await db.AiResponseCaches
            .Where(e => e.InputHash == key && e.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        if (entry == null) return default;

        logger.LogDebug("Cache hit: {Key}", key);
        return JsonSerializer.Deserialize<T>(entry.ResponseJson);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        var now = DateTime.UtcNow;

        var existing = await db.AiResponseCaches
            .FirstOrDefaultAsync(e => e.InputHash == key, ct);

        if (existing != null)
        {
            existing.ResponseJson = json;
            existing.ExpiresAt = now.Add(ttl);
            existing.CachedAt = now;
        }
        else
        {
            db.AiResponseCaches.Add(new Domain.Entities.AiResponseCache
            {
                InputHash = key,
                PromptType = "generic",
                ResponseJson = json,
                Provider = "cache",
                CachedAt = now,
                ExpiresAt = now.Add(ttl)
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan ttl, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached != null) return cached;

        var value = await factory(ct);
        if (value != null)
            await SetAsync(key, value, ttl, ct);

        return value;
    }
}
