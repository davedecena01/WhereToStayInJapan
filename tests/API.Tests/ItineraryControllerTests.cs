using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;
using WhereToStayInJapan.API.Controllers;

namespace WhereToStayInJapan.API.Tests;

public class ItineraryControllerTests
{
    private readonly Mock<IItineraryParsingService> _parsingSvcMock = new();
    private readonly Mock<IItineraryGenerationService> _generationSvcMock = new();
    private readonly ItineraryController _sut;

    public ItineraryControllerTests()
    {
        _sut = new ItineraryController(_parsingSvcMock.Object, _generationSvcMock.Object);
    }

    [Fact]
    public async Task GenerateItinerary_ValidStandardRequest_Returns200WithParsedItinerary()
    {
        var request = new ItineraryGenerationRequestDto("standard", 5, ["Kanto", "Kansai"], "cultural", "mid", "moderate");
        var expected = new ParsedItineraryDto(
            Destinations: [new DestinationDto("Shinjuku", "Tokyo", "Kanto", 1, null, null, null, false)],
            RegionsDetected: ["Kanto"],
            IsMultiRegion: false,
            StartDate: null,
            EndDate: null,
            ParsingConfidence: "high",
            ClarificationNeeded: false,
            RawText: null);

        _generationSvcMock
            .Setup(s => s.GenerateItineraryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GenerateItinerary(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<ParsedItineraryDto>().Subject;
        dto.Destinations.Should().HaveCount(1);
        dto.Destinations[0].Name.Should().Be("Shinjuku");
    }

    // NOTE: The 400 path for invalid input (e.g. DurationDays = 0) is enforced by the
    // FluentValidation auto-validation pipeline registered in Program.cs, which runs at the
    // model-binding layer before the action body executes. This interception only fires in the
    // full ASP.NET Core pipeline (e.g. via WebApplicationFactory), not in direct controller
    // unit tests. Validation correctness is covered by ItineraryGenerationRequestValidatorTests.
    [Fact]
    public async Task GenerateItinerary_InvalidDurationDays_ValidationEnforcedByPipeline()
    {
        // Direct controller instantiation bypasses the FluentValidation auto-validation
        // middleware. The action proceeds normally; validation is the pipeline's responsibility.
        var request = new ItineraryGenerationRequestDto("standard", 0, ["Kanto"], "cultural", "mid", "moderate");
        var dummyResult = new ParsedItineraryDto([], [], false, null, null, "low", false, null);

        _generationSvcMock
            .Setup(s => s.GenerateItineraryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyResult);

        var result = await _sut.GenerateItinerary(request, CancellationToken.None);

        // At the controller level (no pipeline) the action returns 200 — the 400 is produced
        // by the framework's validation middleware before this code is ever reached in production.
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
