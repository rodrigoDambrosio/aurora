using System;

namespace Aurora.Application.DTOs;

/// <summary>
/// Detalle resumido del registro de ánimo de un día.
/// </summary>
public class MoodDaySnapshotDto
{
    /// <summary>
    /// Fecha representada.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Calificación asignada ese día.
    /// </summary>
    public int MoodRating { get; set; }

    /// <summary>
    /// Nota opcional registrada.
    /// </summary>
    public string? Notes { get; set; }
}
