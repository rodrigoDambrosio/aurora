using Aurora.Application.DTOs;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Servicio de aplicación para gestionar registros de estado de ánimo diario.
/// </summary>
public interface IDailyMoodService
{
    /// <summary>
    /// Obtiene los registros del mes indicado.
    /// </summary>
    Task<MonthlyMoodResponseDto> GetMonthlyMoodEntriesAsync(Guid? userId, int year, int month);

    /// <summary>
    /// Crea o actualiza un registro de estado de ánimo para la fecha indicada.
    /// </summary>
    Task<DailyMoodEntryDto> UpsertDailyMoodAsync(Guid? userId, UpsertDailyMoodDto dto);

    /// <summary>
    /// Elimina el registro de estado de ánimo de la fecha indicada.
    /// </summary>
    Task<bool> DeleteDailyMoodAsync(Guid? userId, DateTime entryDate);
}
