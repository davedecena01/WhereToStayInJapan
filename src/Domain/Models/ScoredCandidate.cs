using WhereToStayInJapan.Domain.Entities;

namespace WhereToStayInJapan.Domain.Models;

public class ScoredCandidate
{
    public StationArea Area { get; set; } = null!;
    public double TotalScore { get; set; }
    public ScoreBreakdown Breakdown { get; set; } = new();
    public double? AvgTravelTimeMinutes { get; set; }
}
