using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.Application.Services;

// Stub — full implementation in Phase 4
public class HotelSearchService : IHotelSearchService
{
    public Task<HotelSearchResultDto> SearchAsync(
        Guid areaId,
        UserPreferencesDto preferences,
        int page = 1,
        CancellationToken ct = default)
        => Task.FromResult(new HotelSearchResultDto(Hotels: [], Page: page, PageSize: 10, HasMore: false));
}
