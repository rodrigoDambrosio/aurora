using Aurora.Domain.Enums;

namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para crear o actualizar eventos
/// </summary>
public class CreateEventDto
{
    /// <summary>
    /// Título del evento
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción del evento (opcional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Fecha y hora de inicio del evento
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Fecha y hora de finalización del evento
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Indica si el evento dura todo el día
    /// </summary>
    public bool IsAllDay { get; set; } = false;

    /// <summary>
    /// Ubicación del evento (opcional)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Notas adicionales del evento
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Prioridad del evento
    /// </summary>
    public EventPriority Priority { get; set; } = EventPriority.Medium;

    /// <summary>
    /// Color personalizado del evento (opcional)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Indica si el evento es recurrente
    /// </summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Patrón de recurrencia (si aplica)
    /// </summary>
    public string? RecurrencePattern { get; set; }

    /// <summary>
    /// ID de la categoría del evento
    /// </summary>
    public Guid EventCategoryId { get; set; }

    /// <summary>
    /// Nombre de categoría sugerida por IA (opcional, se crea si no existe)
    /// </summary>
    public string? SuggestedCategoryName { get; set; }

    /// <summary>
    /// Desplazamiento de la zona horaria en minutos (opcional)
    /// </summary>
    public int? TimezoneOffsetMinutes { get; set; }
}