using Aurora.Application.DTOs;
using FluentValidation;

namespace Aurora.Application.Validators;

/// <summary>
/// Valida los datos para crear o actualizar un registro de estado de ánimo diario.
/// </summary>
public class UpsertDailyMoodDtoValidator : AbstractValidator<UpsertDailyMoodDto>
{
    public UpsertDailyMoodDtoValidator()
    {
        RuleFor(x => x.EntryDate)
            .NotEmpty()
            .WithMessage("La fecha es obligatoria.");

        RuleFor(x => x.MoodRating)
            .InclusiveBetween(1, 5)
            .WithMessage("La calificación debe estar entre 1 y 5.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("La nota no puede superar los 500 caracteres.");
    }
}
