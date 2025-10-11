using Aurora.Domain.Entities;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Operaciones especializadas para la entidad User.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
