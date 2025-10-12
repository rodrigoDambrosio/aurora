using Aurora.Application.DTOs.User;
using FluentValidation;

namespace Aurora.Application.Validators;

/// <summary>
/// Validador para la actualización de perfil de usuario
/// </summary>
public class UpdateUserProfileDtoValidator : AbstractValidator<UpdateUserProfileDto>
{
    public UpdateUserProfileDtoValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
        {
            RuleFor(x => x.Name)
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
                .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El email debe tener un formato válido")
                .MaximumLength(255).WithMessage("El email no puede exceder los 255 caracteres");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Timezone), () =>
        {
            RuleFor(x => x.Timezone)
                .MaximumLength(50).WithMessage("La zona horaria no puede exceder los 50 caracteres");
        });
    }
}
