using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WhereToStayInJapan.Domain.Entities;
using WhereToStayInJapan.Infrastructure.Persistence;

namespace WhereToStayInJapan.Infrastructure.Seed;

public class DataSeeder(IServiceProvider services, ILogger<DataSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedStationAreasAsync(db, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DataSeeder failed — seed data may need to be applied manually.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task SeedStationAreasAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.StationAreas.AnyAsync(ct))
        {
            logger.LogDebug("Station areas already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding station areas...");

        var seedPath = Path.Combine(AppContext.BaseDirectory, "seed", "station_areas.json");
        if (!File.Exists(seedPath))
        {
            logger.LogWarning("Seed file not found: {Path}", seedPath);
            return;
        }

        var json = await File.ReadAllTextAsync(seedPath, ct);
        var records = JsonSerializer.Deserialize<List<StationAreaSeedRecord>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (records == null || records.Count == 0)
        {
            logger.LogWarning("No station area records found in seed file.");
            return;
        }

        var areas = records.Select(r => new StationArea
        {
            Id = Guid.Parse(r.Id),
            City = r.City,
            Region = r.Region,
            AreaName = r.AreaName,
            Station = r.Station,
            Lat = r.Lat,
            Lng = r.Lng,
            StationLat = r.StationLat,
            StationLng = r.StationLng,
            AvgHotelPriceJpy = r.AvgHotelPriceJpy,
            FoodAccessScore = r.FoodAccessScore,
            ShoppingScore = r.ShoppingScore
        }).ToList();

        db.StationAreas.AddRange(areas);
        await db.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation("Seeded {Count} station areas.", areas.Count);

        await SeedFoodAsync(db, areas, ct);
        await SeedAttractionsAsync(db, areas, ct);
    }

    private async Task SeedFoodAsync(ApplicationDbContext db, List<StationArea> areas, CancellationToken ct)
    {
        var seedPath = Path.Combine(AppContext.BaseDirectory, "seed", "curated_food.json");
        if (!File.Exists(seedPath))
        {
            logger.LogWarning("Food seed file not found: {Path}", seedPath);
            return;
        }

        var json = await File.ReadAllTextAsync(seedPath, ct);
        var records = JsonSerializer.Deserialize<List<FoodSeedRecord>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (records == null || records.Count == 0) return;

        var areaById = areas.ToDictionary(a => a.Id);
        var food = records
            .Where(r => areaById.ContainsKey(Guid.Parse(r.StationAreaId)))
            .Select(r => new CuratedFood
            {
                StationAreaId = Guid.Parse(r.StationAreaId),
                Name = r.Name,
                CuisineType = r.CuisineType,
                Address = r.Address,
                Lat = r.Lat,
                Lng = r.Lng,
                Notes = r.Notes,
                Source = r.Source,
                IsFeatured = r.IsFeatured
            }).ToList();

        db.CuratedFood.AddRange(food);
        await db.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation("Seeded {Count} curated food items.", food.Count);
    }

    private async Task SeedAttractionsAsync(ApplicationDbContext db, List<StationArea> areas, CancellationToken ct)
    {
        var seedPath = Path.Combine(AppContext.BaseDirectory, "seed", "curated_attractions.json");
        if (!File.Exists(seedPath))
        {
            logger.LogWarning("Attractions seed file not found: {Path}", seedPath);
            return;
        }

        var json = await File.ReadAllTextAsync(seedPath, ct);
        var records = JsonSerializer.Deserialize<List<AttractionSeedRecord>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (records == null || records.Count == 0) return;

        var areaById = areas.ToDictionary(a => a.Id);
        var attractions = records
            .Where(r => areaById.ContainsKey(Guid.Parse(r.StationAreaId)))
            .Select(r => new CuratedAttraction
            {
                StationAreaId = Guid.Parse(r.StationAreaId),
                Name = r.Name,
                Category = r.Category,
                WalkMinutes = r.WalkMinutes,
                Notes = r.Notes
            }).ToList();

        db.CuratedAttractions.AddRange(attractions);
        await db.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation("Seeded {Count} curated attractions.", attractions.Count);
    }

    // Private seed record types used only for deserialization
    private record StationAreaSeedRecord(
        string Id, string City, string Region, string AreaName, string Station,
        decimal Lat, decimal Lng, decimal StationLat, decimal StationLng,
        int AvgHotelPriceJpy, decimal FoodAccessScore, decimal ShoppingScore);

    private record FoodSeedRecord(
        string StationAreaId, string Name, string CuisineType, string Address,
        decimal? Lat, decimal? Lng, string Notes, string Source, bool IsFeatured);

    private record AttractionSeedRecord(
        string StationAreaId, string Name, string Category, int? WalkMinutes, string Notes);
}
