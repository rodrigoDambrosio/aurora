using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Application.Services.Helpers;
using Aurora.Domain.Entities;

namespace Aurora.Application.Services;

public class ReminderService : IReminderService
{
    private readonly IRepository<EventReminder> _reminderRepository;
    private readonly IRepository<Event> _eventRepository;

    public ReminderService(
        IRepository<EventReminder> reminderRepository,
        IRepository<Event> eventRepository)
    {
        _reminderRepository = reminderRepository;
        _eventRepository = eventRepository;
    }

    public async Task<ReminderDto> CreateReminderAsync(CreateReminderDto dto)
    {
        // Verificar que el evento existe
        var eventExists = await _eventRepository.GetByIdAsync(dto.EventId);
        if (eventExists == null)
        {
            throw new InvalidOperationException($"El evento con ID {dto.EventId} no existe");
        }

        // Calcular la fecha/hora de disparo
        var triggerDateTime = ReminderCalculator.CalculateTriggerDateTime(
            eventExists.StartDate,
            dto.ReminderType,
            dto.CustomTimeHours,
            dto.CustomTimeMinutes);

        // Validar que el recordatorio es para el futuro
        if (triggerDateTime <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("No se pueden crear recordatorios para eventos pasados");
        }

        // Crear la entidad
        var reminder = new EventReminder
        {
            Id = Guid.NewGuid(),
            EventId = dto.EventId,
            ReminderType = dto.ReminderType,
            CustomTimeHours = dto.CustomTimeHours,
            CustomTimeMinutes = dto.CustomTimeMinutes,
            TriggerDateTime = triggerDateTime,
            IsSent = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _reminderRepository.AddAsync(reminder);
        await _reminderRepository.SaveChangesAsync();

        return await MapToDto(reminder);
    }

    public async Task<IEnumerable<ReminderDto>> GetPendingRemindersAsync()
    {
        var now = DateTime.UtcNow;
        var toleranceMinutes = 2;
        var maxTriggerTime = now.AddMinutes(toleranceMinutes);

        // Obtener todos los recordatorios y filtrar en memoria
        var allReminders = await _reminderRepository.GetAllAsync();

        var pendingReminders = allReminders
            .Where(r => r.IsActive && r.TriggerDateTime <= maxTriggerTime && !r.IsSent)
            .ToList();

        var dtos = new List<ReminderDto>();
        foreach (var reminder in pendingReminders)
        {
            dtos.Add(await MapToDto(reminder));
        }

        return dtos;
    }

    public async Task<ReminderDto> GetReminderByIdAsync(Guid id)
    {
        var reminder = await _reminderRepository.GetByIdAsync(id);

        if (reminder == null)
        {
            throw new InvalidOperationException($"El recordatorio con ID {id} no existe");
        }

        return await MapToDto(reminder);
    }

    public async Task<IEnumerable<ReminderDto>> GetRemindersByEventIdAsync(Guid eventId)
    {
        // Obtener todos los recordatorios y filtrar en memoria
        var allReminders = await _reminderRepository.GetAllAsync();

        var eventReminders = allReminders
            .Where(r => r.IsActive && r.EventId == eventId)
            .ToList();

        var dtos = new List<ReminderDto>();
        foreach (var reminder in eventReminders)
        {
            dtos.Add(await MapToDto(reminder));
        }

        return dtos;
    }

    public async Task DeleteReminderAsync(Guid id)
    {
        var reminder = await _reminderRepository.GetByIdAsync(id);
        if (reminder == null)
        {
            throw new InvalidOperationException($"El recordatorio con ID {id} no existe");
        }

        await _reminderRepository.DeleteAsync(id);
        await _reminderRepository.SaveChangesAsync();
    }

    public async Task DeleteAllRemindersAsync()
    {
        var allReminders = await _reminderRepository.GetAllAsync();

        foreach (var reminder in allReminders)
        {
            await _reminderRepository.DeleteAsync(reminder.Id);
        }

        await _reminderRepository.SaveChangesAsync();
    }

    public async Task MarkAsSentAsync(Guid id)
    {
        var reminder = await _reminderRepository.GetByIdAsync(id);
        if (reminder == null)
        {
            throw new InvalidOperationException($"El recordatorio con ID {id} no existe");
        }

        reminder.IsSent = true;
        reminder.UpdatedAt = DateTime.UtcNow;

        await _reminderRepository.UpdateAsync(reminder);
        await _reminderRepository.SaveChangesAsync();
    }

    private async Task<ReminderDto> MapToDto(EventReminder reminder)
    {
        // Cargar el evento si no está cargado
        var eventEntity = await _eventRepository.GetByIdAsync(reminder.EventId);
        if (eventEntity == null)
        {
            throw new InvalidOperationException($"El evento con ID {reminder.EventId} no existe");
        }

        return new ReminderDto
        {
            Id = reminder.Id,
            EventId = reminder.EventId,
            EventTitle = eventEntity.Title,
            EventStartDate = eventEntity.StartDate,
            EventCategoryColor = eventEntity.EventCategory?.Color ?? "#000000",
            ReminderType = reminder.ReminderType,
            CustomTimeHours = reminder.CustomTimeHours,
            CustomTimeMinutes = reminder.CustomTimeMinutes,
            TriggerDateTime = reminder.TriggerDateTime,
            IsSent = reminder.IsSent,
            CreatedAt = reminder.CreatedAt
        };
    }
}
