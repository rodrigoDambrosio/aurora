using System;

namespace Aurora.Application.DTOs;

/// <summary>
/// Payload para registrar la reacción del usuario ante una recomendación.
/// </summary>
public class RecommendationFeedbackDto
{
    /// <summary>
    /// Identificador de la recomendación evaluada.
    /// </summary>
    public string RecommendationId { get; set; } = string.Empty;

    /// <summary>
    /// Indica si la sugerencia resultó útil.
    /// </summary>
    public bool Accepted { get; set; }

    /// <summary>
    /// Comentario opcional del usuario.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Estado de ánimo declarado después de aplicar la sugerencia (1-5).
    /// </summary>
    public int? MoodAfter { get; set; }

    /// <summary>
    /// Fecha en la que se envió el feedback.
    /// </summary>
    public DateTime? SubmittedAtUtc { get; set; }
}
