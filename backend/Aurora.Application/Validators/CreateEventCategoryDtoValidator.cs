using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using FluentValidation;

namespace Aurora.Application.Validators;

/// <summary>
/// Validador para la creación de categorías de eventos
/// </summary>
public class CreateEventCategoryDtoValidator : AbstractValidator<CreateEventCategoryDto>
{
    private readonly IEventCategoryRepository _categoryRepository;

    public CreateEventCategoryDtoValidator(IEventCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre de la categoría es obligatorio")
            .MaximumLength(50)
            .WithMessage("El nombre no puede superar los 50 caracteres")
            .Must(BeValidName)
            .WithMessage("El nombre no puede contener solo espacios");

        RuleFor(x => x.Description)
            .MaximumLength(200)
            .WithMessage("La descripción no puede superar los 200 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Color)
            .NotEmpty()
            .WithMessage("El color es obligatorio")
            .Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
            .WithMessage("El color debe estar en formato hexadecimal válido (ej: #3b82f6)");

        RuleFor(x => x.Icon)
            .MaximumLength(100)
            .WithMessage("El ícono no puede superar los 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Icon));
    }

    private bool BeValidName(string name)
    {
        return !string.IsNullOrWhiteSpace(name);
    }
}
