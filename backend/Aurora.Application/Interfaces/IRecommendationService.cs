using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Application.DTOs;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Contrato para generar recomendaciones personalizadas y recibir feedback.
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Genera una colección de sugerencias en base al historial del usuario.
    /// </summary>
    Task<IReadOnlyCollection<RecommendationDto>> GetRecommendationsAsync(
        Guid? userId,
        RecommendationRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra feedback del usuario para mejorar las futuras sugerencias.
    /// </summary>
    Task RecordFeedbackAsync(
        Guid? userId,
        RecommendationFeedbackDto feedback,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene métricas agregadas del feedback del usuario en un período dado.
    /// </summary>
    Task<RecommendationFeedbackSummaryDto> GetFeedbackSummaryAsync(
        Guid? userId,
        DateTime periodStartUtc,
        CancellationToken cancellationToken = default);
}
