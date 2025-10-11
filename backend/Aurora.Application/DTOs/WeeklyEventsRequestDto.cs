namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para solicitar eventos de una semana específica
/// </summary>
public class WeeklyEventsRequestDto
{
    /// <summary>
    /// Fecha de inicio de la semana
    /// </summary>
    public DateTime WeekStart { get; set; }

    /// <summary>
    /// Incluir categorías en la respuesta
    /// </summary>
    public bool IncludeCategories { get; set; } = true;

    /// <summary>
    /// Incluir eventos de todo el día
    /// </summary>
    public bool IncludeAllDayEvents { get; set; } = true;

    /// <summary>
    /// Incluir eventos recurrentes
    /// </summary>
    public bool IncludeRecurringEvents { get; set; } = true;
}