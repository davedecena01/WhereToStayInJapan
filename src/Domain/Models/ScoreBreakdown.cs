namespace WhereToStayInJapan.Domain.Models;

public class ScoreBreakdown
{
    public double TravelTimeScore { get; set; }
    public double CostScore { get; set; }
    public double StationProximityScore { get; set; }
    public double FoodAccessScore { get; set; }
    public double ShoppingScore { get; set; }
}
