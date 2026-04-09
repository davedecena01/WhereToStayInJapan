namespace WhereToStayInJapan.Domain.Entities;

public class AiResponseCache
{
    public Guid Id { get; set; }
    public string InputHash { get; set; } = string.Empty;
    public string PromptType { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
