namespace WhereToStayInJapan.Domain.Models;

public class ParsedItinerary
{
    public List<Destination> Destinations { get; set; } = [];
    public List<string> RegionsDetected { get; set; } = [];
    public bool IsMultiRegion { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string ParsingConfidence { get; set; } = "low";
    public bool ClarificationNeeded { get; set; }
    public string? RawText { get; set; }
}
