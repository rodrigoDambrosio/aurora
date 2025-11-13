namespace Aurora.Application.DTOs;

/// <summary>
/// DTO con el resultado de la generación de un plan multi-día
/// </summary>
public class GeneratePlanResponseDto
{
    /// <summary>
    /// Título descriptivo del plan generado
    /// </summary>
    public string PlanTitle { get; set; } = string.Empty;

    /// <summary>
    /// Descripción general del plan y su estructura
    /// </summary>
    public string PlanDescription { get; set; } = string.Empty;

    /// <summary>
    /// Duración total del plan en semanas
    /// </summary>
    public int DurationWeeks { get; set; }

    /// <summary>
    /// Número total de sesiones generadas
    /// </summary>
    public int TotalSessions { get; set; }

    /// <summary>
    /// Lista de eventos que conforman el plan
    /// </summary>
    public List<CreateEventDto> Events { get; set; } = new();

    /// <summary>
    /// Consejos o recomendaciones adicionales de la IA
    /// </summary>
    public string? AdditionalTips { get; set; }

    /// <summary>
    /// Indica si se detectaron posibles conflictos con eventos existentes
    /// </summary>
    public bool HasPotentialConflicts { get; set; }

    /// <summary>
    /// Lista de advertencias sobre posibles conflictos
    /// </summary>
    public List<string> ConflictWarnings { get; set; } = new();
}
