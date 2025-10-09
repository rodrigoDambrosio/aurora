namespace Aurora.Domain.Entities;

/// <summary>
/// Entidad que representa una categoría de evento personalizable
/// </summary>
public class EventCategory : BaseEntity
{
    /// <summary>
    /// Nombre de la categoría
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la categoría (opcional)
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

    /// <summary>
    /// Indica si es una categoría predeterminada del sistema
    /// </summary>
    public bool IsSystemDefault { get; set; } = false;

    /// <summary>
    /// Orden de visualización de la categoría
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // Relaciones
    /// <summary>
    /// ID del usuario propietario de la categoría (null para categorías del sistema)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Usuario propietario de la categoría
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Eventos que pertenecen a esta categoría
    /// </summary>
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    // Métodos de dominio
    /// <summary>
    /// Verifica si la categoría es personalizada (creada por usuario)
    /// </summary>
    public bool IsCustomCategory => UserId.HasValue && !IsSystemDefault;

    /// <summary>
    /// Verifica si la categoría es del sistema
    /// </summary>
    public bool IsSystemCategory => !UserId.HasValue || IsSystemDefault;

    /// <summary>
    /// Verifica si la categoría está disponible para un usuario específico
    /// En modo desarrollo, todas las categorías están disponibles
    /// </summary>
    /// <param name="userId">ID del usuario, puede ser null para acceso anónimo</param>
    /// <returns>True si la categoría está disponible para el usuario</returns>
    public bool IsAvailableForUser(Guid? userId)
    {
        return IsAvailableForUser(userId, Constants.DomainConstants.Development.AllowAnonymousAccess);
    }

    /// <summary>
    /// Verifica si la categoría está disponible para un usuario específico
    /// Versión testeable que permite controlar el modo de desarrollo
    /// </summary>
    /// <param name="userId">ID del usuario, puede ser null para acceso anónimo</param>
    /// <param name="allowAnonymousAccess">Si permite acceso anónimo (modo desarrollo)</param>
    /// <returns>True si la categoría está disponible para el usuario</returns>
    public bool IsAvailableForUser(Guid? userId, bool allowAnonymousAccess)
    {
        // Categorías inactivas nunca están disponibles
        if (!IsActive)
            return false;

        // Categorías del sistema siempre están disponibles (si están activas)
        if (IsSystemDefault)
            return true;

        // En modo desarrollo, todas las categorías están disponibles
        if (allowAnonymousAccess)
            return true;

        // Categorías personalizadas solo para su propietario
        return UserId == userId;
    }
}