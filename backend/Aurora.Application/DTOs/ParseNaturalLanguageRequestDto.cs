using System.ComponentModel.DataAnnotations;

namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para solicitud de parsing de texto natural
/// </summary>
public class ParseNaturalLanguageRequestDto
{
    /// <summary>
    /// Texto en lenguaje natural a parsear (ej: "reunión mañana a las 3pm")
    /// </summary>
    [Required(ErrorMessage = "El texto es requerido")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "El texto debe tener entre 3 y 500 caracteres")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Offset de zona horaria en minutos desde UTC (ej: -180 para Argentina UTC-3)
    /// </summary>
    public int TimezoneOffsetMinutes { get; set; } = 0;
}
