using Aurora.Domain.Entities;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Interfaz para el repositorio de registros de estado de ánimo diario.
/// </summary>
public interface IDailyMoodRepository : IRepository<DailyMoodEntry>
{
    /// <summary>
    /// Obtiene los registros activos de un usuario para el mes indicado.
    /// </summary>
    Task<IReadOnlyList<DailyMoodEntry>> GetMonthlyEntriesAsync(Guid userId, int year, int month);

    /// <summary>
    /// Obtiene un registro por fecha para el usuario indicado.
    /// </summary>
    Task<DailyMoodEntry?> GetByDateAsync(Guid userId, DateTime entryDateUtc);

    /// <summary>
    /// Elimina (soft delete) todos los registros anteriores a la fecha indicada.
    /// </summary>
    Task RemoveEntriesBeforeAsync(Guid userId, DateTime cutoffDateUtc);
}
