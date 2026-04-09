namespace WhereToStayInJapan.Domain.Models;

public class UserPreferences
{
    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public int Travelers { get; set; } = 1;
    public string BudgetTier { get; set; } = "mid";
    public List<string> PreferredAtmosphere { get; set; } = [];
    public bool AvoidLongWalking { get; set; }
    public string? MustBeNearStation { get; set; }
}
