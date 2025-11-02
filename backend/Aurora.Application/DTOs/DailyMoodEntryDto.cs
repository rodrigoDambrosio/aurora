namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para exponer registros de estado de ánimo diarios.
/// </summary>
public class DailyMoodEntryDto
{
    /// <summary>
    /// Identificador único del registro.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Fecha del registro (ISO 8601, truncada a día).
    /// </summary>
    public DateTime EntryDate { get; set; }

    /// <summary>
    /// Calificación del estado de ánimo (1-5).
    /// </summary>
    public int MoodRating { get; set; }

    /// <summary>
    /// Nota opcional asociada al registro.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Identificador del usuario propietario del registro.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Fecha de creación del registro.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha de última actualización del registro.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
