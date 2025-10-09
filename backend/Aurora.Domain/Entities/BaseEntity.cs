namespace Aurora.Domain.Entities;

/// <summary>
/// Entidad base que proporciona propiedades comunes para todas las entidades del dominio
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Identificador único de la entidad
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Fecha y hora de creación de la entidad
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha y hora de la última actualización de la entidad
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indica si la entidad está activa (soft delete)
    /// </summary>
    public bool IsActive { get; set; } = true;
}