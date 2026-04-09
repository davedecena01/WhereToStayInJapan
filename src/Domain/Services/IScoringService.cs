using WhereToStayInJapan.Domain.Entities;
using WhereToStayInJapan.Domain.Models;

namespace WhereToStayInJapan.Domain.Services;

public interface IScoringService
{
    IReadOnlyList<ScoredCandidate> ScoreCandidates(
        IReadOnlyList<StationArea> candidates,
        TravelTimeMatrix travelTimes,
        UserPreferences preferences);
}
