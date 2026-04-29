using Moq;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Application.Services;
using WhereToStayInJapan.Domain.Models;
using FluentAssertions;

namespace WhereToStayInJapan.Application.Tests;

public class ItineraryGenerationServiceTests
{
    private readonly Mock<IAIProvider> _aiMock = new();
    private readonly ItineraryGenerationService _sut;

    public ItineraryGenerationServiceTests()
    {
        _sut = new ItineraryGenerationService(_aiMock.Object);
    }

    [Fact]
    public async Task standard_mode_maps_destination_and_confidence()
    {
        var request = new ItineraryGenerationRequestDto("standard", 5, ["Kanto", "Kansai"], "cultural", "mid", "moderate");
        var parsed = new ParsedItinerary
        {
            ParsingConfidence = "high",
            ClarificationNeeded = false,
            Destinations =
            [
                new Destination { Name = "Shinjuku", City = "Tokyo", Region = "Kanto", DayNumber = 1 },
                new Destination { Name = "Gion", City = "Kyoto", Region = "Kansai", DayNumber = 2 }
            ],
            RegionsDetected = ["Kanto", "Kansai"],
            IsMultiRegion = true
        };
        _aiMock.Setup(a => a.GenerateItineraryAsync(request, default)).ReturnsAsync(parsed);

        var result = await _sut.GenerateItineraryAsync(request);

        result.Destinations.Should().HaveCount(2);
        result.Destinations[0].Name.Should().Be("Shinjuku");
        result.ParsingConfidence.Should().Be("high");
        result.ClarificationNeeded.Should().BeFalse();
        result.IsMultiRegion.Should().BeTrue();
    }

    [Fact]
    public async Task challenge_mode_delegates_to_ai_provider()
    {
        var request = new ItineraryGenerationRequestDto("challenge", 7, ["Tohoku"], null, null, null);
        var parsed = new ParsedItinerary
        {
            ParsingConfidence = "high",
            ClarificationNeeded = false,
            Destinations = [new Destination { Name = "Tono", City = "Tono", Region = "Tohoku", DayNumber = 1 }],
            RegionsDetected = ["Tohoku"],
            IsMultiRegion = false
        };
        _aiMock.Setup(a => a.GenerateItineraryAsync(request, default)).ReturnsAsync(parsed);

        var result = await _sut.GenerateItineraryAsync(request);

        _aiMock.Verify(a => a.GenerateItineraryAsync(request, default), Times.Once);
        result.Destinations.Should().HaveCount(1);
    }

    [Fact]
    public async Task raw_text_is_null_on_generated_itinerary()
    {
        var request = new ItineraryGenerationRequestDto("standard", 3, ["Kanto"], "foodie", "budget", "relaxed");
        var parsed = new ParsedItinerary
        {
            RawText = "some text that should be stripped",
            ParsingConfidence = "high",
            ClarificationNeeded = false,
            Destinations = [],
            RegionsDetected = []
        };
        _aiMock.Setup(a => a.GenerateItineraryAsync(request, default)).ReturnsAsync(parsed);

        var result = await _sut.GenerateItineraryAsync(request);

        result.RawText.Should().BeNull();
    }
}
