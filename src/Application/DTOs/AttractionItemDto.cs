namespace WhereToStayInJapan.Application.DTOs;

public record AttractionItemDto(
    string Name,
    string Category,
    int? WalkMinutes,
    string? Notes
);
