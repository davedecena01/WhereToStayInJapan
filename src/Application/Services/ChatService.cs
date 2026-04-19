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
        // If message looks like a new itinerary paste, re-parse it entirely
        if (LooksLikeItineraryText(message))
        {
            var parsed = await aiProvider.ParseItineraryAsync(message, ct);
            return new ChatResponseDto(
                Message: "I've parsed your itinerary. Please review and confirm the details below.",
                UpdatedItinerary: ToDto(parsed),
                HasItineraryUpdate: true);
        }

        // If message looks like an edit command (remove/add/move/etc.), apply it to current itinerary
        if (LooksLikeEditCommand(message) && currentItinerary is { Destinations.Count: > 0 })
        {
            var current = ToDomain(currentItinerary);
            var edited = await aiProvider.EditItineraryAsync(message, current, ct);
            return new ChatResponseDto(
                Message: "Done! I've updated your itinerary. Does it look right?",
                UpdatedItinerary: ToDto(edited),
                HasItineraryUpdate: true);
        }

        // General question — generate a contextual explanation
        var explanation = await aiProvider.GenerateExplanationAsync(
            areaName: ExtractFirstArea(currentItinerary),
            city: ExtractFirstCity(currentItinerary),
            destinations: ExtractDestinationNames(currentItinerary),
            ct);

        return new ChatResponseDto(
            Message: $"{explanation}\n\nIs there anything specific about your itinerary you'd like to adjust?",
            UpdatedItinerary: null,
            HasItineraryUpdate: false);
    }

    private static bool LooksLikeEditCommand(string message)
    {
        var lower = message.ToLowerInvariant();
        return lower.StartsWith("remove ") || lower.StartsWith("add ") || lower.StartsWith("move ")
            || lower.StartsWith("delete ") || lower.StartsWith("change ") || lower.StartsWith("rename ")
            || lower.StartsWith("swap ") || lower.StartsWith("put ") || lower.StartsWith("transfer ")
            || lower.Contains(" remove ") || lower.Contains(" add ") || lower.Contains(" move ")
            || lower.Contains(" delete ") || lower.Contains(" change day");
    }

    private static bool LooksLikeItineraryText(string message)
    {
        // Heuristic: longer messages with day/date references suggest itinerary input
        if (message.Length < 80) return false;
        var lower = message.ToLowerInvariant();
        return lower.Contains("day ") || lower.Contains("visit") || lower.Contains("arrive")
            || lower.Contains("depart") || lower.Contains("hotel") || lower.Contains("night");
    }

    private static string ExtractFirstArea(ParsedItineraryDto? dto)
        => dto?.Destinations.FirstOrDefault()?.Name ?? "your area";

    private static string ExtractFirstCity(ParsedItineraryDto? dto)
        => dto?.Destinations.FirstOrDefault()?.City ?? "Japan";

    private static IEnumerable<string> ExtractDestinationNames(ParsedItineraryDto? dto)
        => dto?.Destinations.Select(d => d.Name) ?? [];

    private static ParsedItinerary ToDomain(ParsedItineraryDto dto) => new()
    {
        RawText = dto.RawText ?? string.Empty,
        ParsingConfidence = dto.ParsingConfidence ?? "high",
        ClarificationNeeded = dto.ClarificationNeeded,
        IsMultiRegion = dto.IsMultiRegion,
        RegionsDetected = dto.RegionsDetected ?? [],
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        Destinations = dto.Destinations.Select(d => new Destination
        {
            Name = d.Name,
            City = d.City,
            Region = d.Region,
            DayNumber = d.DayNumber,
            ActivityType = d.ActivityType
        }).ToList()
    };

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
