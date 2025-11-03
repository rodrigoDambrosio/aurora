namespace Aurora.Domain.Entities;

/// <summary>
/// Entidad que representa un usuario del sistema
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Dirección de correo electrónico del usuario (único)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del usuario
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hash de la contraseña del usuario
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el email del usuario ha sido verificado
    /// </summary>
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>
    /// Fecha y hora del último login del usuario
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Zona horaria del usuario
    /// </summary>
    public string? Timezone { get; set; }

    // Navegación
    /// <summary>
    /// Colección de eventos creados por este usuario
    /// </summary>
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    /// <summary>
    /// Categorías de eventos personalizadas creadas por este usuario
    /// </summary>
    public virtual ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();

    /// <summary>
    /// Preferencias del usuario
    /// </summary>
    public virtual UserPreferences? Preferences { get; set; }

    /// <summary>
    /// Sesiones de autenticación activas del usuario
    /// </summary>
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();

    /// <summary>
    /// Registros de estado de ánimo del usuario
    /// </summary>
    public virtual ICollection<DailyMoodEntry> DailyMoodEntries { get; set; } = new List<DailyMoodEntry>();

    /// <summary>
    /// Feedback entregado sobre recomendaciones generadas.
    /// </summary>
    public virtual ICollection<RecommendationFeedback> RecommendationFeedback { get; set; } = new List<RecommendationFeedback>();
}