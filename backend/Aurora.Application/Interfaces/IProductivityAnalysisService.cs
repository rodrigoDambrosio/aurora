using Aurora.Application.DTOs;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Servicio para análisis de productividad del usuario
/// </summary>
public interface IProductivityAnalysisService
{
    /// <summary>
    /// Genera un análisis completo de productividad basado en historial de eventos y estado de ánimo
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="periodDays">Cantidad de días hacia atrás a analizar (por defecto 30)</param>
    /// <returns>Análisis de productividad completo</returns>
    Task<ProductivityAnalysisDto> AnalyzeProductivityAsync(Guid userId, int periodDays = 30);
}
