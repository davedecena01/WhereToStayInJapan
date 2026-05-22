using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/itinerary")]
[EnableRateLimiting("parse")]
public class ItineraryController(
    IItineraryParsingService parsingService,
    IItineraryGenerationService generationService) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".docx", ".txt"];

    // Magic bytes for binary formats; .txt has no reliable signature so skipped
    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        [".pdf"]  = [0x25, 0x50, 0x44, 0x46],  // "%PDF"
        [".docx"] = [0x50, 0x4B, 0x03, 0x04],  // ZIP (Office Open XML)
    };

    [HttpPost("parse")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ParsedItineraryDto>> ParseAsync(
        [FromBody] ParseTextRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { error = "Text is required." });

        var result = await parsingService.ParseTextAsync(request.Text, ct);
        return Ok(result);
    }

    [HttpPost("parse/file")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ParsedItineraryDto>> ParseFileAsync(
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File is required." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { error = "Only PDF, DOCX, and TXT files are supported." });

        if (MagicBytes.TryGetValue(ext, out var magic))
        {
            await using var peek = file.OpenReadStream();
            var header = new byte[magic.Length];
            var read = await peek.ReadAsync(header.AsMemory(0, magic.Length), ct);
            if (read < magic.Length || !header.SequenceEqual(magic))
                return BadRequest(new { error = "File content does not match the declared file type." });
        }

        // Always re-open a fresh stream for parsing — the peek stream above was consumed
        await using var stream = file.OpenReadStream();
        var result = await parsingService.ParseFileAsync(stream, file.FileName, ct);
        return Ok(result);
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ParsedItineraryDto>> GenerateItinerary(
        [FromBody] ItineraryGenerationRequestDto request,
        CancellationToken ct)
    {
        var result = await generationService.GenerateItineraryAsync(request, ct);
        return Ok(result);
    }
}

public record ParseTextRequest(string Text);
