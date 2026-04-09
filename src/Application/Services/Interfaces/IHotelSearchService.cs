using WhereToStayInJapan.Application.DTOs;

namespace WhereToStayInJapan.Application.Services.Interfaces;

public interface IHotelSearchService
{
    Task<HotelSearchResultDto> SearchAsync(
        Guid areaId,
        UserPreferencesDto preferences,
        int page = 1,
        CancellationToken ct = default);
}
