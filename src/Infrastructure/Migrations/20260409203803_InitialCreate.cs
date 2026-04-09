using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereToStayInJapan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_response_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    InputHash = table.Column<string>(type: "text", nullable: false),
                    PromptType = table.Column<string>(type: "text", nullable: false),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_response_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "geocode_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NormalizedKey = table.Column<string>(type: "text", nullable: false),
                    RawQuery = table.Column<string>(type: "text", nullable: false),
                    Lat = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    Lng = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geocode_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recommendation_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    InputHash = table.Column<string>(type: "text", nullable: false),
                    TopAreas = table.Column<string[]>(type: "text[]", nullable: false),
                    RegionCount = table.Column<int>(type: "integer", nullable: false),
                    AiUsed = table.Column<bool>(type: "boolean", nullable: false),
                    HotelsFetched = table.Column<bool>(type: "boolean", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommendation_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "routing_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CacheKey = table.Column<string>(type: "text", nullable: false),
                    OriginLat = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    OriginLng = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    DestLat = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    DestLng = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    TravelMode = table.Column<string>(type: "text", nullable: false),
                    DurationMins = table.Column<int>(type: "integer", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "station_areas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    City = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: false),
                    AreaName = table.Column<string>(type: "text", nullable: false),
                    Station = table.Column<string>(type: "text", nullable: false),
                    Lat = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    Lng = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    StationLat = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    StationLng = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AvgHotelPriceJpy = table.Column<int>(type: "integer", nullable: false),
                    FoodAccessScore = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    ShoppingScore = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_station_areas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "curated_attractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    StationAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Lat = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Lng = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    WalkMinutes = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_curated_attractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_curated_attractions_station_areas_StationAreaId",
                        column: x => x.StationAreaId,
                        principalTable: "station_areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "curated_food",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    StationAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CuisineType = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Lat = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Lng = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_curated_food", x => x.Id);
                    table.ForeignKey(
                        name: "FK_curated_food_station_areas_StationAreaId",
                        column: x => x.StationAreaId,
                        principalTable: "station_areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_click_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    HotelId = table.Column<string>(type: "text", nullable: false),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    AreaName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_click_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_click_logs_station_areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "station_areas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "hotel_search_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CacheKey = table.Column<string>(type: "text", nullable: false),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckinDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckoutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BudgetTier = table.Column<string>(type: "text", nullable: false),
                    ResultsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_search_cache", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hotel_search_cache_station_areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "station_areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_ai_response_cache_hash",
                table: "ai_response_cache",
                column: "InputHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ai_response_cache_type",
                table: "ai_response_cache",
                column: "PromptType");

            migrationBuilder.CreateIndex(
                name: "idx_curated_attractions_area",
                table: "curated_attractions",
                column: "StationAreaId");

            migrationBuilder.CreateIndex(
                name: "idx_curated_food_area",
                table: "curated_food",
                column: "StationAreaId");

            migrationBuilder.CreateIndex(
                name: "idx_geocode_cache_key",
                table: "geocode_cache",
                column: "NormalizedKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_hotel_click_logs_created",
                table: "hotel_click_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_hotel_click_logs_session",
                table: "hotel_click_logs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_click_logs_AreaId",
                table: "hotel_click_logs",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "idx_hotel_search_cache_key",
                table: "hotel_search_cache",
                column: "CacheKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hotel_search_cache_AreaId",
                table: "hotel_search_cache",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "idx_recommendation_logs_created",
                table: "recommendation_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_recommendation_logs_session",
                table: "recommendation_logs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "idx_routing_cache_key",
                table: "routing_cache",
                column: "CacheKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_station_areas_city",
                table: "station_areas",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "idx_station_areas_region",
                table: "station_areas",
                column: "Region");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_response_cache");

            migrationBuilder.DropTable(
                name: "curated_attractions");

            migrationBuilder.DropTable(
                name: "curated_food");

            migrationBuilder.DropTable(
                name: "geocode_cache");

            migrationBuilder.DropTable(
                name: "hotel_click_logs");

            migrationBuilder.DropTable(
                name: "hotel_search_cache");

            migrationBuilder.DropTable(
                name: "recommendation_logs");

            migrationBuilder.DropTable(
                name: "routing_cache");

            migrationBuilder.DropTable(
                name: "station_areas");
        }
    }
}
