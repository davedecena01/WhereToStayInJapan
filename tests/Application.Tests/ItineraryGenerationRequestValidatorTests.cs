using FluentAssertions;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Validation;

namespace WhereToStayInJapan.Application.Tests;

public class ItineraryGenerationRequestValidatorTests
{
    private readonly ItineraryGenerationRequestValidator _validator = new();

    [Fact]
    public void Standard_valid_request_passes()
    {
        var req = new ItineraryGenerationRequestDto("standard", 5, ["Kanto"], "cultural", "mid", "moderate");
        _validator.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Challenge_valid_request_passes_without_style_pace_budget()
    {
        var req = new ItineraryGenerationRequestDto("challenge", 7, ["Kansai"], null, null, null);
        _validator.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_mode_fails_with_message()
    {
        var req = new ItineraryGenerationRequestDto("yolo", 5, ["Kanto"], "cultural", "mid", "moderate");
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Mode must be 'standard' or 'challenge'.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Duration_out_of_range_fails(int days)
    {
        var req = new ItineraryGenerationRequestDto("standard", days, ["Kanto"], "cultural", "mid", "moderate");
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Duration must be between 1 and 30 days.");
    }

    [Fact]
    public void Empty_regions_fails()
    {
        var req = new ItineraryGenerationRequestDto("standard", 5, [], "cultural", "mid", "moderate");
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Please select at least one region.");
    }

    [Fact]
    public void Standard_without_travel_style_fails()
    {
        var req = new ItineraryGenerationRequestDto("standard", 5, ["Kanto"], null, "mid", "moderate");
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Standard_without_budget_fails()
    {
        var req = new ItineraryGenerationRequestDto("standard", 5, ["Kanto"], "cultural", null, "moderate");
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Standard_without_pace_fails()
    {
        var req = new ItineraryGenerationRequestDto("standard", 5, ["Kanto"], "cultural", "mid", null);
        _validator.Validate(req).IsValid.Should().BeFalse();
    }
}
