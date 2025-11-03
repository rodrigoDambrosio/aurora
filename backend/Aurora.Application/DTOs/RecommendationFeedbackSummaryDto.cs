using System;

namespace Aurora.Application.DTOs;

/// <summary>
/// Resumen de métricas sobre el feedback brindado a las recomendaciones personalizadas.
/// </summary>
public class RecommendationFeedbackSummaryDto
{
    /// <summary>
    /// Cantidad total de interacciones registradas en el período.
    /// </summary>
    public int TotalFeedback { get; set; }

    /// <summary>
    /// Cantidad de recomendaciones marcadas como útiles.
    /// </summary>
    public int AcceptedCount { get; set; }

    /// <summary>
    /// Cantidad de recomendaciones descartadas.
    /// </summary>
    public int RejectedCount { get; set; }

    /// <summary>
    /// Porcentaje de aceptación (0-100).
    /// </summary>
    public double AcceptanceRate { get; set; }

    /// <summary>
    /// Promedio de ánimo reportado luego de seguir la recomendación (1-5) o null si no hay datos.
    /// </summary>
    public double? AverageMoodAfter { get; set; }

    /// <summary>
    /// Fecha de inicio del período analizado (UTC).
    /// </summary>
    public DateTime PeriodStartUtc { get; set; }

    /// <summary>
    /// Fecha de finalización del período analizado (UTC).
    /// </summary>
    public DateTime PeriodEndUtc { get; set; }
}
