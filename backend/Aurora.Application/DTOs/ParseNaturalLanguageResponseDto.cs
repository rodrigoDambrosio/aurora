namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para respuesta de parsing de texto natural
/// </summary>
public class ParseNaturalLanguageResponseDto
{
    /// <summary>
    /// Evento parseado desde el texto natural
    /// </summary>
    public CreateEventDto Event { get; set; } = null!;

    /// <summary>
    /// Resultado de validación de IA (opcional)
    /// </summary>
    public AIValidationResult? Validation { get; set; }

    /// <summary>
    /// Indica si el evento fue parseado exitosamente
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje de error si el parsing falló
    /// </summary>
    public string? ErrorMessage { get; set; }
}
