using Aurora.Domain.Enums;

namespace Aurora.Domain.Entities;

/// <summary>
/// Entidad que representa una sugerencia de reorganización del calendario
/// </summary>
public class ScheduleSuggestion : BaseEntity
{
    /// <summary>
    /// ID del usuario propietario
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// ID del evento que se sugiere mover (si aplica)
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// Evento relacionado (si aplica)
    /// </summary>
    public Event? Event { get; set; }

    /// <summary>
    /// Tipo de sugerencia
    /// </summary>
    public SuggestionType Type { get; set; }

    /// <summary>
    /// Descripción de la sugerencia
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Razón/justificación de la sugerencia
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Prioridad de la sugerencia (1-5, siendo 5 más urgente)
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Fecha/hora sugerida (para movimientos de eventos)
    /// </summary>
    public DateTime? SuggestedDateTime { get; set; }

    /// <summary>
    /// Estado de la sugerencia
    /// </summary>
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Pending;

    /// <summary>
    /// Fecha en que el usuario respondió a la sugerencia
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Puntuación de confianza del algoritmo (0-100)
    /// </summary>
    public int ConfidenceScore { get; set; } = 70;

    /// <summary>
    /// Datos adicionales en formato JSON
    /// </summary>
    public string? Metadata { get; set; }
}
