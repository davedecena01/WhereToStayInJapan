using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.Application.Services;

// Stub — full implementation in Phase 3
public class ChatService : IChatService
{
    public Task<ChatResponseDto> SendMessageAsync(
        string sessionId,
        string message,
        ParsedItineraryDto? currentItinerary,
        CancellationToken ct = default)
        => Task.FromResult(new ChatResponseDto(
            Message: "AI chat is not yet available. Please refine your itinerary manually.",
            UpdatedItinerary: null,
            HasItineraryUpdate: false));
}
