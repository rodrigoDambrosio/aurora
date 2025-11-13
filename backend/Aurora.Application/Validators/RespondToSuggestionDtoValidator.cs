using Aurora.Application.DTOs;
using Aurora.Domain.Enums;
using FluentValidation;

namespace Aurora.Application.Validators;

/// <summary>
/// Validador para RespondToSuggestionDto
/// </summary>
public class RespondToSuggestionDtoValidator : AbstractValidator<RespondToSuggestionDto>
{
    public RespondToSuggestionDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("El estado debe ser válido (Accepted, Rejected o Postponed)")
            .Must(status => status == SuggestionStatus.Accepted || 
                          status == SuggestionStatus.Rejected || 
                          status == SuggestionStatus.Postponed)
            .WithMessage("Solo se permite Aceptar, Rechazar o Posponer");

        RuleFor(x => x.UserComment)
            .MaximumLength(500)
            .WithMessage("El comentario no puede superar los 500 caracteres");
    }
}
