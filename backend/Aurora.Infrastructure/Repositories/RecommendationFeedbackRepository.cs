using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aurora.Infrastructure.Repositories;

/// <summary>
/// Repositorio concreto para gestionar el feedback de recomendaciones.
/// </summary>
public class RecommendationFeedbackRepository : Repository<RecommendationFeedback>, IRecommendationFeedbackRepository
{
    public RecommendationFeedbackRepository(AuroraDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<RecommendationFeedback?> GetByUserAndRecommendationAsync(
        Guid userId,
        string recommendationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsQueryable()
            .FirstOrDefaultAsync(
                feedback => feedback.UserId == userId && feedback.RecommendationId == recommendationId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecommendationFeedback>> GetFromDateAsync(
        Guid userId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(feedback => feedback.UserId == userId && feedback.SubmittedAtUtc >= fromUtc)
            .OrderByDescending(feedback => feedback.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
