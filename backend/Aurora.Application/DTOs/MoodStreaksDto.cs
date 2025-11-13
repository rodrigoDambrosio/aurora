namespace Aurora.Application.DTOs;

/// <summary>
/// Métricas de rachas positivas y negativas dentro de un período.
/// </summary>
public class MoodStreaksDto
{
    /// <summary>
    /// Racha positiva actual (calificaciones >= 4) al cierre del período.
    /// </summary>
    public int CurrentPositive { get; set; }

    /// <summary>
    /// Racha positiva más larga dentro del período analizado.
    /// </summary>
    public int LongestPositive { get; set; }

    /// <summary>
    /// Racha negativa actual (calificaciones <= 2) al cierre del período.
    /// </summary>
    public int CurrentNegative { get; set; }

    /// <summary>
    /// Racha negativa más larga dentro del período analizado.
    /// </summary>
    public int LongestNegative { get; set; }
}
