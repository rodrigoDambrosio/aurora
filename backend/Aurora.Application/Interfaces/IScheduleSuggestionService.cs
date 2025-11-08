using Aurora.Application.DTOs;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Interfaz del servicio de sugerencias de reorganización
/// </summary>
public interface IScheduleSuggestionService
{
    /// <summary>
    /// Genera sugerencias de reorganización para un usuario
    /// </summary>
    Task<IEnumerable<ScheduleSuggestionDto>> GenerateSuggestionsAsync(Guid userId);

    /// <summary>
    /// Obtiene las sugerencias pendientes de un usuario
    /// </summary>
    Task<IEnumerable<ScheduleSuggestionDto>> GetPendingSuggestionsAsync(Guid userId);

    /// <summary>
    /// Responde a una sugerencia (aceptar/rechazar/posponer)
    /// </summary>
    Task<ScheduleSuggestionDto> RespondToSuggestionAsync(Guid suggestionId, RespondToSuggestionDto response, Guid userId);
}
