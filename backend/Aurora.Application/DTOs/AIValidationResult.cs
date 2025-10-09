namespace Aurora.Application.DTOs;

/// <summary>
/// Resultado de la validación de IA para la creación de eventos
/// </summary>
public class AIValidationResult
{
    /// <summary>
    /// Indica si el evento está aprobado para ser creado
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Mensaje de recomendación de la IA (solo si no está aprobado)
    /// </summary>
    public string? RecommendationMessage { get; set; }

    /// <summary>
    /// Severidad de la recomendación
    /// </summary>
    public AIValidationSeverity Severity { get; set; }

    /// <summary>
    /// Sugerencias adicionales de la IA
    /// </summary>
    public List<string>? Suggestions { get; set; }
}

/// <summary>
/// Severidad de la validación de IA
/// </summary>
public enum AIValidationSeverity
{
    /// <summary>
    /// Información general
    /// </summary>
    Info = 0,

    /// <summary>
    /// Advertencia - el evento puede crearse pero con precaución
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Crítico - se recomienda fuertemente no crear el evento
    /// </summary>
    Critical = 2
}
