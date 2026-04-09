using WhereToStayInJapan.Domain.Entities;
using WhereToStayInJapan.Domain.Models;
using WhereToStayInJapan.Shared.Extensions;

namespace WhereToStayInJapan.Domain.Services;

public class ScoringService : IScoringService
{
    private const double TravelTimeWeight      = 0.40;
    private const double CostWeight            = 0.30;
    private const double StationProximityWeight = 0.15;
    private const double FoodAccessWeight      = 0.10;
    private const double ShoppingWeight        = 0.05;

    public IReadOnlyList<ScoredCandidate> ScoreCandidates(
        IReadOnlyList<StationArea> candidates,
        TravelTimeMatrix travelTimes,
        UserPreferences preferences)
    {
        if (candidates.Count == 0)
            return [];

        var rawScores = candidates.Select(c => ComputeRaw(c, travelTimes)).ToList();

        var normalized = Normalize(candidates, rawScores, preferences);

        return normalized
            .OrderByDescending(s => s.TotalScore)
            .Take(5)
            .ToList();
    }

    private static (double AvgTravelTime, double Cost, double StationProximityKm) ComputeRaw(
        StationArea area, TravelTimeMatrix travelTimes)
    {
        var avgTravel = travelTimes.GetAverage(area.Id) ?? double.MaxValue;
        var cost = (double)area.AvgHotelPriceJpy;
        var proximityKm = GeoExtensions.HaversineDistance(
            (double)area.Lat, (double)area.Lng,
            (double)area.StationLat, (double)area.StationLng);

        return (avgTravel, cost, proximityKm);
    }

    private List<ScoredCandidate> Normalize(
        IReadOnlyList<StationArea> candidates,
        List<(double AvgTravelTime, double Cost, double StationProximityKm)> raw,
        UserPreferences preferences)
    {
        var travelTimes = raw.Select(r => r.AvgTravelTime).ToList();
        var costs = raw.Select(r => r.Cost).ToList();
        var proximities = raw.Select(r => r.StationProximityKm).ToList();

        double stationWeight = StationProximityWeight;
        if (preferences.AvoidLongWalking)
            stationWeight = Math.Min(1.0, StationProximityWeight * 1.5);

        var results = new List<ScoredCandidate>();

        for (var i = 0; i < candidates.Count; i++)
        {
            var area = candidates[i];

            var travelScore  = MinMaxInverse(travelTimes[i], travelTimes);
            var costScore    = MinMaxInverse(costs[i], costs);
            var proxScore    = MinMaxInverse(proximities[i], proximities);
            var foodScore    = (double)area.FoodAccessScore;
            var shopScore    = (double)area.ShoppingScore;

            if (preferences.PreferredAtmosphere.Contains("nightlife", StringComparer.OrdinalIgnoreCase))
                shopScore = Math.Min(1.0, shopScore * 1.2);

            var total = (travelScore * TravelTimeWeight)
                      + (costScore * CostWeight)
                      + (proxScore * stationWeight)
                      + (foodScore * FoodAccessWeight)
                      + (shopScore * ShoppingWeight);

            results.Add(new ScoredCandidate
            {
                Area = area,
                TotalScore = total,
                AvgTravelTimeMinutes = travelTimes[i] == double.MaxValue ? null : travelTimes[i],
                Breakdown = new ScoreBreakdown
                {
                    TravelTimeScore       = travelScore,
                    CostScore             = costScore,
                    StationProximityScore = proxScore,
                    FoodAccessScore       = foodScore,
                    ShoppingScore         = shopScore
                }
            });
        }

        return results;
    }

    private static double MinMaxInverse(double value, List<double> all)
    {
        var min = all.Min();
        var max = all.Max();
        if (Math.Abs(max - min) < 1e-10) return 0.5;
        return 1.0 - (value - min) / (max - min);
    }
}
