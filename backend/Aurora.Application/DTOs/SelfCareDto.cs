namespace Aurora.Application.DTOs;

/// <summary>
/// Request para obtener sugerencias de autocuidado
/// </summary>
public class SelfCareRequestDto
{
    /// <summary>
    /// Contexto adicional (opcional)
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Mood actual del usuario (1-5, opcional)
    /// </summary>
    public int? CurrentMood { get; set; }

    /// <summary>
    /// Cantidad de sugerencias solicitadas (por defecto 5)
    /// </summary>
    public int Count { get; set; } = 5;
}

/// <summary>
/// Sugerencia de autocuidado personalizada
/// </summary>
public class SelfCareRecommendationDto
{
    /// <summary>
    /// ID único de la sugerencia
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de actividad de autocuidado
    /// </summary>
    public SelfCareType Type { get; set; }

    /// <summary>
    /// Descripción del tipo
    /// </summary>
    public string TypeDescription { get; set; } = string.Empty;

    /// <summary>
    /// Título de la sugerencia
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Duración estimada en minutos
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Razón personalizada basada en datos del usuario
    /// </summary>
    public string PersonalizedReason { get; set; } = string.Empty;

    /// <summary>
    /// Score de confianza (0-100)
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// Icono recomendado para la UI
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Impacto histórico en el mood (0-100, null si no hay datos)
    /// </summary>
    public int? HistoricalMoodImpact { get; set; }

    /// <summary>
    /// Tasa de completitud histórica (0-100, null si no hay datos)
    /// </summary>
    public int? CompletionRate { get; set; }

    /// <summary>
    /// Fecha/hora sugerida para agendar
    /// </summary>
    public DateTime? SuggestedDateTime { get; set; }

    /// <summary>
    /// ID de categoría si se agenda como evento
    /// </summary>
    public Guid? CategoryId { get; set; }
}

/// <summary>
/// Tipos de actividades de autocuidado
/// </summary>
public enum SelfCareType
{
    /// <summary>
    /// Actividad física (caminar, ejercicio, estiramientos)
    /// </summary>
    Physical = 1,

    /// <summary>
    /// Actividad mental (meditación, respiración, descanso visual)
    /// </summary>
    Mental = 2,

    /// <summary>
    /// Actividad social (llamar amigo, mensaje familiar, café virtual)
    /// </summary>
    Social = 3,

    /// <summary>
    /// Actividad creativa (journal, dibujar, música)
    /// </summary>
    Creative = 4,

    /// <summary>
    /// Descanso (siesta, desconectar, té/infusión)
    /// </summary>
    Rest = 5
}

/// <summary>
/// Feedback de una sugerencia de autocuidado
/// </summary>
public class SelfCareFeedbackDto
{
    /// <summary>
    /// ID de la sugerencia
    /// </summary>
    public string RecommendationId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de acción realizada
    /// </summary>
    public SelfCareFeedbackAction Action { get; set; }

    /// <summary>
    /// Mood después de la actividad (1-5, opcional)
    /// </summary>
    public int? MoodAfter { get; set; }

    /// <summary>
    /// Notas adicionales (opcional)
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Timestamp del feedback
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Tipos de acciones de feedback
/// </summary>
public enum SelfCareFeedbackAction
{
    /// <summary>
    /// Usuario agendó la actividad
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Usuario hizo la actividad inmediatamente
    /// </summary>
    CompletedNow = 2,

    /// <summary>
    /// Usuario rechazó la sugerencia
    /// </summary>
    Dismissed = 3,

    /// <summary>
    /// Usuario ignoró la sugerencia
    /// </summary>
    Ignored = 4
}
