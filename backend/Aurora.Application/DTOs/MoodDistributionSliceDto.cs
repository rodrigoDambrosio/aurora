namespace Aurora.Application.DTOs;

/// <summary>
/// Describe la distribución de registros por calificación de ánimo.
/// </summary>
public class MoodDistributionSliceDto
{
    /// <summary>
    /// Calificación de ánimo (1-5).
    /// </summary>
    public int MoodRating { get; set; }

    /// <summary>
    /// Cantidad de registros que tienen la calificación indicada.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Porcentaje respecto al total de registros del período.
    /// </summary>
    public double Percentage { get; set; }
}
