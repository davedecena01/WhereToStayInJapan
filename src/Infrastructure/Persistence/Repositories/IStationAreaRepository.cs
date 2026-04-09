using WhereToStayInJapan.Domain.Entities;

namespace WhereToStayInJapan.Infrastructure.Persistence.Repositories;

public interface IStationAreaRepository
{
    Task<IReadOnlyList<StationArea>> GetByRegionsAsync(IEnumerable<string> regions, CancellationToken ct = default);
    Task<StationArea?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
