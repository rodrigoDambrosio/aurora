using Aurora.Application.DTOs;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Servicio para generar métricas agregadas del bienestar mensual.
/// </summary>
public interface IWellnessInsightsService
{
    /// <summary>
    /// Obtiene un resumen para el mes indicado del usuario autenticado.
    /// </summary>
    Task<WellnessSummaryDto> GetMonthlySummaryAsync(Guid? userId, int year, int month);
}
