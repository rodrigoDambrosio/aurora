using Aurora.Application.DTOs;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Servicio para generar sugerencias personalizadas de autocuidado
/// </summary>
public interface ISelfCareService
{
    /// <summary>
    /// Genera sugerencias de autocuidado personalizadas para el usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="request">Parámetros de la solicitud</param>
    /// <returns>Lista de sugerencias personalizadas</returns>
    Task<IEnumerable<SelfCareRecommendationDto>> GetRecommendationsAsync(Guid userId, SelfCareRequestDto request);

    /// <summary>
    /// Registra feedback de una sugerencia de autocuidado
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="feedback">Feedback de la sugerencia</param>
    Task RegisterFeedbackAsync(Guid userId, SelfCareFeedbackDto feedback);

    /// <summary>
    /// Obtiene sugerencias genéricas offline (fallback)
    /// </summary>
    /// <param name="count">Cantidad de sugerencias</param>
    /// <returns>Lista de sugerencias genéricas</returns>
    IEnumerable<SelfCareRecommendationDto> GetGenericRecommendations(int count = 5);
}
