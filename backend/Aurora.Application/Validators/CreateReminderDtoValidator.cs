using Aurora.Application.DTOs;
using Aurora.Domain.Enums;
using FluentValidation;

namespace Aurora.Application.Validators;

public class CreateReminderDtoValidator : AbstractValidator<CreateReminderDto>
{
    public CreateReminderDtoValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("El ID del evento es requerido");

        RuleFor(x => x.ReminderType)
            .IsInEnum()
            .WithMessage("El tipo de recordatorio no es válido");

        When(x => x.ReminderType == ReminderType.OneDayBefore, () =>
        {
            RuleFor(x => x.CustomTimeHours)
                .NotNull()
                .WithMessage("La hora personalizada es requerida para recordatorios de un día antes")
                .InclusiveBetween(0, 23)
                .WithMessage("La hora debe estar entre 0 y 23");

            RuleFor(x => x.CustomTimeMinutes)
                .NotNull()
                .WithMessage("Los minutos personalizados son requeridos para recordatorios de un día antes")
                .InclusiveBetween(0, 59)
                .WithMessage("Los minutos deben estar entre 0 y 59");
        });
    }
}
