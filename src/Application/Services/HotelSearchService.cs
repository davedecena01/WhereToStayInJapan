using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.Application.Services;

public class HotelSearchService(
    IHotelProvider hotelProvider,
    IStationAreaRepository areaRepository) : IHotelSearchService
{
    private static readonly DateOnly DefaultCheckIn  = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
    private static readonly DateOnly DefaultCheckOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(37));

    public async Task<HotelSearchResultDto> SearchAsync(
        Guid areaId,
        UserPreferencesDto preferences,
        int page = 1,
        CancellationToken ct = default)
    {
        var area = await areaRepository.GetByIdAsync(areaId, ct);
        if (area is null) return Empty(page);

        var checkIn  = preferences.CheckIn  ?? DefaultCheckIn;
        var checkOut = preferences.CheckOut ?? DefaultCheckOut;
        if (checkOut <= checkIn) checkOut = checkIn.AddDays(1);

        var searchParams = new HotelSearchParams(
            Lat:        (double)area.StationLat,
            Lng:        (double)area.StationLng,
            CheckIn:    checkIn,
            CheckOut:   checkOut,
            BudgetTier: preferences.BudgetTier,
            Travelers:  preferences.Travelers,
            Page:       page,
            PageSize:   10);

        try
        {
            var result = await hotelProvider.SearchAsync(searchParams, ct);
            var dtos   = result.Select(MapToDto).ToList();

            return new HotelSearchResultDto(
                Hotels:   dtos,
                Total:    dtos.Count,
                Page:     page,
                PageSize: 10,
                HasMore:  dtos.Count == 10,
                Provider: "rakuten");
        }
        catch
        {
            return Empty(page);
        }
    }

    private static HotelItemDto MapToDto(HotelItem h) => new(
        HotelId:             h.HotelId,
        Name:                h.Name,
        ThumbnailUrl:        h.ImageUrl,
        PricePerNightJpy:    h.PricePerNightJpy,
        ReviewScore:         h.ReviewRating > 0 ? h.ReviewRating : null,
        ReviewCount:         null,
        DistanceToStationKm: h.DistanceToStationKm > 0 ? h.DistanceToStationKm : null,
        DeepLinkUrl:         h.DeepLinkUrl,
        Address:             null);

    private static HotelSearchResultDto Empty(int page) =>
        new(Hotels: [], Total: 0, Page: page, PageSize: 10, HasMore: false, Provider: "unavailable");
}
