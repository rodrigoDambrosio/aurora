using FluentValidation;
using Aurora.Application.DTOs;

namespace Aurora.Application.Validators;

/// <summary>
/// Validador para GeneratePlanRequestDto
/// </summary>
public class GeneratePlanRequestValidator : AbstractValidator<GeneratePlanRequestDto>
{
    public GeneratePlanRequestValidator()
    {
        RuleFor(x => x.Goal)
            .NotEmpty()
            .WithMessage("El objetivo del plan es requerido")
            .MinimumLength(10)
            .WithMessage("El objetivo debe tener al menos 10 caracteres")
            .MaximumLength(500)
            .WithMessage("El objetivo no puede exceder 500 caracteres");

        RuleFor(x => x.TimezoneOffsetMinutes)
            .InclusiveBetween(-720, 840)
            .WithMessage("El desplazamiento de zona horaria debe estar entre -720 y 840 minutos (UTC-12 a UTC+14)");

        RuleFor(x => x.DurationWeeks)
            .GreaterThan(0)
            .WithMessage("La duración del plan debe ser mayor a 0 semanas")
            .LessThanOrEqualTo(52)
            .WithMessage("La duración del plan no puede exceder 52 semanas")
            .When(x => x.DurationWeeks.HasValue);

        RuleFor(x => x.SessionsPerWeek)
            .GreaterThan(0)
            .WithMessage("Las sesiones por semana deben ser mayor a 0")
            .LessThanOrEqualTo(14)
            .WithMessage("Las sesiones por semana no pueden exceder 14 (2 por día)")
            .When(x => x.SessionsPerWeek.HasValue);

        RuleFor(x => x.SessionDurationMinutes)
            .GreaterThan(0)
            .WithMessage("La duración de la sesión debe ser mayor a 0 minutos")
            .LessThanOrEqualTo(480)
            .WithMessage("La duración de la sesión no puede exceder 8 horas (480 minutos)")
            .When(x => x.SessionDurationMinutes.HasValue);

        RuleFor(x => x.PreferredTimeOfDay)
            .Matches(@"^([01]\d|2[0-3]):([0-5]\d)$")
            .WithMessage("El horario preferido debe estar en formato HH:mm (00:00 - 23:59)")
            .When(x => !string.IsNullOrEmpty(x.PreferredTimeOfDay));
    }
}
