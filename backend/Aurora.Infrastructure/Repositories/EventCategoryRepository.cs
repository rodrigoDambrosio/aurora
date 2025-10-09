using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aurora.Infrastructure.Repositories;

/// <summary>
/// Repositorio específico para la entidad EventCategory
/// </summary>
public class EventCategoryRepository : Repository<EventCategory>, IEventCategoryRepository
{
    public EventCategoryRepository(AuroraDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<EventCategory>> GetAvailableCategoriesForUserAsync(Guid userId)
    {
        return await GetCategoriesByUserIdAsync(userId);
    }

    public async Task<IEnumerable<EventCategory>> GetSystemCategoriesAsync()
    {
        // En nuestro modelo, las categorías "del sistema" son las por defecto
        // que se crean para cada usuario
        return await _dbSet
            .Where(c => c.IsSystemDefault)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<EventCategory>> GetUserCustomCategoriesAsync(Guid userId)
    {
        return await GetCustomCategoriesAsync(userId);
    }

    public async Task<bool> UserCanUseCategoryAsync(Guid categoryId, Guid userId)
    {
        return await _dbSet.AnyAsync(c => c.Id == categoryId && c.UserId == userId);
    }

    public async Task<IEnumerable<EventCategory>> GetCategoriesOrderedAsync(Guid userId)
    {
        return await GetCategoriesByUserIdAsync(userId);
    }

    public async Task<IEnumerable<EventCategory>> GetCategoriesByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.IsSystemDefault ? 0 : 1) // Categorías por defecto primero
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<EventCategory>> GetDefaultCategoriesAsync(Guid userId)
    {
        return await _dbSet
            .Where(c => c.UserId == userId && c.IsSystemDefault)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<EventCategory>> GetCustomCategoriesAsync(Guid userId)
    {
        return await _dbSet
            .Where(c => c.UserId == userId && !c.IsSystemDefault)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<EventCategory?> GetCategoryByNameAsync(Guid userId, string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.UserId == userId &&
                                      c.Name.ToLower() == name.ToLower());
    }

    public async Task<bool> CategoryExistsAsync(Guid userId, string name, Guid? excludeCategoryId = null)
    {
        var query = _dbSet.Where(c => c.UserId == userId &&
                                      c.Name.ToLower() == name.ToLower());

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCategoryId);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> CategoryHasEventsAsync(Guid categoryId)
    {
        return await _context.Events
            .AnyAsync(e => e.EventCategoryId == categoryId);
    }

    public async Task<int> GetEventCountByCategoryAsync(Guid categoryId)
    {
        return await _context.Events
            .CountAsync(e => e.EventCategoryId == categoryId);
    }

    public async Task<IEnumerable<EventCategory>> GetCategoriesWithEventCountAsync(Guid userId)
    {
        return await _dbSet
            .Where(c => c.UserId == userId)
            .Select(c => new EventCategory
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Color = c.Color,
                Icon = c.Icon,
                IsSystemDefault = c.IsSystemDefault,
                UserId = c.UserId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsActive = c.IsActive,
                // Nota: Para obtener el conteo de eventos, se necesitaría una consulta adicional
                // o usar Include con proyección
            })
            .OrderBy(c => c.IsSystemDefault ? 0 : 1)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public override async Task<EventCategory?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}