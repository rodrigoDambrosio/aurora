using Aurora.Domain.Entities;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Interfaz específica para el repositorio de eventos
/// </summary>
public interface IEventRepository : IRepository<Event>
{
    /// <summary>
    /// Obtiene eventos de un usuario en un rango de fechas
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="startDate">Fecha de inicio</param>
    /// <param name="endDate">Fecha de fin</param>
    /// <returns>Lista de eventos en el rango</returns>
    Task<IEnumerable<Event>> GetEventsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Obtiene eventos de una semana específica para un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="weekStart">Fecha de inicio de la semana</param>
    /// <param name="categoryId">ID de categoría para filtrar (opcional)</param>
    /// <returns>Lista de eventos de la semana</returns>
    Task<IEnumerable<Event>> GetWeeklyEventsAsync(Guid userId, DateTime weekStart, Guid? categoryId = null);

    /// <summary>
    /// Obtiene eventos de un mes específico para un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="year">Año del mes a consultar</param>
    /// <param name="month">Mes a consultar (1-12)</param>
    /// <param name="categoryId">ID de categoría para filtrar (opcional)</param>
    /// <returns>Lista de eventos del mes</returns>
    Task<IEnumerable<Event>> GetMonthlyEventsAsync(Guid userId, int year, int month, Guid? categoryId = null);

    /// <summary>
    /// Obtiene eventos de un usuario por categoría
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="categoryId">ID de la categoría</param>
    /// <returns>Lista de eventos de la categoría</returns>
    Task<IEnumerable<Event>> GetEventsByCategoryAsync(Guid userId, Guid categoryId);

    /// <summary>
    /// Obtiene eventos que se superponen con un rango de fechas
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="startDate">Fecha de inicio</param>
    /// <param name="endDate">Fecha de fin</param>
    /// <returns>Lista de eventos que se superponen</returns>
    Task<IEnumerable<Event>> GetOverlappingEventsAsync(Guid userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Obtiene eventos recurrentes de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de eventos recurrentes</returns>
    Task<IEnumerable<Event>> GetRecurringEventsAsync(Guid userId);

    /// <summary>
    /// Verifica si un usuario tiene acceso a un evento
    /// </summary>
    /// <param name="eventId">ID del evento</param>
    /// <param name="userId">ID del usuario</param>
    /// <returns>True si tiene acceso</returns>
    Task<bool> UserHasAccessToEventAsync(Guid eventId, Guid userId);
}