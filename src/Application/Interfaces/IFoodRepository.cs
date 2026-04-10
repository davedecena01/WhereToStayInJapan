using WhereToStayInJapan.Domain.Entities;

namespace WhereToStayInJapan.Application.Interfaces;

public interface IFoodRepository
{
    Task<IReadOnlyList<CuratedFood>> GetCuratedFoodAsync(Guid stationAreaId, int limit = 8, CancellationToken ct = default);
}
