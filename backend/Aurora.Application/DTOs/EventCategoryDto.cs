namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para transferencia de datos de categorías de eventos
/// </summary>
public class EventCategoryDto
{
    /// <summary>
    /// Identificador único de la categoría
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nombre de la categoría
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la categoría
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Color de la categoría en formato hexadecimal
    /// </summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Icono de la categoría
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Indica si es una categoría predeterminada del sistema
    /// </summary>
    public bool IsSystemDefault { get; set; }

    /// <summary>
    /// Orden de visualización
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// ID del usuario propietario (null para categorías del sistema)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Fecha de creación
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}