using Aurora.Application.DTOs.User;
using FluentValidation;

namespace Aurora.Application.Validators;

/// <summary>
/// Validador para la actualización de preferencias de usuario
/// </summary>
public class UpdateUserPreferencesDtoValidator : AbstractValidator<UpdateUserPreferencesDto>
{
    public UpdateUserPreferencesDtoValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.Theme), () =>
        {
            RuleFor(x => x.Theme)
                .Must(t => t == "light" || t == "dark")
                .WithMessage("El tema debe ser 'light' o 'dark'");
        });

        When(x => !string.IsNullOrWhiteSpace(x.TimeFormat), () =>
        {
            RuleFor(x => x.TimeFormat)
                .Must(t => t == "12h" || t == "24h")
                .WithMessage("El formato de hora debe ser '12h' o '24h'");
        });

        When(x => x.FirstDayOfWeek.HasValue, () =>
        {
            RuleFor(x => x.FirstDayOfWeek)
                .InclusiveBetween(0, 6)
                .WithMessage("El primer día de la semana debe estar entre 0 (Domingo) y 6 (Sábado)");
        });

        When(x => x.DefaultReminderMinutes.HasValue, () =>
        {
            RuleFor(x => x.DefaultReminderMinutes)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Los minutos de recordatorio deben ser mayores o iguales a 0");
        });

        When(x => x.WorkDaysOfWeek != null, () =>
        {
            RuleFor(x => x.WorkDaysOfWeek)
                .Must(days => days == null || days.All(d => d >= 0 && d <= 6))
                .WithMessage("Los días laborales deben estar entre 0 (Domingo) y 6 (Sábado)");
        });

        When(x => x.ExerciseDaysOfWeek != null, () =>
        {
            RuleFor(x => x.ExerciseDaysOfWeek)
                .Must(days => days == null || days.All(d => d >= 0 && d <= 6))
                .WithMessage("Los días de ejercicio deben estar entre 0 (Domingo) y 6 (Sábado)");
        });
    }
}
