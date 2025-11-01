using Aurora.Application.DTOs;

namespace Aurora.Application.Interfaces;

public interface IReminderService
{
    Task<ReminderDto> CreateReminderAsync(CreateReminderDto dto);
    Task<IEnumerable<ReminderDto>> GetPendingRemindersAsync();
    Task<ReminderDto> GetReminderByIdAsync(Guid id);
    Task<IEnumerable<ReminderDto>> GetRemindersByEventIdAsync(Guid eventId);
    Task DeleteReminderAsync(Guid id);
    Task MarkAsSentAsync(Guid id);
}
