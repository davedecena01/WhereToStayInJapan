using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Application.Services.Interfaces;
using WhereToStayInJapan.Domain.Models;

namespace WhereToStayInJapan.Application.Services;

public class ChatService(IAIProvider aiProvider) : IChatService
{
    public async Task<ChatResponseDto> SendMessageAsync(
        string sessionId,
        string message,
        ParsedItineraryDto? currentItinerary,
        CancellationToken ct = default)
    {
        // If message looks like a new itinerary or user asks to re-parse, attempt AI parse
        if (LooksLikeItineraryText(message))
        {
            var parsed = await aiProvider.ParseItineraryAsync(message, ct);
            var updatedDto = ToDto(parsed);
            return new ChatResponseDto(
                Message: "I've parsed your itinerary. Please review and confirm the details below.",
                UpdatedItinerary: updatedDto,
                HasItineraryUpdate: true);
        }

        // General chat — respond based on itinerary context
        var context = BuildContext(currentItinerary);
        var explanation = await aiProvider.GenerateExplanationAsync(
            areaName: ExtractFirstArea(currentItinerary),
            city: ExtractFirstCity(currentItinerary),
            destinations: ExtractDestinationNames(currentItinerary),
            ct);

        var reply = $"{explanation}\n\nIs there anything specific about your itinerary you'd like to adjust?";
        return new ChatResponseDto(Message: reply, UpdatedItinerary: null, HasItineraryUpdate: false);
    }

    private static bool LooksLikeItineraryText(string message)
    {
        // Heuristic: longer messages with day/date references suggest itinerary input
        if (message.Length < 80) return false;
        var lower = message.ToLowerInvariant();
        return lower.Contains("day ") || lower.Contains("visit") || lower.Contains("arrive")
            || lower.Contains("depart") || lower.Contains("hotel") || lower.Contains("night");
    }

    private static string BuildContext(ParsedItineraryDto? itinerary)
    {
        if (itinerary is null || itinerary.Destinations.Count == 0)
            return "no current itinerary";
        return string.Join(", ", itinerary.Destinations.Select(d => d.Name));
    }

    private static string ExtractFirstArea(ParsedItineraryDto? dto)
        => dto?.Destinations.FirstOrDefault()?.Name ?? "your area";

    private static string ExtractFirstCity(ParsedItineraryDto? dto)
        => dto?.Destinations.FirstOrDefault()?.City ?? "Japan";

    private static IEnumerable<string> ExtractDestinationNames(ParsedItineraryDto? dto)
        => dto?.Destinations.Select(d => d.Name) ?? [];

    private static ParsedItineraryDto ToDto(ParsedItinerary parsed) => new(
        Destinations: parsed.Destinations.Select(d => new DestinationDto(
            d.Name, d.City, d.Region, d.DayNumber, d.ActivityType, d.Lat, d.Lng, d.IsAmbiguous
        )).ToList(),
        RegionsDetected: parsed.RegionsDetected,
        IsMultiRegion: parsed.IsMultiRegion,
        StartDate: parsed.StartDate,
        EndDate: parsed.EndDate,
        ParsingConfidence: parsed.ParsingConfidence,
        ClarificationNeeded: parsed.ClarificationNeeded,
        RawText: parsed.RawText
    );
}
