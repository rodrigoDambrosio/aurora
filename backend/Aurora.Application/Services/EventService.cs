using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Aurora.Application.Services;

/// <summary>
/// Servicio para la gestión de eventos
/// </summary>
public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventCategoryRepository _categoryRepository;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventRepository eventRepository,
        IEventCategoryRepository categoryRepository,
        ILogger<EventService> logger)
    {
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WeeklyEventsResponseDto> GetWeeklyEventsAsync(Guid? userId, DateTime weekStart, Guid? categoryId = null)
    {
        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        // Calcular fin de semana (7 días después)
        var weekEnd = weekStart.AddDays(7).AddTicks(-1);

        // Obtener eventos de la semana
        var events = await _eventRepository.GetWeeklyEventsAsync(currentUserId, weekStart, categoryId);

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

    public async Task<WeeklyEventsResponseDto> GetMonthlyEventsAsync(Guid? userId, int year, int month, Guid? categoryId = null)
    {
        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        // Calcular inicio y fin del mes
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

        // Obtener eventos del mes
        var events = await _eventRepository.GetMonthlyEventsAsync(currentUserId, year, month, categoryId);

        // Obtener categorías disponibles para el usuario
        var categories = await _categoryRepository.GetAvailableCategoriesForUserAsync(currentUserId);

        // Convertir eventos a DTOs
        var eventDtos = events.Select(MapEventToDto).ToList();

        // Convertir categorías a DTOs
        var categoryDtos = categories.Select(MapCategoryToDto).ToList();

        return new WeeklyEventsResponseDto
        {
            Events = eventDtos,
            WeekStart = monthStart,
            WeekEnd = monthEnd,
            Categories = categoryDtos,
            TotalEvents = eventDtos.Count,
            HasMoreEvents = false
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
        EventCategory? categoryEntity;

        if (!canUseCategory)
        {
            // Si hay un nombre de categoría sugerido, intentar crearla automáticamente
            if (!string.IsNullOrWhiteSpace(createEventDto.SuggestedCategoryName))
            {
                _logger.LogInformation(
                    "Categoría {CategoryId} no disponible. Creando nueva categoría '{SuggestedName}' para usuario {UserId}",
                    createEventDto.EventCategoryId, createEventDto.SuggestedCategoryName, currentUserId);

                // Verificar si ya existe una categoría con ese nombre
                var existingCategories = await _categoryRepository.GetAvailableCategoriesForUserAsync(currentUserId);
                categoryEntity = existingCategories.FirstOrDefault(c =>
                    c.Name.Equals(createEventDto.SuggestedCategoryName, StringComparison.OrdinalIgnoreCase));

                if (categoryEntity == null)
                {
                    // Crear la nueva categoría
                    categoryEntity = new EventCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = createEventDto.SuggestedCategoryName.Trim(),
                        Description = $"Categoría creada automáticamente por IA",
                        Color = GenerateRandomColor(),
                        Icon = "category",
                        IsSystemDefault = false,
                        SortOrder = 100,
                        UserId = currentUserId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _categoryRepository.AddAsync(categoryEntity);
                    await _categoryRepository.SaveChangesAsync();

                    _logger.LogInformation(
                        "Nueva categoría '{CategoryName}' creada con ID {CategoryId}",
                        categoryEntity.Name, categoryEntity.Id);
                }

                createEventDto.EventCategoryId = categoryEntity.Id;
            }
            else
            {
                // Si no hay categoría sugerida, usar la primera disponible como fallback
                _logger.LogWarning(
                    "Categoría {CategoryId} no disponible para usuario {UserId}. Usando categoría por defecto.",
                    createEventDto.EventCategoryId, currentUserId);

                var availableCategories = await _categoryRepository.GetAvailableCategoriesForUserAsync(currentUserId);
                categoryEntity = availableCategories.FirstOrDefault()
                    ?? throw new InvalidOperationException("El usuario no tiene categorías disponibles. Por favor, crea una categoría primero.");

                createEventDto.EventCategoryId = categoryEntity.Id;
            }
        }
        else
        {
            categoryEntity = await _categoryRepository.GetByIdAsync(createEventDto.EventCategoryId)
                ?? throw new InvalidOperationException("La categoría seleccionada no existe.");
        }

        // Crear entidad evento
        var eventEntity = new Event
        {
            Title = createEventDto.Title,
            Description = createEventDto.Description,
            StartDate = EnsureUtc(createEventDto.StartDate),
            EndDate = EnsureUtc(createEventDto.EndDate),
            IsAllDay = createEventDto.IsAllDay,
            Location = createEventDto.Location,
            Notes = createEventDto.Notes,
            Color = createEventDto.Color,
            IsRecurring = createEventDto.IsRecurring,
            RecurrencePattern = createEventDto.RecurrencePattern,
            Priority = createEventDto.Priority,
            EventCategoryId = createEventDto.EventCategoryId,
            EventCategory = categoryEntity,
            UserId = currentUserId
        };

        var createdEvent = await _eventRepository.AddAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();

        // Asegurarse de que la navegación esté cargada al devolver el DTO
        if (createdEvent.EventCategory == null)
        {
            createdEvent = await _eventRepository.GetByIdAsync(createdEvent.Id)
                           ?? createdEvent;
        }

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

        var categoryEntity = await _categoryRepository.GetByIdAsync(updateEventDto.EventCategoryId)
            ?? throw new InvalidOperationException("La categoría seleccionada no existe.");

        // Actualizar propiedades
        eventEntity.Title = updateEventDto.Title;
        eventEntity.Description = updateEventDto.Description;
        eventEntity.StartDate = EnsureUtc(updateEventDto.StartDate);
        eventEntity.EndDate = EnsureUtc(updateEventDto.EndDate);
        eventEntity.IsAllDay = updateEventDto.IsAllDay;
        eventEntity.Location = updateEventDto.Location;
        eventEntity.Notes = updateEventDto.Notes;
        eventEntity.Color = updateEventDto.Color;
        eventEntity.IsRecurring = updateEventDto.IsRecurring;
        eventEntity.RecurrencePattern = updateEventDto.RecurrencePattern;
        eventEntity.Priority = updateEventDto.Priority;
        eventEntity.EventCategoryId = updateEventDto.EventCategoryId;
        eventEntity.EventCategory = categoryEntity;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        var updatedEvent = await _eventRepository.UpdateAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();

        if (updatedEvent.EventCategory == null)
        {
            updatedEvent = await _eventRepository.GetByIdAsync(updatedEvent.Id)
                          ?? updatedEvent;
        }

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
            StartDate = EnsureUtc(eventEntity.StartDate),
            EndDate = EnsureUtc(eventEntity.EndDate),
            IsAllDay = eventEntity.IsAllDay,
            Location = eventEntity.Location,
            Notes = eventEntity.Notes,
            Color = eventEntity.Color,
            Priority = eventEntity.Priority,
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

    private static DateTime EnsureUtc(DateTime value)
    {
        if (value == default)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static string GenerateRandomColor()
    {
        // Paleta de colores predefinidos para categorías
        var colors = new[]
        {
            "#3b82f6", // Azul
            "#8b5cf6", // Púrpura
            "#10b981", // Verde
            "#f59e0b", // Naranja
            "#ef4444", // Rojo
            "#06b6d4", // Cyan
            "#ec4899", // Rosa
            "#14b8a6", // Teal
            "#f97316", // Naranja oscuro
            "#6366f1"  // Índigo
        };

        var random = new Random();
        return colors[random.Next(colors.Length)];
    }
}