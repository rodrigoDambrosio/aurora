using FluentValidation;

namespace Aurora.Application.Validators;

/// <summary>
/// Validador para UpdateEventMoodDto
/// </summary>
public class UpdateEventMoodDtoValidator : AbstractValidator<DTOs.UpdateEventMoodDto>
{
    public UpdateEventMoodDtoValidator()
    {
        RuleFor(x => x.MoodRating)
            .InclusiveBetween(1, 5)
            .When(x => x.MoodRating.HasValue)
            .WithMessage("La calificación del estado de ánimo debe estar entre 1 y 5.");

        RuleFor(x => x.MoodNotes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.MoodNotes))
            .WithMessage("Las notas no pueden exceder los 500 caracteres.");
    }
}
