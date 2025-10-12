namespace Aurora.Application.DTOs;

/// <summary>
/// Resultado de la validaci�n de IA para la creaci�n de eventos
/// </summary>
public class AIValidationResult
{
    /// <summary>
    /// Indica si el evento est� aprobado para ser creado
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Mensaje de recomendaci�n de la IA (solo si no est� aprobado)
    /// </summary>
    public string? RecommendationMessage { get; set; }

    /// <summary>
    /// Severidad de la recomendaci�n
    /// </summary>
    public AIValidationSeverity Severity { get; set; }

    /// <summary>
    /// Sugerencias adicionales de la IA
    /// </summary>
    public List<string>? Suggestions { get; set; }

    /// <summary>
    /// Indica si el resultado fue generado por IA o por una validación básica de respaldo
    /// </summary>
    public bool UsedAi { get; set; } = true;
}

/// <summary>
/// Severidad de la validaci�n de IA
/// </summary>
public enum AIValidationSeverity
{
    /// <summary>
    /// Informaci�n general
    /// </summary>
    Info = 0,

    /// <summary>
    /// Advertencia - el evento puede crearse pero con precauci�n
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Cr�tico - se recomienda fuertemente no crear el evento
    /// </summary>
    Critical = 2
}
