using Aurora.Domain.Entities;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Interfaz del repositorio de sugerencias de reorganización
/// </summary>
public interface IScheduleSuggestionRepository
{
    /// <summary>
    /// Obtiene todas las sugerencias pendientes de un usuario
    /// </summary>
    Task<IEnumerable<ScheduleSuggestion>> GetPendingSuggestionsByUserIdAsync(Guid userId);

    /// <summary>
    /// Obtiene una sugerencia por ID
    /// </summary>
    Task<ScheduleSuggestion?> GetByIdAsync(Guid id);

    /// <summary>
    /// Crea una nueva sugerencia
    /// </summary>
    Task<ScheduleSuggestion> CreateAsync(ScheduleSuggestion suggestion);

    /// <summary>
    /// Actualiza una sugerencia existente
    /// </summary>
    Task UpdateAsync(ScheduleSuggestion suggestion);

    /// <summary>
    /// Marca sugerencias como expiradas según criterios
    /// </summary>
    Task ExpireOldSuggestionsAsync(Guid userId, DateTime beforeDate);

    /// <summary>
    /// Descarta todas las sugerencias pendientes de un usuario
    /// </summary>
    Task DiscardPendingSuggestionsAsync(Guid userId);
}
