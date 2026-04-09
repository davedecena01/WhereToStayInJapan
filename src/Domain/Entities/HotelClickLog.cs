namespace WhereToStayInJapan.Domain.Entities;

public class HotelClickLog
{
    public Guid Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string HotelId { get; set; } = string.Empty;
    public Guid? AreaId { get; set; }
    public string? AreaName { get; set; }
    public DateTime CreatedAt { get; set; }

    public StationArea? Area { get; set; }
}
