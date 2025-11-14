using Aurora.Domain.Enums;

namespace Aurora.Application.DTOs;

public class CreateReminderDto
{
    public Guid EventId { get; set; }
    public ReminderType ReminderType { get; set; }
    public int? CustomTimeHours { get; set; }
    public int? CustomTimeMinutes { get; set; }
}
