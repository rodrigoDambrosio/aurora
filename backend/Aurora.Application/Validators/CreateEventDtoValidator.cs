using Aurora.Application.DTOs;
using FluentValidation;

namespace Aurora.Application.Validators;

/// <summary>
/// Validador para CreateEventDto
/// </summary>
public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    public CreateEventDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("El título del evento es obligatorio")
            .MaximumLength(200)
            .WithMessage("El título no puede tener más de 200 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("La descripción no puede tener más de 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("La fecha de inicio es obligatoria");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("La fecha de fin es obligatoria")
            .GreaterThan(x => x.StartDate)
            .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio");

        RuleFor(x => x)
            .Must(ValidateDateRange)
            .WithMessage("La diferencia entre fecha de inicio y fin no puede ser mayor a 7 días")
            .When(x => x.StartDate != default && x.EndDate != default);

        RuleFor(x => x.Location)
            .MaximumLength(500)
            .WithMessage("La ubicación no puede tener más de 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Las notas no pueden tener más de 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Color)
            .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
            .WithMessage("El color debe estar en formato hexadecimal (#RRGGBB o #RGB)")
            .When(x => !string.IsNullOrEmpty(x.Color));

        RuleFor(x => x.EventCategoryId)
            .NotEmpty()
            .WithMessage("La categoría del evento es obligatoria");

        RuleFor(x => x.RecurrencePattern)
            .MaximumLength(100)
            .WithMessage("El patrón de recurrencia no puede tener más de 100 caracteres")
            .When(x => x.IsRecurring && !string.IsNullOrEmpty(x.RecurrencePattern));

        RuleFor(x => x.RecurrencePattern)
            .NotEmpty()
            .WithMessage("El patrón de recurrencia es obligatorio cuando el evento es recurrente")
            .When(x => x.IsRecurring);
    }

    private bool ValidateDateRange(CreateEventDto dto)
    {
        if (dto.StartDate == default || dto.EndDate == default)
            return true; // Other rules will handle empty dates

        var duration = dto.EndDate - dto.StartDate;
        return duration.TotalDays <= 7;
    }
}