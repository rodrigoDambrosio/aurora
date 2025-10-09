using Aurora.Application.DTOs;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Servicio de validación de eventos usando IA
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
}
