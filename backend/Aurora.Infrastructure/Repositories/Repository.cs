using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Aurora.Infrastructure.Repositories;

/// <summary>
/// Implementación base del patrón Repository
/// </summary>
/// <typeparam name="T">Tipo de entidad que hereda de BaseEntity</typeparam>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AuroraDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AuroraDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.CountAsync(predicate);
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        return entities;
    }

    public virtual Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return Task.FromResult(entity);
    }

    public virtual T Update(T entity)
    {
        _dbSet.Update(entity);
        return entity;
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        // Implementar soft delete
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        Update(entity);
        return true;
    }

    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await _dbSet.AnyAsync(e => e.Id == id);
    }

    public virtual void Remove(T entity)
    {
        // Implementar soft delete
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        Update(entity);
    }

    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        // Implementar soft delete en lote
        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.IsActive = false;
            entity.UpdatedAt = now;
        }
        UpdateRange(entities);
    }

    public virtual void HardRemove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual void HardRemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public virtual async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public virtual void Dispose()
    {
        _context?.Dispose();
    }
}