namespace WhereToStayInJapan.Domain.Entities;

public class RecommendationLog
{
    public Guid Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string InputHash { get; set; } = string.Empty;
    public string[] TopAreas { get; set; } = [];
    public int RegionCount { get; set; } = 1;
    public bool AiUsed { get; set; }
    public bool HotelsFetched { get; set; }
    public int? DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
