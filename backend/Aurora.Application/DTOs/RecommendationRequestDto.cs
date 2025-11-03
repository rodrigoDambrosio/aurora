using System;

namespace Aurora.Application.DTOs;

/// <summary>
/// Parámetros opcionales para solicitar recomendaciones personalizadas.
/// </summary>
public class RecommendationRequestDto
{
    /// <summary>
    /// Fecha de referencia para generar las sugerencias. Si no se indica se usa la fecha actual.
    /// </summary>
    public DateTime? ReferenceDate { get; set; }

    /// <summary>
    /// Cantidad máxima de recomendaciones solicitadas (5 - 10 sugerido).
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Estado de ánimo actual informado por el cliente (1-5). Permite refinar el contexto.
    /// </summary>
    public int? CurrentMood { get; set; }

    /// <summary>
    /// Resumen de condiciones externas relevantes (ej. "Lluvioso", "Frío").
    /// </summary>
    public string? ExternalContext { get; set; }
}
