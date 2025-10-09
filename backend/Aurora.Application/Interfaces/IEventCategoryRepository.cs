using Aurora.Domain.Entities;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Interfaz específica para el repositorio de categorías de eventos
/// </summary>
public interface IEventCategoryRepository : IRepository<EventCategory>
{
    /// <summary>
    /// Obtiene categorías disponibles para un usuario (sistema + personalizadas)
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de categorías disponibles</returns>
    Task<IEnumerable<EventCategory>> GetAvailableCategoriesForUserAsync(Guid userId);

    /// <summary>
    /// Obtiene categorías del sistema
    /// </summary>
    /// <returns>Lista de categorías del sistema</returns>
    Task<IEnumerable<EventCategory>> GetSystemCategoriesAsync();

    /// <summary>
    /// Obtiene categorías personalizadas de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de categorías personalizadas</returns>
    Task<IEnumerable<EventCategory>> GetUserCustomCategoriesAsync(Guid userId);

    /// <summary>
    /// Verifica si una categoría pertenece a un usuario o es del sistema
    /// </summary>
    /// <param name="categoryId">ID de la categoría</param>
    /// <param name="userId">ID del usuario</param>
    /// <returns>True si el usuario puede usar la categoría</returns>
    Task<bool> UserCanUseCategoryAsync(Guid categoryId, Guid userId);

    /// <summary>
    /// Obtiene categorías ordenadas por SortOrder
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de categorías ordenadas</returns>
    Task<IEnumerable<EventCategory>> GetCategoriesOrderedAsync(Guid userId);
}