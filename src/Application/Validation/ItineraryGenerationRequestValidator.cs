using FluentValidation;
using WhereToStayInJapan.Application.DTOs;

namespace WhereToStayInJapan.Application.Validation;

public class ItineraryGenerationRequestValidator : AbstractValidator<ItineraryGenerationRequestDto>
{
    private static readonly HashSet<string> ValidModes = ["standard", "challenge"];
    private static readonly HashSet<string> ValidStyles = ["cultural", "foodie", "nature", "urban", "mix"];
    private static readonly HashSet<string> ValidBudgets = ["budget", "mid", "luxury"];
    private static readonly HashSet<string> ValidPaces = ["relaxed", "moderate", "packed"];

    public ItineraryGenerationRequestValidator()
    {
        RuleFor(x => x.Mode)
            .Must(m => ValidModes.Contains(m))
            .WithMessage("Mode must be 'standard' or 'challenge'.");

        RuleFor(x => x.DurationDays)
            .InclusiveBetween(1, 30)
            .WithMessage("Duration must be between 1 and 30 days.");

        RuleFor(x => x.Regions)
            .NotEmpty()
            .WithMessage("Please select at least one region.");

        When(x => x.Mode == "standard", () =>
        {
            RuleFor(x => x.TravelStyle)
                .NotEmpty()
                .Must(s => s == null || ValidStyles.Contains(s))
                .WithMessage("Travel style is required for standard mode.");

            RuleFor(x => x.BudgetTier)
                .NotEmpty()
                .Must(b => b == null || ValidBudgets.Contains(b))
                .WithMessage("Budget tier is required for standard mode.");

            RuleFor(x => x.Pace)
                .NotEmpty()
                .Must(p => p == null || ValidPaces.Contains(p))
                .WithMessage("Pace is required for standard mode.");
        });
    }
}
