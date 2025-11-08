namespace Aurora.Domain.Enums;

/// <summary>
/// Estados de una sugerencia
/// </summary>
public enum SuggestionStatus
{
    /// <summary>
    /// Pendiente de revisión
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Aceptada por el usuario
    /// </summary>
    Accepted = 2,

    /// <summary>
    /// Rechazada por el usuario
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Pospuesta para más tarde
    /// </summary>
    Postponed = 4,

    /// <summary>
    /// Expirada (ya no relevante)
    /// </summary>
    Expired = 5
}
