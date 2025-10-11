using Aurora.Application.DTOs;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Servicio de validaci�n de eventos usando IA
/// </summary>
public interface IAIValidationService
{
    /// <summary>
    /// Valida si un evento debería ser creado en la fecha especificada
    /// </summary>
    /// <param name="eventDto">Datos del evento a validar</param>
    /// <param name="userId">ID del usuario que crea el evento</param>
    /// <param name="existingEvents">Eventos existentes del usuario para contexto (opcional)</param>
    /// <returns>Resultado de la validación con recomendaciones</returns>
    Task<AIValidationResult> ValidateEventCreationAsync(
        CreateEventDto eventDto, 
        Guid userId, 
        IEnumerable<EventDto>? existingEvents = null);

    /// <summary>
    /// Parsea texto en lenguaje natural a un evento estructurado y devuelve el análisis asociado usando IA
    /// </summary>
    /// <param name="naturalLanguageText">Texto en lenguaje natural (ej: "reunión mañana a las 3pm por 2 horas")</param>
    /// <param name="userId">ID del usuario que crea el evento</param>
    /// <param name="availableCategories">Categorías disponibles con sus IDs reales</param>
    /// <param name="timezoneOffsetMinutes">Offset de zona horaria del usuario en minutos desde UTC (ej: -180 para UTC-3)</param>
    /// <param name="existingEvents">Eventos existentes del usuario para contexto (opcional)</param>
    /// <returns>Evento parseado y análisis de validación</returns>
    Task<ParseNaturalLanguageResponseDto> ParseNaturalLanguageAsync(
        string naturalLanguageText,
        Guid userId,
        IEnumerable<EventCategoryDto> availableCategories,
        int timezoneOffsetMinutes = 0,
        IEnumerable<EventDto>? existingEvents = null);
}
