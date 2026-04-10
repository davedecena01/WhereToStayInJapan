using FluentAssertions;
using WhereToStayInJapan.Domain.Entities;
using WhereToStayInJapan.Domain.Models;
using WhereToStayInJapan.Domain.Services;

namespace WhereToStayInJapan.Domain.Tests;

public class ScoringServiceTests
{
    private static ScoringService CreateService() => new();

    private static StationArea MakeArea(Guid id, int avgHotelPriceJpy, double foodScore = 0.8, double shopScore = 0.6) =>
        new()
        {
            Id = id,
            AreaName = $"Area-{id.ToString()[..4]}",
            City = "Tokyo",
            Region = "Kanto",
            Station = "Station",
            Lat = 35.6m,
            Lng = 139.7m,
            StationLat = 35.6m,
            StationLng = 139.7m,
            AvgHotelPriceJpy = avgHotelPriceJpy,
            FoodAccessScore = (decimal)foodScore,
            ShoppingScore = (decimal)shopScore,
            IsActive = true
        };

    private static TravelTimeMatrix MatrixWith(Guid id, string destName, int? minutes) =>
        MatrixWith([(id, destName, minutes)]);

    private static TravelTimeMatrix MatrixWith(IEnumerable<(Guid id, string dest, int? mins)> entries)
    {
        var matrix = new TravelTimeMatrix();
        foreach (var (id, dest, mins) in entries)
            matrix.Set(id, dest, mins);
        return matrix;
    }

    private static UserPreferences DefaultPrefs() => new()
    {
        BudgetTier = "mid",
        Travelers = 2,
        AvoidLongWalking = false
    };

    // ──────────────────────────────────────────────────────────────────────
    // Empty / single candidate
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ScoreCandidates_EmptyList_ReturnsEmpty()
    {
        var svc = CreateService();
        var result = svc.ScoreCandidates([], new TravelTimeMatrix(), DefaultPrefs());
        result.Should().BeEmpty();
    }

