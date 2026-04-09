using WhereToStayInJapan.Application.DTOs;

namespace WhereToStayInJapan.Application.Services.Interfaces;

public interface IItineraryParsingService
{
    Task<ParsedItineraryDto> ParseTextAsync(string rawText, CancellationToken ct = default);
    Task<ParsedItineraryDto> ParseFileAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}
