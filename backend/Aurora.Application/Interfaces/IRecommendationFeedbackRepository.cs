using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Domain.Entities;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Contrato de persistencia para el feedback de recomendaciones personalizadas.
/// </summary>
public interface IRecommendationFeedbackRepository : IRepository<RecommendationFeedback>
{
    /// <summary>
    /// Obtiene un feedback existente para el usuario y recomendación indicados.
    /// </summary>
    Task<RecommendationFeedback?> GetByUserAndRecommendationAsync(Guid userId, string recommendationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda los cambios pendientes en la base de datos con soporte de cancelación.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene el feedback registrado desde una fecha específica (UTC).
    /// </summary>
    Task<IReadOnlyList<RecommendationFeedback>> GetFromDateAsync(Guid userId, DateTime fromUtc, CancellationToken cancellationToken = default);
}
