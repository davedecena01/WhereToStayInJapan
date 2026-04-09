namespace WhereToStayInJapan.Application.DTOs;

public record ErrorResponseDto(
    string Type,
    string Title,
    int Status,
    string? Detail = null,
    Dictionary<string, string[]>? Errors = null
);
