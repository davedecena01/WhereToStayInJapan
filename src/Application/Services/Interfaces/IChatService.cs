using WhereToStayInJapan.Application.DTOs;

namespace WhereToStayInJapan.Application.Services.Interfaces;

public interface IChatService
{
    Task<ChatResponseDto> SendMessageAsync(
        string sessionId,
        string message,
        ParsedItineraryDto? currentItinerary,
        CancellationToken ct = default);
}

public record ChatResponseDto(
    string Message,
    ParsedItineraryDto? UpdatedItinerary,
    bool HasItineraryUpdate
);
