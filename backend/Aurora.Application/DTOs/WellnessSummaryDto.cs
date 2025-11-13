using System;
using System.Collections.Generic;

namespace Aurora.Application.DTOs;

/// <summary>
/// Resumen integral del estado de ánimo mensual para la vista de bienestar.
/// </summary>
public class WellnessSummaryDto
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
    /// Promedio general del mes. Vale 0 si no hubo registros.
    /// </summary>
    public double AverageMood { get; set; }

    /// <summary>
    /// Día con mejor calificación en el período.
    /// </summary>
    public MoodDaySnapshotDto? BestDay { get; set; }

    /// <summary>
    /// Día con peor calificación en el período.
    /// </summary>
    public MoodDaySnapshotDto? WorstDay { get; set; }

    /// <summary>
    /// Tendencia diaria de ánimo.
    /// </summary>
    public IReadOnlyCollection<MoodTrendPointDto> MoodTrend { get; set; } = Array.Empty<MoodTrendPointDto>();

    /// <summary>
    /// Distribución por calificación.
    /// </summary>
    public IReadOnlyCollection<MoodDistributionSliceDto> MoodDistribution { get; set; } = Array.Empty<MoodDistributionSliceDto>();

    /// <summary>
    /// Rachas positivas/negativas dentro del período.
    /// </summary>
    public MoodStreaksDto Streaks { get; set; } = new();

    /// <summary>
    /// Impacto por categoría de eventos completados.
    /// </summary>
    public IReadOnlyCollection<CategoryMoodImpactDto> CategoryImpacts { get; set; } = Array.Empty<CategoryMoodImpactDto>();

    /// <summary>
    /// Cantidad total de días con registro.
    /// </summary>
    public int TotalTrackedDays { get; set; }

    /// <summary>
    /// Días catalogados como positivos (calificación >= 4).
    /// </summary>
    public int PositiveDays { get; set; }

    /// <summary>
    /// Días catalogados como neutros (calificación == 3).
    /// </summary>
    public int NeutralDays { get; set; }

    /// <summary>
    /// Días catalogados como negativos (calificación <= 2).
    /// </summary>
    public int NegativeDays { get; set; }

    /// <summary>
    /// Porcentaje de cobertura de registro vs. días del mes (0-1).
    /// </summary>
    public double TrackingCoverage { get; set; }

    /// <summary>
    /// Indica si se encontró al menos un evento con estado de ánimo registrado.
    /// </summary>
    public bool HasEventMoodData { get; set; }
}
