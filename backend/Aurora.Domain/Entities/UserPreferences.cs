namespace Aurora.Domain.Entities;

/// <summary>
/// Entidad que almacena las preferencias personalizadas del usuario
/// </summary>
public class UserPreferences : BaseEntity
{
    /// <summary>
    /// Zona horaria preferida del usuario
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Formato de fecha preferido (dd/MM/yyyy, MM/dd/yyyy, etc.)
    /// </summary>
    public string DateFormat { get; set; } = "dd/MM/yyyy";

    /// <summary>
    /// Formato de hora preferido (24h o 12h)
    /// </summary>
    public string TimeFormat { get; set; } = "24h";

    /// <summary>
    /// Primer día de la semana (0=Domingo, 1=Lunes)
    /// </summary>
    public int FirstDayOfWeek { get; set; } = 1; // Lunes por defecto

    /// <summary>
    /// Idioma preferido del usuario
    /// </summary>
    public string Language { get; set; } = "es-ES";

    /// <summary>
    /// Tema de la aplicación (light/dark)
    /// </summary>
    public string Theme { get; set; } = "light";

    /// <summary>
    /// Indica si recibir notificaciones por email
    /// </summary>
    public bool EmailNotifications { get; set; } = true;

    /// <summary>
    /// Minutos de antelación para recordatorios de eventos
    /// </summary>
    public int DefaultReminderMinutes { get; set; } = 15;

    /// <summary>
    /// Vista de calendario por defecto (week/month/day)
    /// </summary>
    public string DefaultCalendarView { get; set; } = "week";

    /// <summary>
    /// Hora de inicio del horario laboral (formato HH:mm)
    /// </summary>
    public string? WorkStartTime { get; set; }

    /// <summary>
    /// Hora de fin del horario laboral (formato HH:mm)
    /// </summary>
    public string? WorkEndTime { get; set; }

    /// <summary>
    /// Días laborales de la semana (0=Domingo, 1=Lunes, etc.)
    /// Almacenado como JSON
    /// </summary>
    public string? WorkDaysOfWeek { get; set; }

    /// <summary>
    /// Días preferidos para ejercicio (0=Domingo, 1=Lunes, etc.)
    /// Almacenado como JSON
    /// </summary>
    public string? ExerciseDaysOfWeek { get; set; }

    /// <summary>
    /// Palabras clave personalizadas para NLP
    /// Almacenado como JSON
    /// </summary>
    public string? NlpKeywords { get; set; }

    /// <summary>
    /// Indica si las notificaciones están habilitadas
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;

    // Relaciones
    /// <summary>
    /// ID del usuario propietario de estas preferencias
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Usuario propietario de estas preferencias
    /// </summary>
    public virtual User User { get; set; } = null!;
}