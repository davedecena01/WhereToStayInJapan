namespace WhereToStayInJapan.Domain.Entities;

public class CuratedFood
{
    public Guid Id { get; set; }
    public Guid StationAreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CuisineType { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    public string? Notes { get; set; }
    public string Source { get; set; } = "admin";
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }

    public StationArea StationArea { get; set; } = null!;
}
