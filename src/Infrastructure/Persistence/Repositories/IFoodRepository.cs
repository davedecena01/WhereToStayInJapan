using WhereToStayInJapan.Domain.Entities;

namespace WhereToStayInJapan.Infrastructure.Persistence.Repositories;

public interface IFoodRepository
{
    Task<IReadOnlyList<CuratedFood>> GetCuratedFoodAsync(Guid stationAreaId, int limit = 8, CancellationToken ct = default);
}
