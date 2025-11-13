using System;

namespace Aurora.Application.DTOs;

/// <summary>
/// Resumen del impacto del estado de ánimo asociado a una categoría de eventos.
/// </summary>
public class CategoryMoodImpactDto
{
    /// <summary>
    /// Identificador de la categoría.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Nombre visible de la categoría.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Color asociado a la categoría, si existe.
    /// </summary>
    public string? CategoryColor { get; set; }

    /// <summary>
    /// Promedio de estado de ánimo registrado en eventos de la categoría.
    /// </summary>
    public double AverageMood { get; set; }

    /// <summary>
    /// Cantidad total de eventos con estado de ánimo registrado.
    /// </summary>
    public int EventCount { get; set; }

    /// <summary>
    /// Eventos calificados como positivos (>= 4).
    /// </summary>
    public int PositiveCount { get; set; }

    /// <summary>
    /// Eventos calificados como negativos (<= 2).
    /// </summary>
    public int NegativeCount { get; set; }
}
