using FluentValidation;
using WhereToStayInJapan.Application.DTOs;

namespace WhereToStayInJapan.Application.Validation;

public class UserPreferencesValidator : AbstractValidator<UserPreferencesDto>
{
    private static readonly HashSet<string> ValidBudgetTiers = ["budget", "mid", "luxury"];

    public UserPreferencesValidator()
    {
        RuleFor(x => x.Travelers)
            .InclusiveBetween(1, 20).WithMessage("Travelers must be between 1 and 20.");

        RuleFor(x => x.BudgetTier)
            .Must(t => ValidBudgetTiers.Contains(t.ToLowerInvariant()))
            .WithMessage("Budget tier must be 'budget', 'mid', or 'luxury'.");

        When(x => x.CheckIn.HasValue && x.CheckOut.HasValue, () =>
            RuleFor(x => x.CheckOut!.Value)
                .GreaterThan(x => x.CheckIn!.Value)
                .WithMessage("Check-out date must be after check-in date."));
    }
}
