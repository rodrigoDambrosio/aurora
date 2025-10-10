using Aurora.Application.DTOs;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Interfaz para el servicio de gestión de eventos
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Obtiene eventos de una semana específica para un usuario
    /// </summary>
    /// <param name="userId">ID del usuario (opcional en modo desarrollo)</param>
    /// <param name="weekStart">Fecha de inicio de la semana</param>
    /// <param name="categoryId">ID de categoría para filtrar (opcional)</param>
    /// <returns>Respuesta con eventos de la semana</returns>
    Task<WeeklyEventsResponseDto> GetWeeklyEventsAsync(Guid? userId, DateTime weekStart, Guid? categoryId = null);

    /// <summary>
    /// Obtiene eventos de un mes específico para un usuario
    /// </summary>
    /// <param name="userId">ID del usuario (opcional en modo desarrollo)</param>
    /// <param name="year">Año del mes a consultar</param>
    /// <param name="month">Mes a consultar (1-12)</param>
    /// <param name="categoryId">ID de categoría para filtrar (opcional)</param>
    /// <returns>Respuesta con eventos del mes</returns>
    Task<WeeklyEventsResponseDto> GetMonthlyEventsAsync(Guid? userId, int year, int month, Guid? categoryId = null);

    /// <summary>
    /// Obtiene un evento específico por su ID
    /// </summary>
    /// <param name="eventId">ID del evento</param>
    /// <param name="userId">ID del usuario (opcional en modo desarrollo)</param>
    /// <returns>Datos del evento o null si no existe</returns>
    Task<EventDto?> GetEventAsync(Guid eventId, Guid? userId);

    /// <summary>
    /// Crea un nuevo evento
    /// </summary>
    /// <param name="userId">ID del usuario (opcional en modo desarrollo)</param>
    /// <param name="createEventDto">Datos del evento a crear</param>
    /// <returns>Evento creado</returns>
    Task<EventDto> CreateEventAsync(Guid? userId, CreateEventDto createEventDto);

    /// <summary>
    /// Actualiza un evento existente
    /// </summary>
    /// <param name="eventId">ID del evento a actualizar</param>
    /// <param name="userId">ID del usuario (opcional en modo desarrollo)</param>
    /// <param name="updateEventDto">Nuevos datos del evento</param>
    /// <returns>Evento actualizado</returns>
    Task<EventDto> UpdateEventAsync(Guid eventId, Guid? userId, CreateEventDto updateEventDto);

    /// <summary>
    /// Elimina un evento
    /// </summary>
    /// <param name="eventId">ID del evento a eliminar</param>
    /// <param name="userId">ID del usuario (opcional en modo desarrollo)</param>
    /// <returns>True si se eliminó correctamente</returns>
    Task<bool> DeleteEventAsync(Guid eventId, Guid? userId);

    /// <summary>
    /// Obtiene eventos por rango de fechas
    /// </summary>
    /// <param name="userId">ID del usuario (opcional en modo desarrollo)</param>
    /// <param name="startDate">Fecha de inicio</param>
    /// <param name="endDate">Fecha de fin</param>
    /// <returns>Lista de eventos en el rango</returns>
    Task<IEnumerable<EventDto>> GetEventsByDateRangeAsync(Guid? userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Obtiene eventos por categoría
    /// </summary>
    /// <param name="userId">ID del usuario (opcional en modo desarrollo)</param>
    /// <param name="categoryId">ID de la categoría</param>
    /// <returns>Lista de eventos de la categoría</returns>
    Task<IEnumerable<EventDto>> GetEventsByCategoryAsync(Guid? userId, Guid categoryId);

    /// <summary>
    /// Verifica si hay conflictos de horario para un evento
    /// </summary>
    /// <param name="userId">ID del usuario (opcional en modo desarrollo)</param>
    /// <param name="startDate">Fecha de inicio</param>
    /// <param name="endDate">Fecha de fin</param>
    /// <param name="excludeEventId">ID del evento a excluir de la verificación (para actualizaciones)</param>
    /// <returns>Lista de eventos que se superponen</returns>
    Task<IEnumerable<EventDto>> GetConflictingEventsAsync(Guid? userId, DateTime startDate, DateTime endDate, Guid? excludeEventId = null);
}