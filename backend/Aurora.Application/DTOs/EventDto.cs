namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para transferencia de datos de eventos
/// </summary>
public class EventDto
{
    /// <summary>
    /// Identificador único del evento
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Título del evento
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción del evento (opcional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Fecha y hora de inicio del evento (ISO 8601)
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Fecha y hora de finalización del evento (ISO 8601)
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Indica si el evento dura todo el día
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>
    /// Ubicación del evento (opcional)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Notas adicionales del evento
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Color personalizado del evento (hex color)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Indica si el evento es recurrente
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Patrón de recurrencia (si aplica)
    /// </summary>
    public string? RecurrencePattern { get; set; }

    /// <summary>
    /// ID de la categoría del evento
    /// </summary>
    public Guid EventCategoryId { get; set; }

    /// <summary>
    /// Información de la categoría del evento
    /// </summary>
    public EventCategoryDto? EventCategory { get; set; }

    /// <summary>
    /// ID del usuario propietario del evento
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Fecha de creación del evento
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}