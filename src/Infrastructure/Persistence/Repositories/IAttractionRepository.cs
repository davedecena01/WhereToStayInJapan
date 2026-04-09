using WhereToStayInJapan.Domain.Entities;

namespace WhereToStayInJapan.Infrastructure.Persistence.Repositories;

public interface IAttractionRepository
{
    Task<IReadOnlyList<CuratedAttraction>> GetCuratedAttractionsAsync(Guid stationAreaId, int limit = 10, CancellationToken ct = default);
}
