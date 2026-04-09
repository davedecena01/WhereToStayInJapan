namespace WhereToStayInJapan.Domain.Entities;

public class GeocodeCache
{
    public Guid Id { get; set; }
    public string NormalizedKey { get; set; } = string.Empty;
    public string RawQuery { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public string Provider { get; set; } = "nominatim";
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
