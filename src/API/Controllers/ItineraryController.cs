using Microsoft.AspNetCore.Mvc;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;
using WhereToStayInJapan.Application.Validation;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/itinerary")]
public class ItineraryController(IItineraryParsingService parsingService) : ControllerBase
{
    [HttpPost("parse")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
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

        await using var stream = file.OpenReadStream();
        var result = await parsingService.ParseFileAsync(stream, file.FileName, ct);
        return Ok(result);
    }
}

public record ParseTextRequest(string Text);
