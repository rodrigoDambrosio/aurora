using Aurora.Domain.Enums;

namespace Aurora.Domain.Entities;

/// <summary>
/// Entidad que representa un evento en el calendario
/// </summary>
public class Event : BaseEntity
{
    /// <summary>
    /// Título del evento
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada del evento (opcional)
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
    /// ID de la categoría del evento
    /// </summary>
    public Guid EventCategoryId { get; set; }

    /// <summary>
    /// Categoría del evento
    /// </summary>
    public virtual EventCategory EventCategory { get; set; } = null!;

    /// <summary>
    /// Indica si el evento dura todo el día
    /// </summary>
    public bool IsAllDay { get; set; } = false;

    /// <summary>
    /// Ubicación del evento (opcional)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Color personalizado para el evento (opcional, hex color)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Notas adicionales del evento
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Prioridad asignada al evento
    /// </summary>
    public EventPriority Priority { get; set; } = EventPriority.Medium;

    /// <summary>
    /// Indica si el evento se repite
    /// </summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Patrón de repetición (si es recurrente)
    /// </summary>
    public string? RecurrencePattern { get; set; }

    // Relaciones
    /// <summary>
    /// ID del usuario propietario del evento
    /// En modo desarrollo puede ser null para acceso anónimo
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Usuario propietario del evento
    /// </summary>
    public virtual User? User { get; set; }

    // Métodos de dominio
    /// <summary>
    /// Valida si las fechas del evento son consistentes
    /// </summary>
    public bool IsValidDateRange()
    {
        return StartDate < EndDate;
    }

    /// <summary>
    /// Calcula la duración del evento en minutos
    /// </summary>
    public int GetDurationInMinutes()
    {
        return (int)(EndDate - StartDate).TotalMinutes;
    }

    /// <summary>
    /// Verifica si el evento ocurre en una fecha específica
    /// </summary>
    public bool OccursOnDate(DateTime date)
    {
        var eventDate = StartDate.Date;
        var targetDate = date.Date;

        if (IsAllDay)
        {
            return eventDate == targetDate;
        }

        return eventDate == targetDate ||
               (StartDate.Date <= targetDate && EndDate.Date >= targetDate);
    }

    /// <summary>
    /// Verifica si el evento se superpone con otro evento
    /// </summary>
    public bool OverlapsWith(Event otherEvent)
    {
        return StartDate < otherEvent.EndDate && EndDate > otherEvent.StartDate;
    }

    /// <summary>
    /// Verifica si el evento pertenece a un usuario específico
    /// En modo desarrollo, considera el usuario demo
    /// </summary>
    /// <param name="userId">ID del usuario a verificar</param>
    /// <returns>True si el evento pertenece al usuario</returns>
    public bool BelongsToUser(Guid? userId)
    {
        // En modo desarrollo, si no hay usuario específico, usa el demo
        if (Constants.DomainConstants.Development.AllowAnonymousAccess)
        {
            var targetUserId = userId ?? Constants.DomainConstants.DemoUser.Id;
            return UserId == targetUserId;
        }

        return UserId == userId;
    }

    /// <summary>
    /// Establece el propietario del evento, usando usuario demo si no se especifica
    /// </summary>
    /// <param name="userId">ID del usuario, null para usar usuario demo</param>
    public void SetOwner(Guid? userId = null)
    {
        UserId = userId ?? Constants.DomainConstants.DemoUser.Id;
    }
}