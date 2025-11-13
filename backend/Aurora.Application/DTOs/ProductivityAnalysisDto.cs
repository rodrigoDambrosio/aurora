namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para el análisis completo de productividad del usuario
/// </summary>
public class ProductivityAnalysisDto
{
    /// <summary>
    /// Productividad por franja horaria (0-23 horas)
    /// </summary>
    public List<HourlyProductivityDto> HourlyProductivity { get; set; } = new();

    /// <summary>
    /// Productividad por día de la semana (0=Domingo, 6=Sábado)
    /// </summary>
    public List<DailyProductivityDto> DailyProductivity { get; set; } = new();

    /// <summary>
    /// Horas doradas identificadas (alta productividad)
    /// </summary>
    public List<GoldenHourDto> GoldenHours { get; set; } = new();

    /// <summary>
    /// Horas de baja energía identificadas
    /// </summary>
    public List<LowEnergyHourDto> LowEnergyHours { get; set; } = new();

    /// <summary>
    /// Productividad por tipo de actividad (categoría)
    /// </summary>
    public List<CategoryProductivityDto> CategoryProductivity { get; set; } = new();

    /// <summary>
    /// Recomendaciones generadas por el análisis
    /// </summary>
    public List<ProductivityRecommendationDto> Recommendations { get; set; } = new();

    /// <summary>
    /// Período analizado (desde)
    /// </summary>
    public DateTime AnalysisPeriodStart { get; set; }

    /// <summary>
    /// Período analizado (hasta)
    /// </summary>
    public DateTime AnalysisPeriodEnd { get; set; }

    /// <summary>
    /// Total de eventos analizados
    /// </summary>
    public int TotalEventsAnalyzed { get; set; }

    /// <summary>
    /// Total de registros de estado de ánimo analizados
    /// </summary>
    public int TotalMoodRecordsAnalyzed { get; set; }
}

/// <summary>
/// Productividad por hora del día
/// </summary>
public class HourlyProductivityDto
{
    /// <summary>
    /// Hora del día (0-23)
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// Promedio de estado de ánimo en esta hora (1-5)
    /// </summary>
    public double AverageMood { get; set; }

    /// <summary>
    /// Cantidad de eventos completados en esta hora
    /// </summary>
    public int EventsCompleted { get; set; }

    /// <summary>
    /// Cantidad total de eventos en esta hora
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Tasa de completitud (0-100%)
    /// </summary>
    public double CompletionRate { get; set; }

    /// <summary>
    /// Score de productividad (0-100)
    /// </summary>
    public double ProductivityScore { get; set; }
}

/// <summary>
/// Productividad por día de la semana
/// </summary>
public class DailyProductivityDto
{
    /// <summary>
    /// Día de la semana (0=Domingo, 6=Sábado)
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>
    /// Nombre del día
    /// </summary>
    public string DayName { get; set; } = string.Empty;

    /// <summary>
    /// Promedio de estado de ánimo en este día
    /// </summary>
    public double AverageMood { get; set; }

    /// <summary>
    /// Score de productividad promedio
    /// </summary>
    public double ProductivityScore { get; set; }

    /// <summary>
    /// Total de eventos en este día
    /// </summary>
    public int TotalEvents { get; set; }
}

/// <summary>
/// Hora dorada identificada (alta productividad)
/// </summary>
public class GoldenHourDto
{
    /// <summary>
    /// Hora de inicio (0-23)
    /// </summary>
    public int StartHour { get; set; }

    /// <summary>
    /// Hora de fin (0-23)
    /// </summary>
    public int EndHour { get; set; }

    /// <summary>
    /// Score de productividad promedio en este rango
    /// </summary>
    public double AverageProductivityScore { get; set; }

    /// <summary>
    /// Descripción del período
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Días de la semana donde aplica (null = todos)
    /// </summary>
    public List<int>? ApplicableDays { get; set; }
}

/// <summary>
/// Hora de baja energía identificada
/// </summary>
public class LowEnergyHourDto
{
    /// <summary>
    /// Hora de inicio (0-23)
    /// </summary>
    public int StartHour { get; set; }

    /// <summary>
    /// Hora de fin (0-23)
    /// </summary>
    public int EndHour { get; set; }

    /// <summary>
    /// Score de productividad promedio en este rango
    /// </summary>
    public double AverageProductivityScore { get; set; }

    /// <summary>
    /// Descripción del período
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Productividad por categoría de actividad
/// </summary>
public class CategoryProductivityDto
{
    /// <summary>
    /// ID de la categoría
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Nombre de la categoría
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Color de la categoría
    /// </summary>
    public string CategoryColor { get; set; } = string.Empty;

    /// <summary>
    /// Horas óptimas para esta categoría
    /// </summary>
    public List<int> OptimalHours { get; set; } = new();

    /// <summary>
    /// Score de productividad promedio
    /// </summary>
    public double AverageProductivityScore { get; set; }

    /// <summary>
    /// Mejor día de la semana para esta categoría
    /// </summary>
    public int BestDayOfWeek { get; set; }
}

/// <summary>
/// Recomendación de productividad
/// </summary>
public class ProductivityRecommendationDto
{
    /// <summary>
    /// Título de la recomendación
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Nivel de prioridad (1-5)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Tipo de recomendación
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Categorías afectadas
    /// </summary>
    public List<string> AffectedCategories { get; set; } = new();

    /// <summary>
    /// Horarios sugeridos
    /// </summary>
    public List<int> SuggestedHours { get; set; } = new();
}
