namespace Aurora.Application.DTOs;

/// <summary>
/// Datos necesarios para crear o actualizar un registro de estado de ánimo diario.
/// </summary>
public class UpsertDailyMoodDto
{
    /// <summary>
    /// Fecha del registro en horario local del usuario (se normaliza a UTC).
    /// </summary>
    public DateTime EntryDate { get; set; }

    /// <summary>
    /// Calificación del estado de ánimo (1-5).
    /// </summary>
    public int MoodRating { get; set; }

    /// <summary>
    /// Nota opcional asociada al estado de ánimo.
    /// </summary>
    public string? Notes { get; set; }
}
