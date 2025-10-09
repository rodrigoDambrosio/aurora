using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Domain.Services;

namespace Aurora.Application.Services;

/// <summary>
/// Servicio para la gestión de eventos
/// </summary>
public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventCategoryRepository _categoryRepository;

    public EventService(IEventRepository eventRepository, IEventCategoryRepository categoryRepository)
    {
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public async Task<WeeklyEventsResponseDto> GetWeeklyEventsAsync(Guid? userId, DateTime weekStart)
    {
        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        // Calcular fin de semana (7 días después)
        var weekEnd = weekStart.AddDays(7).AddTicks(-1);

        // Obtener eventos de la semana
        var events = await _eventRepository.GetWeeklyEventsAsync(currentUserId, weekStart);

        // Obtener categorías disponibles para el usuario
        var categories = await _categoryRepository.GetAvailableCategoriesForUserAsync(currentUserId);

        // Convertir eventos a DTOs
        var eventDtos = events.Select(MapEventToDto).ToList();

        // Convertir categorías a DTOs
        var categoryDtos = categories.Select(MapCategoryToDto).ToList();

        return new WeeklyEventsResponseDto
        {
            Events = eventDtos,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Categories = categoryDtos,
            TotalEvents = eventDtos.Count,
            HasMoreEvents = false // TODO: Implementar lógica de paginación si es necesario
        };
    }

    public async Task<EventDto?> GetEventAsync(Guid eventId, Guid? userId)
    {
        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        var eventEntity = await _eventRepository.GetByIdAsync(eventId);

        if (eventEntity == null || !eventEntity.BelongsToUser(currentUserId))
        {
            return null;
        }

        return MapEventToDto(eventEntity);
    }

    public async Task<EventDto> CreateEventAsync(Guid? userId, CreateEventDto createEventDto)
    {
        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        // Verificar que la categoría existe y el usuario puede usarla
        var canUseCategory = await _categoryRepository.UserCanUseCategoryAsync(createEventDto.EventCategoryId, currentUserId);
        if (!canUseCategory)
        {
            throw new InvalidOperationException("La categoría especificada no está disponible para este usuario");
        }

        // Crear entidad evento
        var eventEntity = new Event
        {
            Title = createEventDto.Title,
            Description = createEventDto.Description,
            StartDate = createEventDto.StartDate,
            EndDate = createEventDto.EndDate,
            IsAllDay = createEventDto.IsAllDay,
            Location = createEventDto.Location,
            Notes = createEventDto.Notes,
            Color = createEventDto.Color,
            IsRecurring = createEventDto.IsRecurring,
            RecurrencePattern = createEventDto.RecurrencePattern,
            EventCategoryId = createEventDto.EventCategoryId,
            UserId = currentUserId
        };

        var createdEvent = await _eventRepository.AddAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();

        return MapEventToDto(createdEvent);
    }

    public async Task<EventDto> UpdateEventAsync(Guid eventId, Guid? userId, CreateEventDto updateEventDto)
    {
        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        var eventEntity = await _eventRepository.GetByIdAsync(eventId);

        if (eventEntity == null || !eventEntity.BelongsToUser(currentUserId))
        {
            throw new InvalidOperationException("Evento no encontrado o sin permisos para modificarlo");
        }

        // Verificar que la categoría existe y el usuario puede usarla
        var canUseCategory = await _categoryRepository.UserCanUseCategoryAsync(updateEventDto.EventCategoryId, currentUserId);
        if (!canUseCategory)
        {
            throw new InvalidOperationException("La categoría especificada no está disponible para este usuario");
        }

        // Actualizar propiedades
        eventEntity.Title = updateEventDto.Title;
        eventEntity.Description = updateEventDto.Description;
        eventEntity.StartDate = updateEventDto.StartDate;
        eventEntity.EndDate = updateEventDto.EndDate;
        eventEntity.IsAllDay = updateEventDto.IsAllDay;
        eventEntity.Location = updateEventDto.Location;
        eventEntity.Notes = updateEventDto.Notes;
        eventEntity.Color = updateEventDto.Color;
        eventEntity.IsRecurring = updateEventDto.IsRecurring;
        eventEntity.RecurrencePattern = updateEventDto.RecurrencePattern;
        eventEntity.EventCategoryId = updateEventDto.EventCategoryId;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        var updatedEvent = await _eventRepository.UpdateAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();

        return MapEventToDto(updatedEvent);
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, Guid? userId)
    {
        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        var eventEntity = await _eventRepository.GetByIdAsync(eventId);

        if (eventEntity == null || !eventEntity.BelongsToUser(currentUserId))
        {
            return false;
        }

        await _eventRepository.DeleteAsync(eventId);
        await _eventRepository.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<EventDto>> GetEventsByDateRangeAsync(Guid? userId, DateTime startDate, DateTime endDate)
    {
        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        var events = await _eventRepository.GetEventsByDateRangeAsync(currentUserId, startDate, endDate);

        return events.Select(MapEventToDto);
    }

    public async Task<IEnumerable<EventDto>> GetEventsByCategoryAsync(Guid? userId, Guid categoryId)
    {
        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        var events = await _eventRepository.GetEventsByCategoryAsync(currentUserId, categoryId);

        return events.Select(MapEventToDto);
    }

    public async Task<IEnumerable<EventDto>> GetConflictingEventsAsync(Guid? userId, DateTime startDate, DateTime endDate, Guid? excludeEventId = null)
    {
        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        var overlappingEvents = await _eventRepository.GetOverlappingEventsAsync(currentUserId, startDate, endDate);

        if (excludeEventId.HasValue)
        {
            overlappingEvents = overlappingEvents.Where(e => e.Id != excludeEventId.Value);
        }

        return overlappingEvents.Select(MapEventToDto);
    }

    private static EventDto MapEventToDto(Event eventEntity)
    {
        return new EventDto
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            IsAllDay = eventEntity.IsAllDay,
            Location = eventEntity.Location,
            Notes = eventEntity.Notes,
            Color = eventEntity.Color,
            IsRecurring = eventEntity.IsRecurring,
            RecurrencePattern = eventEntity.RecurrencePattern,
            EventCategoryId = eventEntity.EventCategoryId,
            EventCategory = eventEntity.EventCategory != null ? MapCategoryToDto(eventEntity.EventCategory) : null,
            UserId = eventEntity.UserId,
            CreatedAt = eventEntity.CreatedAt,
            UpdatedAt = eventEntity.UpdatedAt
        };
    }

    private static EventCategoryDto MapCategoryToDto(EventCategory categoryEntity)
    {
        return new EventCategoryDto
        {
            Id = categoryEntity.Id,
            Name = categoryEntity.Name,
            Description = categoryEntity.Description,
            Color = categoryEntity.Color,
            Icon = categoryEntity.Icon,
            IsSystemDefault = categoryEntity.IsSystemDefault,
            SortOrder = categoryEntity.SortOrder,
            UserId = categoryEntity.UserId,
            CreatedAt = categoryEntity.CreatedAt,
            UpdatedAt = categoryEntity.UpdatedAt
        };
    }
}