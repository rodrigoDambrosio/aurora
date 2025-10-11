using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aurora.Infrastructure.Repositories;

/// <summary>
/// Repositorio concreto para la entidad User.
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AuroraDbContext context) : base(context)
    {
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbSet.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbSet.AnyAsync(user => user.Email == email, cancellationToken);
    }
}
