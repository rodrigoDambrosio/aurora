namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para la creación de una nueva categoría de eventos
/// </summary>
public class CreateEventCategoryDto
{
    /// <summary>
    /// Nombre de la categoría (obligatorio, máx. 50 caracteres)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la categoría (opcional, máx. 200 caracteres)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Color de la categoría en formato hexadecimal (ej: #3b82f6)
    /// </summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Icono de la categoría (opcional, puede ser emoji o nombre de icono)
    /// </summary>
    public string? Icon { get; set; }
}
