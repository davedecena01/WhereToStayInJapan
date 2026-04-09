using FluentValidation;

namespace WhereToStayInJapan.Application.Validation;

public record ParseItineraryRequest(string? Text, string? FileName);

public class ParseItineraryRequestValidator : AbstractValidator<ParseItineraryRequest>
{
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".docx", ".txt"];

    public ParseItineraryRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Text) || !string.IsNullOrWhiteSpace(x.FileName))
            .WithMessage("Either text or a file must be provided.");

        When(x => !string.IsNullOrWhiteSpace(x.Text), () =>
            RuleFor(x => x.Text!)
                .MinimumLength(10).WithMessage("Itinerary text is too short.")
                .MaximumLength(50000).WithMessage("Itinerary text exceeds the 50,000 character limit."));

        When(x => !string.IsNullOrWhiteSpace(x.FileName), () =>
            RuleFor(x => x.FileName!)
                .Must(name => AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
                .WithMessage("Only PDF, DOCX, and TXT files are supported."));
    }
}
