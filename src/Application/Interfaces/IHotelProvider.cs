namespace WhereToStayInJapan.Application.Interfaces;

public record HotelItem(
    string HotelId,
    string Name,
    string? ImageUrl,
    decimal PricePerNightJpy,
    double ReviewRating,
    string DeepLinkUrl,
    double DistanceToStationKm);

public record HotelSearchParams(
    double Lat,
    double Lng,
    DateOnly CheckIn,
    DateOnly CheckOut,
    string BudgetTier,
    int Page = 1,
    int PageSize = 10);

public interface IHotelProvider
{
    Task<IReadOnlyList<HotelItem>> SearchAsync(HotelSearchParams searchParams, CancellationToken ct = default);
}
