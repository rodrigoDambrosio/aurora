using Aurora.Domain.Enums;

namespace Aurora.Application.DTOs;

public class ReminderDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartDate { get; set; }
    public string EventCategoryColor { get; set; } = string.Empty;
    public ReminderType ReminderType { get; set; }
    public int? CustomTimeHours { get; set; }
    public int? CustomTimeMinutes { get; set; }
    public DateTime TriggerDateTime { get; set; }
    public bool IsSent { get; set; }
    public DateTime CreatedAt { get; set; }
}
