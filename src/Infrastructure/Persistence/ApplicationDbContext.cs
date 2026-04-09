using Microsoft.EntityFrameworkCore;
using WhereToStayInJapan.Domain.Entities;

namespace WhereToStayInJapan.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<StationArea> StationAreas => Set<StationArea>();
    public DbSet<CuratedFood> CuratedFood => Set<CuratedFood>();
    public DbSet<CuratedAttraction> CuratedAttractions => Set<CuratedAttraction>();
    public DbSet<GeocodeCache> GeocodeCaches => Set<GeocodeCache>();
    public DbSet<RoutingCache> RoutingCaches => Set<RoutingCache>();
    public DbSet<AiResponseCache> AiResponseCaches => Set<AiResponseCache>();
    public DbSet<HotelSearchCache> HotelSearchCaches => Set<HotelSearchCache>();
    public DbSet<RecommendationLog> RecommendationLogs => Set<RecommendationLog>();
    public DbSet<HotelClickLog> HotelClickLogs => Set<HotelClickLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // StationArea
        modelBuilder.Entity<StationArea>(e =>
        {
            e.ToTable("station_areas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Lat).HasPrecision(9, 6);
            e.Property(x => x.Lng).HasPrecision(9, 6);
            e.Property(x => x.StationLat).HasPrecision(9, 6);
            e.Property(x => x.StationLng).HasPrecision(9, 6);
            e.Property(x => x.FoodAccessScore).HasPrecision(3, 2);
            e.Property(x => x.ShoppingScore).HasPrecision(3, 2);
            e.HasIndex(x => x.Region).HasDatabaseName("idx_station_areas_region");
            e.HasIndex(x => x.City).HasDatabaseName("idx_station_areas_city");
        });

        // CuratedFood
        modelBuilder.Entity<CuratedFood>(e =>
        {
            e.ToTable("curated_food");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Lat).HasPrecision(9, 6);
            e.Property(x => x.Lng).HasPrecision(9, 6);
            e.HasIndex(x => x.StationAreaId).HasDatabaseName("idx_curated_food_area");
            e.HasOne(x => x.StationArea).WithMany(x => x.CuratedFood).HasForeignKey(x => x.StationAreaId).OnDelete(DeleteBehavior.Cascade);
        });

        // CuratedAttraction
        modelBuilder.Entity<CuratedAttraction>(e =>
        {
            e.ToTable("curated_attractions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Lat).HasPrecision(9, 6);
            e.Property(x => x.Lng).HasPrecision(9, 6);
            e.HasIndex(x => x.StationAreaId).HasDatabaseName("idx_curated_attractions_area");
            e.HasOne(x => x.StationArea).WithMany(x => x.CuratedAttractions).HasForeignKey(x => x.StationAreaId).OnDelete(DeleteBehavior.Cascade);
        });

        // GeocodeCache
        modelBuilder.Entity<GeocodeCache>(e =>
        {
            e.ToTable("geocode_cache");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Lat).HasPrecision(9, 6);
            e.Property(x => x.Lng).HasPrecision(9, 6);
            e.HasIndex(x => x.NormalizedKey).IsUnique().HasDatabaseName("idx_geocode_cache_key");
        });

        // RoutingCache
        modelBuilder.Entity<RoutingCache>(e =>
        {
            e.ToTable("routing_cache");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.OriginLat).HasPrecision(9, 6);
            e.Property(x => x.OriginLng).HasPrecision(9, 6);
            e.Property(x => x.DestLat).HasPrecision(9, 6);
            e.Property(x => x.DestLng).HasPrecision(9, 6);
            e.Property(x => x.DistanceKm).HasPrecision(6, 2);
            e.HasIndex(x => x.CacheKey).IsUnique().HasDatabaseName("idx_routing_cache_key");
        });

        // AiResponseCache
        modelBuilder.Entity<AiResponseCache>(e =>
        {
            e.ToTable("ai_response_cache");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ResponseJson).HasColumnType("jsonb");
            e.HasIndex(x => x.InputHash).IsUnique().HasDatabaseName("idx_ai_response_cache_hash");
            e.HasIndex(x => x.PromptType).HasDatabaseName("idx_ai_response_cache_type");
        });

        // HotelSearchCache
        modelBuilder.Entity<HotelSearchCache>(e =>
        {
            e.ToTable("hotel_search_cache");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ResultsJson).HasColumnType("jsonb");
            e.HasIndex(x => x.CacheKey).IsUnique().HasDatabaseName("idx_hotel_search_cache_key");
            e.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId);
        });

        // RecommendationLog
        modelBuilder.Entity<RecommendationLog>(e =>
        {
            e.ToTable("recommendation_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.SessionId).HasDatabaseName("idx_recommendation_logs_session");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_recommendation_logs_created");
        });

        // HotelClickLog
        modelBuilder.Entity<HotelClickLog>(e =>
        {
            e.ToTable("hotel_click_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.SessionId).HasDatabaseName("idx_hotel_click_logs_session");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_hotel_click_logs_created");
            e.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId).IsRequired(false);
        });
    }
}
