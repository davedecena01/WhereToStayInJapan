using Microsoft.EntityFrameworkCore;
using WhereToStayInJapan.Domain.Entities;

namespace WhereToStayInJapan.Infrastructure.Persistence.Repositories;

public class AttractionRepository(ApplicationDbContext db) : IAttractionRepository
{
    public async Task<IReadOnlyList<CuratedAttraction>> GetCuratedAttractionsAsync(Guid stationAreaId, int limit = 10, CancellationToken ct = default)
        => await db.CuratedAttractions
            .Where(a => a.StationAreaId == stationAreaId)
            .Take(limit)
            .ToListAsync(ct);
}
