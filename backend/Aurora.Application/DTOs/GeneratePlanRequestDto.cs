namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para solicitar la generación de un plan multi-día
/// </summary>
public class GeneratePlanRequestDto
{
    /// <summary>
    /// Objetivo o meta a alcanzar (ej: "aprender a tocar la guitarra")
    /// </summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>
    /// Desplazamiento de la zona horaria en minutos
    /// </summary>
    public int TimezoneOffsetMinutes { get; set; }

    /// <summary>
    /// Duración deseada del plan en semanas (opcional, por defecto la IA decide)
    /// </summary>
    public int? DurationWeeks { get; set; }

    /// <summary>
    /// Sesiones por semana preferidas (opcional)
    /// </summary>
    public int? SessionsPerWeek { get; set; }

    /// <summary>
    /// Duración preferida de cada sesión en minutos (opcional)
    /// </summary>
    public int? SessionDurationMinutes { get; set; }

    /// <summary>
    /// Hora preferida del día para las sesiones (opcional, formato "HH:mm")
    /// </summary>
    public string? PreferredTimeOfDay { get; set; }

    /// <summary>
    /// ID de categoría para asignar a todos los eventos del plan (opcional)
    /// </summary>
    public Guid? CategoryId { get; set; }
}
