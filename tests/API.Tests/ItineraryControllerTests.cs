using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;
using WhereToStayInJapan.Application.Validation;
using WhereToStayInJapan.API.Controllers;

namespace WhereToStayInJapan.API.Tests;

public class ItineraryControllerTests
{
    private readonly Mock<IItineraryParsingService> _parsingSvcMock = new();
    private readonly Mock<IItineraryGenerationService> _generationSvcMock = new();
    private readonly ItineraryGenerationRequestValidator _validator = new();
    private readonly ItineraryController _sut;

    public ItineraryControllerTests()
    {
        _sut = new ItineraryController(_parsingSvcMock.Object, _generationSvcMock.Object, _validator);
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

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<ParsedItineraryDto>().Subject;
        dto.Destinations.Should().HaveCount(1);
        dto.Destinations[0].Name.Should().Be("Shinjuku");
    }

    [Fact]
    public async Task GenerateItinerary_InvalidDurationDays_Returns400()
    {
        var request = new ItineraryGenerationRequestDto("standard", 0, ["Kanto"], "cultural", "mid", "moderate");

        var result = await _sut.GenerateItinerary(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
