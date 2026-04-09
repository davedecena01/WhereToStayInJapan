namespace WhereToStayInJapan.Domain.Entities;

public class StationArea
{
    public Guid Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public string Station { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public decimal StationLat { get; set; }
    public decimal StationLng { get; set; }
    public string? Description { get; set; }
    public int AvgHotelPriceJpy { get; set; }
    public decimal FoodAccessScore { get; set; }
    public decimal ShoppingScore { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<CuratedFood> CuratedFood { get; set; } = [];
    public ICollection<CuratedAttraction> CuratedAttractions { get; set; } = [];
}
