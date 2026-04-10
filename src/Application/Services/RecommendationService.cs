using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Application.Services.Interfaces;
using WhereToStayInJapan.Domain.Entities;
using WhereToStayInJapan.Domain.Models;
using WhereToStayInJapan.Domain.Services;

namespace WhereToStayInJapan.Application.Services;

public class RecommendationService(
    IStationAreaRepository stationAreaRepo,
    IGeocodeProvider geocodeProvider,
    IRoutingProvider routingProvider,
    IScoringService scoringService,
    IHotelProvider hotelProvider,
    IAIProvider aiProvider,
    IFoodRepository foodRepo,
    IAttractionRepository attractionRepo) : IRecommendationService
{
    public async Task<RecommendationResultDto> GetRecommendationsAsync(
        ParsedItineraryDto itinerary,
        UserPreferencesDto preferences,
        CancellationToken ct = default)
    {
        var regions = itinerary.RegionsDetected.Count > 0
            ? itinerary.RegionsDetected
            : ["Kanto"];

        var candidates = await stationAreaRepo.GetByRegionsAsync(regions, ct);
        if (candidates.Count == 0)
            return new RecommendationResultDto([], itinerary.IsMultiRegion, itinerary.RegionsDetected, null);

        var travelMatrix = await BuildTravelMatrixAsync(candidates, itinerary.Destinations, ct);
        var domainPrefs = MapPreferences(preferences);
        var scored = scoringService.ScoreCandidates(candidates, travelMatrix, domainPrefs);

        var enrichTasks = scored.Select((sc, i) =>
            EnrichAsync(sc, i + 1, itinerary.Destinations, preferences, ct));
        var recommendations = await Task.WhenAll(enrichTasks);

        var warning = itinerary.IsMultiRegion
            ? "Your itinerary spans multiple regions. Consider separate stay bases for each region."
            : null;

        return new RecommendationResultDto(
            [.. recommendations],
            itinerary.IsMultiRegion,
            itinerary.RegionsDetected,
            warning);
    }

    private async Task<TravelTimeMatrix> BuildTravelMatrixAsync(
        IReadOnlyList<StationArea> candidates,
        List<DestinationDto> destinations,
        CancellationToken ct)
    {
        var matrix = new TravelTimeMatrix();

        // Geocode unique destination names first to avoid redundant calls
        var geocodedPoints = new Dictionary<string, GeoPoint?>(StringComparer.OrdinalIgnoreCase);
        foreach (var dest in destinations)
        {
            if (geocodedPoints.ContainsKey(dest.Name)) continue;

            GeoPoint? point = dest.Lat.HasValue && dest.Lng.HasValue
                ? new GeoPoint(dest.Lat.Value, dest.Lng.Value)
                : await geocodeProvider.GeocodeAsync(dest.Name, ct);

            geocodedPoints[dest.Name] = point;
        }

        foreach (var candidate in candidates)
        {
            var origin = new GeoPoint((double)candidate.Lat, (double)candidate.Lng);
            foreach (var (destName, destPoint) in geocodedPoints)
            {
                if (destPoint is null) continue;
                var routing = await routingProvider.GetTravelTimeAsync(origin, destPoint, "transit", ct);
                matrix.Set(candidate.Id, destName, routing?.DurationMins);
            }
        }

        return matrix;
    }

    private async Task<StayAreaRecommendationDto> EnrichAsync(
        ScoredCandidate sc,
        int rank,
        List<DestinationDto> destinations,
        UserPreferencesDto preferences,
        CancellationToken ct)
    {
        var area = sc.Area;
        var destNames = destinations.Select(d => d.Name).ToList();

        var hotelTask = FetchHotelsAsync(area, preferences, ct);
        var explanationTask = aiProvider.GenerateExplanationAsync(area.AreaName, area.City, destNames, ct);
        var foodTask = foodRepo.GetCuratedFoodAsync(area.Id, 5, ct);
        var attractionTask = attractionRepo.GetCuratedAttractionsAsync(area.Id, 5, ct);

        await Task.WhenAll(hotelTask, explanationTask, foodTask, attractionTask);

        var hotels = hotelTask.Result;

        return new StayAreaRecommendationDto(
            AreaId: area.Id,
            AreaName: area.AreaName,
            City: area.City,
            Region: area.Region,
            Station: area.Station,
            Rank: rank,
            TotalScore: Math.Round(sc.TotalScore, 3),
            ScoreBreakdown: new ScoreBreakdownDto(
                Math.Round(sc.Breakdown.TravelTimeScore, 3),
                Math.Round(sc.Breakdown.CostScore, 3),
                Math.Round(sc.Breakdown.StationProximityScore, 3),
                Math.Round(sc.Breakdown.FoodAccessScore, 3),
                Math.Round(sc.Breakdown.ShoppingScore, 3)),
            AvgTravelTimeMinutes: sc.AvgTravelTimeMinutes.HasValue
                ? (int)Math.Round(sc.AvgTravelTimeMinutes.Value)
                : null,
            AvgHotelPriceJpy: area.AvgHotelPriceJpy,
            Explanation: explanationTask.Result,
            Pros: GeneratePros(sc),
            Cons: GenerateCons(sc),
            FeaturedFood: foodTask.Result.Select(MapFood).ToList(),
            FeaturedAttractions: attractionTask.Result.Select(MapAttraction).ToList(),
            HotelPreview: hotels,
            HotelsAvailable: hotels.Count > 0);
    }

    private async Task<List<HotelItemDto>> FetchHotelsAsync(
        StationArea area, UserPreferencesDto prefs, CancellationToken ct)
    {
        try
        {
            var searchParams = new HotelSearchParams(
                Lat: (double)area.Lat,
                Lng: (double)area.Lng,
                CheckIn: prefs.CheckIn ?? DateOnly.FromDateTime(DateTime.Today),
                CheckOut: prefs.CheckOut ?? DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
                BudgetTier: prefs.BudgetTier,
                PageSize: 3);

            var hotels = await hotelProvider.SearchAsync(searchParams, ct);
            return hotels.Select(h => new HotelItemDto(
                h.HotelId, h.Name, h.ImageUrl, h.PricePerNightJpy,
                h.ReviewRating, null, h.DistanceToStationKm, h.DeepLinkUrl, null)).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static List<string> GeneratePros(ScoredCandidate sc)
    {
        var pros = new List<string>();

        if (sc.Breakdown.TravelTimeScore >= 0.7)
            pros.Add("Shortest average travel time to your destinations");
        if (sc.Breakdown.CostScore >= 0.7)
            pros.Add("Budget-friendly accommodation options");
        if (sc.Breakdown.FoodAccessScore >= 0.7)
            pros.Add("Excellent food and dining scene");
        if (sc.Breakdown.StationProximityScore >= 0.7)
            pros.Add("Walking distance to major transit hub");
        if (sc.Breakdown.ShoppingScore >= 0.7)
            pros.Add("Great shopping access nearby");

        // Ensure at least 2 pros
        if (pros.Count < 2)
        {
            pros.Add("Good balance of convenience and accessibility");
            if (pros.Count < 2)
                pros.Add("Wide range of accommodation options");
        }

        return pros.Take(4).ToList();
    }

    private static List<string> GenerateCons(ScoredCandidate sc)
    {
        var cons = new List<string>();

        if (sc.Breakdown.TravelTimeScore < 0.4)
            cons.Add("Longer travel times to some destinations");
        if (sc.Breakdown.CostScore < 0.4)
            cons.Add("Higher accommodation costs than other areas");
        if (sc.Breakdown.ShoppingScore < 0.4)
            cons.Add("Limited shopping options nearby");

        if (cons.Count == 0)
            cons.Add("Busier and more crowded during peak tourist season");

        return cons.Take(3).ToList();
    }

    private static FoodItemDto MapFood(CuratedFood f) =>
        new(f.Name, f.CuisineType, f.Address,
            f.Lat.HasValue ? (double)f.Lat.Value : null,
            f.Lng.HasValue ? (double)f.Lng.Value : null,
            f.Notes, f.IsFeatured);

    private static AttractionItemDto MapAttraction(CuratedAttraction a) =>
        new(a.Name, a.Category, a.WalkMinutes, a.Notes);

    private static UserPreferences MapPreferences(UserPreferencesDto dto) => new()
    {
        CheckIn = dto.CheckIn,
        CheckOut = dto.CheckOut,
        Travelers = dto.Travelers,
        BudgetTier = dto.BudgetTier,
        PreferredAtmosphere = dto.PreferredAtmosphere,
        AvoidLongWalking = dto.AvoidLongWalking
    };
}
