using Microsoft.EntityFrameworkCore;
using WhereToStayInJapan.Domain.Entities;

namespace WhereToStayInJapan.Infrastructure.Persistence.Repositories;

public class FoodRepository(ApplicationDbContext db) : IFoodRepository
{
    public async Task<IReadOnlyList<CuratedFood>> GetCuratedFoodAsync(Guid stationAreaId, int limit = 8, CancellationToken ct = default)
        => await db.CuratedFood
            .Where(f => f.StationAreaId == stationAreaId)
            .OrderByDescending(f => f.IsFeatured)
            .Take(limit)
            .ToListAsync(ct);
}
