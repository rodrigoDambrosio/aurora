using System;

namespace Aurora.Application.DTOs;

/// <summary>
/// Respuesta con una sugerencia personalizada basada en el historial del usuario.
/// </summary>
public class RecommendationDto
{
    /// <summary>
    /// Identificador estable para la recomendación sugerida.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Título corto que describe la sugerencia.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Texto auxiliar para ampliar el contexto.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Motivo en lenguaje natural que explica por qué se recomienda.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de recomendación (ej. activity, wellbeing, rest, focus).
    /// </summary>
    public string RecommendationType { get; set; } = "activity";

    /// <summary>
    /// Fecha y hora sugeridas para llevar a cabo la actividad.
    /// </summary>
    public DateTime SuggestedStart { get; set; }

    /// <summary>
    /// Duración sugerida en minutos.
    /// </summary>
    public int SuggestedDurationMinutes { get; set; }

    /// <summary>
    /// Nivel de confianza (0-1) de la recomendación.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Categoría asociada cuando corresponde.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Nombre de la categoría asociada.
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Impacto emocional esperado o resumen del beneficio.
    /// </summary>
    public string? MoodImpact { get; set; }

    /// <summary>
    /// Texto breve con el "por qué" sintetizado.
    /// </summary>
    public string? Summary { get; set; }
}
