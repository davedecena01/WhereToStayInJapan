using WhereToStayInJapan.Application.DTOs;

namespace WhereToStayInJapan.Application.Services.Interfaces;

public interface IItineraryGenerationService
{
    Task<ParsedItineraryDto> GenerateItineraryAsync(ItineraryGenerationRequestDto request, CancellationToken ct = default);
}
