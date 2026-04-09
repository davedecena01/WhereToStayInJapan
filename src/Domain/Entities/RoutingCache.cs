namespace WhereToStayInJapan.Domain.Entities;

public class RoutingCache
{
    public Guid Id { get; set; }
    public string CacheKey { get; set; } = string.Empty;
    public decimal OriginLat { get; set; }
    public decimal OriginLng { get; set; }
    public decimal DestLat { get; set; }
    public decimal DestLng { get; set; }
    public string TravelMode { get; set; } = "driving";
    public int DurationMins { get; set; }
    public decimal DistanceKm { get; set; }
    public string Provider { get; set; } = "osrm";
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