    [Fact]
    public void ScoreCandidates_SingleCandidate_ReturnsThatCandidate()
    {
        var id = Guid.NewGuid();
        var area = MakeArea(id, avgHotelPriceJpy: 10_000);
        var matrix = MatrixWith(id, "Shinjuku", 20);
        var svc = CreateService();

        var result = svc.ScoreCandidates([area], matrix, DefaultPrefs());

        result.Should().HaveCount(1);
        result[0].Area.Should().Be(area);
        result[0].TotalScore.Should().BeInRange(0.0, 1.01);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Lower travel time → higher rank
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ScoreCandidates_LowerTravelTime_RanksHigher()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var areaA = MakeArea(idA, avgHotelPriceJpy: 10_000); // short travel time
        var areaB = MakeArea(idB, avgHotelPriceJpy: 10_000); // long travel time

        var matrix = MatrixWith(
        [
            (idA, "Shibuya", 10),
            (idB, "Shibuya", 60)
        ]);

        var result = CreateService().ScoreCandidates([areaA, areaB], matrix, DefaultPrefs());

        result[0].Area.Id.Should().Be(idA, "area with shorter travel time should rank first");
        result[0].TotalScore.Should().BeGreaterThan(result[1].TotalScore);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Lower cost → higher rank when travel times are equal
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ScoreCandidates_LowerCost_RanksHigherWhenTravelTimesEqual()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var cheap = MakeArea(idA, avgHotelPriceJpy: 5_000);
        var expensive = MakeArea(idB, avgHotelPriceJpy: 30_000);

        // Same travel time for both
        var matrix = MatrixWith(
        [
            (idA, "Asakusa", 20),
            (idB, "Asakusa", 20)
        ]);

        var result = CreateService().ScoreCandidates([cheap, expensive], matrix, DefaultPrefs());

        result[0].Area.Id.Should().Be(idA, "cheaper area should rank first when travel times are equal");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Min-max normalization: single dimension all-equal → scores all 0.5
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ScoreCandidates_AllSameTravelTime_TravelScoreIsHalf()
    {
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList();
        var areas = ids.Select(id => MakeArea(id, avgHotelPriceJpy: 10_000)).ToList();
        var matrix = new TravelTimeMatrix();
        foreach (var id in ids)
            matrix.Set(id, "Tokyo", 30);

        var result = CreateService().ScoreCandidates(areas, matrix, DefaultPrefs());

        result.Should().HaveCount(3);
        result.Select(r => r.Breakdown.TravelTimeScore).Should().AllSatisfy(s =>
            s.Should().BeApproximately(0.5, 1e-9));
    }

    // ──────────────────────────────────────────────────────────────────────
    // AvoidLongWalking increases station proximity weight effect
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ScoreCandidates_AvoidWalking_StationProximityAffectsRanking()
    {
        // idNear: station is at same coords as area (proximity 0 km) → best proximity
        // idFar:  station is offset → worse proximity
        var idNear = Guid.NewGuid();
        var idFar = Guid.NewGuid();

        var near = new StationArea
        {
            Id = idNear, AreaName = "Near", City = "Tokyo", Region = "Kanto",
            Station = "NearStation",
            Lat = 35.6m, Lng = 139.7m,
            StationLat = 35.6m, StationLng = 139.7m,  // 0 km distance
            AvgHotelPriceJpy = 10_000,
            FoodAccessScore = 0.5m, ShoppingScore = 0.5m, IsActive = true
        };

        var far = new StationArea
        {
            Id = idFar, AreaName = "Far", City = "Tokyo", Region = "Kanto",
            Station = "FarStation",
            Lat = 35.6m, Lng = 139.7m,
            StationLat = 35.65m, StationLng = 139.75m, // ~7 km distance
            AvgHotelPriceJpy = 10_000,
            FoodAccessScore = 0.5m, ShoppingScore = 0.5m, IsActive = true
        };

        // Equal travel times, equal costs → only station proximity differentiates
        var matrix = MatrixWith(
        [
            (idNear, "Shibuya", 25),
            (idFar, "Shibuya", 25)
        ]);

        var avoidWalkingPrefs = new UserPreferences { AvoidLongWalking = true, BudgetTier = "mid" };
        var result = CreateService().ScoreCandidates([near, far], matrix, avoidWalkingPrefs);

        result[0].Area.Id.Should().Be(idNear, "area closer to its station should rank first when AvoidLongWalking is true");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Returns at most 5 candidates
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ScoreCandidates_MoreThanFiveCandidates_ReturnsAtMostFive()
    {
        var areas = Enumerable.Range(0, 8).Select(i =>
            MakeArea(Guid.NewGuid(), avgHotelPriceJpy: 10_000 + i * 1_000)).ToList();

        var matrix = new TravelTimeMatrix();
        foreach (var a in areas)
            matrix.Set(a.Id, "Tokyo", 20 + (int)(a.AvgHotelPriceJpy / 1000));

        var result = CreateService().ScoreCandidates(areas, matrix, DefaultPrefs());

        result.Count.Should().BeLessOrEqualTo(5);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Null travel times are treated as worst-case (not ranked first)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ScoreCandidates_NullTravelTime_TreatedAsWorstCase()
    {
        var idGood = Guid.NewGuid();
        var idBad = Guid.NewGuid();

        var goodArea = MakeArea(idGood, 10_000);
        var badArea = MakeArea(idBad, 10_000);

        var matrix = MatrixWith(
        [
            (idGood, "Shibuya", 15),
            (idBad, "Shibuya", null)  // routing failed
        ]);

        var result = CreateService().ScoreCandidates([goodArea, badArea], matrix, DefaultPrefs());

        result[0].Area.Id.Should().Be(idGood, "area with routing data should rank above area with null travel time");
    }

    // ──────────────────────────────────────────────────────────────────────
    // AvgTravelTimeMinutes is populated correctly
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ScoreCandidates_PopulatesAvgTravelTimeMinutes()
    {
        var id = Guid.NewGuid();
        var area = MakeArea(id, 10_000);

        var matrix = new TravelTimeMatrix();
        matrix.Set(id, "Asakusa", 10);
        matrix.Set(id, "Shibuya", 30);

        var result = CreateService().ScoreCandidates([area], matrix, DefaultPrefs());

        result[0].AvgTravelTimeMinutes.Should().BeApproximately(20.0, 0.01);
    }
}
