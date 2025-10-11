using Aurora.Domain.Entities;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Contrato para administrar sesiones de usuario persistidas.
/// </summary>
public interface IUserSessionRepository : IRepository<UserSession>
{
    Task<UserSession?> GetByTokenIdAsync(Guid tokenId, CancellationToken cancellationToken = default);
    Task InvalidateSessionsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
