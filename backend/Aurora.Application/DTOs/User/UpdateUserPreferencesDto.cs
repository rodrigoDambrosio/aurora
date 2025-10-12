namespace Aurora.Application.DTOs.User;

/// <summary>
/// DTO para actualizar las preferencias del usuario
/// </summary>
public class UpdateUserPreferencesDto
{
    public string? Theme { get; set; }
    public string? Language { get; set; }
    public int? DefaultReminderMinutes { get; set; }
    public int? FirstDayOfWeek { get; set; }
    public string? TimeFormat { get; set; }
    public string? DateFormat { get; set; }
    public string? WorkStartTime { get; set; }
    public string? WorkEndTime { get; set; }
    public List<int>? WorkDaysOfWeek { get; set; }
    public List<int>? ExerciseDaysOfWeek { get; set; }
    public List<string>? NlpKeywords { get; set; }
    public bool? NotificationsEnabled { get; set; }
}
