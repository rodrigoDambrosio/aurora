using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Aurora.Infrastructure.Repositories;

/// <summary>
/// Repositorio para administrar las sesiones de usuario persistidas.
/// </summary>
public class UserSessionRepository : Repository<UserSession>, IUserSessionRepository
{
    public UserSessionRepository(AuroraDbContext context) : base(context)
    {
    }

    public Task<UserSession?> GetByTokenIdAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        return _dbSet.FirstOrDefaultAsync(session => session.TokenId == tokenId, cancellationToken);
    }

    public async Task InvalidateSessionsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sessions = await _dbSet
            .Where(session => session.UserId == userId && session.IsActive && session.ExpiresAtUtc <= now)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            return;
        }

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.RevokedAtUtc = now;
            session.RevokedReason = "expired";
        }

        UpdateRange(sessions);
        await SaveChangesAsync();
    }
}
