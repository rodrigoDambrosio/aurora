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
    /// <param name="userId">ID del usuario</param>
    /// <param name="timezoneOffsetMinutes">Offset de zona horaria en minutos (ej: -180 para GMT-3)</param>
    Task<IEnumerable<ScheduleSuggestionDto>> GenerateSuggestionsAsync(Guid userId, int timezoneOffsetMinutes);

    /// <summary>
    /// Obtiene las sugerencias pendientes de un usuario
    /// </summary>
    Task<IEnumerable<ScheduleSuggestionDto>> GetPendingSuggestionsAsync(Guid userId);

    /// <summary>
    /// Responde a una sugerencia (aceptar/rechazar/posponer)
    /// </summary>
    Task<ScheduleSuggestionDto> RespondToSuggestionAsync(Guid suggestionId, RespondToSuggestionDto response, Guid userId);
}
