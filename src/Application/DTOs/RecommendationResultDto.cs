namespace WhereToStayInJapan.Application.DTOs;

public record RecommendationResultDto(
    List<StayAreaRecommendationDto> Recommendations,
    bool IsMultiRegion,
    List<string> RegionsDetected,
    string? MultiRegionWarning
);

public record StayAreaRecommendationDto(
    Guid AreaId,
    string AreaName,
    string City,
    string Region,
    string Station,
    int Rank,
    double TotalScore,
    ScoreBreakdownDto ScoreBreakdown,
    int? AvgTravelTimeMinutes,
    int AvgHotelPriceJpy,
    string? Explanation,
    List<string> Pros,
    List<string> Cons,
    List<FoodItemDto> FeaturedFood,
    List<AttractionItemDto> FeaturedAttractions,
    List<HotelItemDto> HotelPreview,
    bool HotelsAvailable
);

public record ScoreBreakdownDto(
    double TravelTime,
    double Cost,
    double StationProximity,
    double FoodAccess,
    double Shopping
);
