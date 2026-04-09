namespace WhereToStayInJapan.Domain.Entities;

public class HotelSearchCache
{
    public Guid Id { get; set; }
    public string CacheKey { get; set; } = string.Empty;
    public Guid AreaId { get; set; }
    public DateOnly CheckinDate { get; set; }
    public DateOnly CheckoutDate { get; set; }
    public string BudgetTier { get; set; } = string.Empty;
    public string ResultsJson { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public StationArea Area { get; set; } = null!;
}
