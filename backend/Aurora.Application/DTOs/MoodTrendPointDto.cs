using System;

namespace Aurora.Application.DTOs;

/// <summary>
/// Representa un punto dentro de la tendencia de ánimo para un día específico.
/// </summary>
public class MoodTrendPointDto
{
    /// <summary>
    /// Día representado dentro del rango solicitado.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Promedio de estado de ánimo del día. Puede ser nulo si no hubo registro.
    /// </summary>
    public double? AverageMood { get; set; }

    /// <summary>
    /// Cantidad de registros de ánimo que alimentan el promedio.
    /// </summary>
    public int Entries { get; set; }
}
