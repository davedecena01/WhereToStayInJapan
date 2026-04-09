using Microsoft.EntityFrameworkCore;
using WhereToStayInJapan.Domain.Entities;

namespace WhereToStayInJapan.Infrastructure.Persistence.Repositories;

public class StationAreaRepository(ApplicationDbContext db) : IStationAreaRepository
{
    public async Task<IReadOnlyList<StationArea>> GetByRegionsAsync(IEnumerable<string> regions, CancellationToken ct = default)
    {
        var regionList = regions.ToList();
        return await db.StationAreas
            .Where(a => a.IsActive && regionList.Contains(a.Region))
            .ToListAsync(ct);
    }

    public Task<StationArea?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.StationAreas.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => db.StationAreas.CountAsync(ct);
}
