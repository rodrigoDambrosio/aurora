using Aurora.Domain.Enums;

namespace Aurora.Domain.Entities;

public class EventReminder : BaseEntity
{
    public Guid EventId { get; set; }
    public ReminderType ReminderType { get; set; }
    public int? CustomTimeHours { get; set; }
    public int? CustomTimeMinutes { get; set; }
    public DateTime TriggerDateTime { get; set; }
    public bool IsSent { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
}
