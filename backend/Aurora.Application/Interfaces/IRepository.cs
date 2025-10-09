using Aurora.Domain.Entities;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Interfaz base para repositorios genéricos
/// </summary>
/// <typeparam name="T">Tipo de entidad</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Obtiene una entidad por su ID
    /// </summary>
    /// <param name="id">ID de la entidad</param>
    /// <returns>La entidad o null si no existe</returns>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Obtiene todas las entidades activas
    /// </summary>
    /// <returns>Lista de entidades</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Agrega una nueva entidad
    /// </summary>
    /// <param name="entity">Entidad a agregar</param>
    /// <returns>La entidad agregada</returns>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Actualiza una entidad existente
    /// </summary>
    /// <param name="entity">Entidad a actualizar</param>
    /// <returns>La entidad actualizada</returns>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Elimina una entidad (soft delete)
    /// </summary>
    /// <param name="id">ID de la entidad a eliminar</param>
    /// <returns>True si se eliminó correctamente</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Verifica si una entidad existe
    /// </summary>
    /// <param name="id">ID de la entidad</param>
    /// <returns>True si existe</returns>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// Guarda los cambios en la base de datos
    /// </summary>
    /// <returns>Número de entidades afectadas</returns>
    Task<int> SaveChangesAsync();
}