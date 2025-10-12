namespace Aurora.Application.DTOs.User;

/// <summary>
/// DTO para las preferencias del usuario
/// </summary>
public class UserPreferencesDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "es-ES";
    public int DefaultReminderMinutes { get; set; } = 15;
    public int FirstDayOfWeek { get; set; } = 1;
    public string TimeFormat { get; set; } = "24h";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string? WorkStartTime { get; set; }
    public string? WorkEndTime { get; set; }
    public List<int>? WorkDaysOfWeek { get; set; }
    public List<int>? ExerciseDaysOfWeek { get; set; }
    public List<string>? NlpKeywords { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
}
