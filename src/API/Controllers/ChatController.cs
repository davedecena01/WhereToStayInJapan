using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.RateLimiting;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/chat")]
[EnableRateLimiting("chat")]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponseDto>> ChatAsync(
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        if (request.Message.Length > 2000)
            return BadRequest(new { error = "Message must not exceed 2000 characters." });

        if (request.SessionId?.Length > 128)
            return BadRequest(new { error = "SessionId must not exceed 128 characters." });

        var result = await chatService.SendMessageAsync(
            request.SessionId, request.Message, request.CurrentItinerary, ct);
        return Ok(result);
    }
}

public record ChatRequest(string? SessionId, string Message, [ValidateNever] ParsedItineraryDto? CurrentItinerary);
