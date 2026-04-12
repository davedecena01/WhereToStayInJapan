using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponseDto>> ChatAsync(
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        var result = await chatService.SendMessageAsync(
            request.SessionId, request.Message, request.CurrentItinerary, ct);
        return Ok(result);
    }
}

public record ChatRequest(string? SessionId, string Message, [ValidateNever] ParsedItineraryDto? CurrentItinerary);
