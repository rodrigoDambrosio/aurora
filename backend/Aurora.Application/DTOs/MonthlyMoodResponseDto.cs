namespace Aurora.Application.DTOs;

/// <summary>
/// Respuesta con el listado de estados de ánimo diarios de un mes.
/// </summary>
public class MonthlyMoodResponseDto
{
    /// <summary>
    /// Año solicitado.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Mes solicitado (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Lista de registros encontrados para el mes.
    /// </summary>
    public IReadOnlyCollection<DailyMoodEntryDto> Entries { get; set; } = Array.Empty<DailyMoodEntryDto>();
}
