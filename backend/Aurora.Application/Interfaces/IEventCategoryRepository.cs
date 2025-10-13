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

    /// <summary>
    /// Obtiene las categorías predeterminadas asociadas a un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de categorías predeterminadas</returns>
    Task<IEnumerable<EventCategory>> GetDefaultCategoriesAsync(Guid userId);

    /// <summary>
    /// Verifica si existe una categoría con el mismo nombre para un usuario
    /// </summary>
    /// <param name="name">Nombre de la categoría</param>
    /// <param name="userId">ID del usuario</param>
    /// <param name="excludeCategoryId">ID de categoría a excluir (para edición)</param>
    /// <returns>True si existe duplicado</returns>
    Task<bool> ExistsCategoryWithNameAsync(string name, Guid userId, Guid? excludeCategoryId = null);

    /// <summary>
    /// Obtiene el número de eventos asociados a una categoría
    /// </summary>
    /// <param name="categoryId">ID de la categoría</param>
    /// <returns>Número de eventos</returns>
    Task<int> GetEventCountForCategoryAsync(Guid categoryId);

    /// <summary>
    /// Reasigna todos los eventos de una categoría a otra
    /// </summary>
    /// <param name="fromCategoryId">ID de la categoría origen</param>
    /// <param name="toCategoryId">ID de la categoría destino</param>
    /// <returns>Número de eventos reasignados</returns>
    Task<int> ReassignEventsToAnotherCategoryAsync(Guid fromCategoryId, Guid toCategoryId);
}