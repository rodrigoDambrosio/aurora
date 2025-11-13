using System;

namespace Aurora.Domain.Entities;

/// <summary>
/// Registro persistente del feedback brindado por un usuario sobre una recomendación sugerida.
/// </summary>
public class RecommendationFeedback : BaseEntity
{
    /// <summary>
    /// Identificador estable de la recomendación que originó el feedback.
    /// </summary>
    public string RecommendationId { get; set; } = string.Empty;

    /// <summary>
    /// Indica si la sugerencia resultó útil o no.
    /// </summary>
    public bool Accepted { get; set; }

    /// <summary>
    /// Comentario opcional brindado por el usuario.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Estado de ánimo declarado después de aplicar la recomendación (1-5).
    /// </summary>
    public int? MoodAfter { get; set; }

    /// <summary>
    /// Fecha en la que se registró el feedback (UTC).
    /// </summary>
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identificador del usuario asociado al feedback.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navegación hacia el usuario que brindó el feedback.
    /// </summary>
    public virtual User? User { get; set; }
}
