namespace WhereToStayInJapan.Domain.Models;

public class Destination
{
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Region { get; set; }
    public int? DayNumber { get; set; }
    public string? ActivityType { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public bool IsAmbiguous { get; set; }
}
