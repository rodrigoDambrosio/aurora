namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para respuesta de eventos semanales
/// </summary>
public class WeeklyEventsResponseDto
{
    /// <summary>
    /// Lista de eventos de la semana
    /// </summary>
    public List<EventDto> Events { get; set; } = new();

    /// <summary>
    /// Fecha de inicio de la semana
    /// </summary>
    public DateTime WeekStart { get; set; }

    /// <summary>
    /// Fecha de fin de la semana
    /// </summary>
    public DateTime WeekEnd { get; set; }

    /// <summary>
    /// Categorías disponibles para el usuario
    /// </summary>
    public List<EventCategoryDto> Categories { get; set; } = new();

    /// <summary>
    /// Total de eventos encontrados
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Indica si hay más eventos fuera del rango de fechas
    /// </summary>
    public bool HasMoreEvents { get; set; } = false;
}