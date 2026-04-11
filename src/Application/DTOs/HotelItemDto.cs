namespace WhereToStayInJapan.Application.DTOs;

public record HotelItemDto(
    string HotelId,
    string Name,
    string? ThumbnailUrl,
    decimal PricePerNightJpy,
    double? ReviewScore,
    int? ReviewCount,
    double? DistanceToStationKm,
    string DeepLinkUrl,
    string? Address
);

public record HotelSearchResultDto(
    List<HotelItemDto> Hotels,
    int Total,
    int Page,
    int PageSize,
    bool HasMore,
    string Provider
);
