using WhereToStayInJapan.Domain.Entities;

namespace WhereToStayInJapan.Application.Interfaces;

public interface IAttractionRepository
{
    Task<IReadOnlyList<CuratedAttraction>> GetCuratedAttractionsAsync(Guid stationAreaId, int limit = 10, CancellationToken ct = default);
}
