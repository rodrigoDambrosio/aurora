using Aurora.Application.DTOs;
using FluentValidation;

namespace Aurora.Application.Validators;

/// <summary>
/// Validador para WeeklyEventsRequestDto
/// </summary>
public class WeeklyEventsRequestDtoValidator : AbstractValidator<WeeklyEventsRequestDto>
{
    public WeeklyEventsRequestDtoValidator()
    {
        RuleFor(x => x.WeekStart)
            .Must(BeValidWeekStart)
            .WithMessage("La fecha de inicio debe corresponder al primer día de una semana")
            .When(x => x.WeekStart != default);

        RuleFor(x => x.WeekStart)
            .Must(BeWithinReasonableRange)
            .WithMessage("La fecha debe estar dentro de un rango razonable (no más de 5 años en el futuro o pasado)")
            .When(x => x.WeekStart != default);
    }

    private bool BeValidWeekStart(DateTime weekStart)
    {
        // En el contexto de Aurora, consideramos que la semana inicia el domingo (DayOfWeek.Sunday = 0)
        // Pero puede ser configurado por el usuario en sus preferencias
        // Por ahora, aceptamos cualquier día como inicio de semana válido
        return true;
    }

    private bool BeWithinReasonableRange(DateTime weekStart)
    {
        var now = DateTime.UtcNow;
        var fiveYearsAgo = now.AddYears(-5);
        var fiveYearsFromNow = now.AddYears(5);

        return weekStart >= fiveYearsAgo && weekStart <= fiveYearsFromNow;
    }
}