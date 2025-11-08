namespace Aurora.Domain.Enums;

/// <summary>
/// Tipos de sugerencias de reorganización
/// </summary>
public enum SuggestionType
{
    /// <summary>
    /// Mover un evento a otro horario
    /// </summary>
    MoveEvent = 1,

    /// <summary>
    /// Resolver un conflicto de horarios
    /// </summary>
    ResolveConflict = 2,

    /// <summary>
    /// Optimizar distribución de tareas
    /// </summary>
    OptimizeDistribution = 3,

    /// <summary>
    /// Alerta sobre patrón problemático
    /// </summary>
    PatternAlert = 4,

    /// <summary>
    /// Sugerencia de descanso
    /// </summary>
    SuggestBreak = 5,

    /// <summary>
    /// Reorganización general
    /// </summary>
    GeneralReorganization = 6
}
