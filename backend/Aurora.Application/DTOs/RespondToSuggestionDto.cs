using Aurora.Domain.Enums;

namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para responder a una sugerencia
/// </summary>
public class RespondToSuggestionDto
{
    /// <summary>
    /// Nueva acción sobre la sugerencia
    /// </summary>
    public SuggestionStatus Status { get; set; }

    /// <summary>
    /// Comentario opcional del usuario
    /// </summary>
    public string? UserComment { get; set; }
}
