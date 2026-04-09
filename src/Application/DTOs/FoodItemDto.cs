namespace WhereToStayInJapan.Application.DTOs;

public record FoodItemDto(
    string Name,
    string CuisineType,
    string? Address,
    double? Lat,
    double? Lng,
    string? Notes,
    bool IsFeatured
);
