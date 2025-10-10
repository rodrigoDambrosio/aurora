using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aurora.Infrastructure.Repositories;

/// <summary>
/// Repositorio específico para la entidad Event
/// </summary>
public class EventRepository : Repository<Event>, IEventRepository
{
    public EventRepository(AuroraDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Event>> GetEventsByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(e => e.UserId == userId)
            .Include(e => e.EventCategory)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetEventsForWeekAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(e => e.UserId == userId &&
                       e.StartDate >= startDate &&
                       e.StartDate <= endDate)
            .Include(e => e.EventCategory)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetWeeklyEventsAsync(Guid userId, DateTime weekStart, Guid? categoryId = null)
    {
        var weekEnd = weekStart.AddDays(7);

        IQueryable<Event> query = _dbSet
            .Where(e => e.UserId == userId &&
                       e.StartDate >= weekStart &&
                       e.StartDate < weekEnd);

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.EventCategoryId == categoryId.Value);
        }

        return await query
            .Include(e => e.EventCategory)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetMonthlyEventsAsync(Guid userId, int year, int month, Guid? categoryId = null)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);

        IQueryable<Event> query = _dbSet
            .Where(e => e.UserId == userId &&
                       e.StartDate >= monthStart &&
                       e.StartDate < monthEnd);

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.EventCategoryId == categoryId.Value);
        }

        return await query
            .Include(e => e.EventCategory)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetEventsByCategoryAsync(Guid userId, Guid categoryId)
    {
        return await _dbSet
            .Where(e => e.UserId == userId && e.EventCategoryId == categoryId)
            .Include(e => e.EventCategory)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetOverlappingEventsAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(e => e.UserId == userId &&
                       !e.IsAllDay &&
                       ((e.StartDate < endDate && e.EndDate > startDate)))
            .Include(e => e.EventCategory)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetRecurringEventsAsync(Guid userId)
    {
        // Por ahora devolver lista vacía ya que no hemos implementado recurrencia
        // En el futuro aquí se filtrarían eventos con reglas de recurrencia
        return await Task.FromResult(new List<Event>());
    }

    public async Task<bool> UserHasAccessToEventAsync(Guid eventId, Guid userId)
    {
        return await _dbSet.AnyAsync(e => e.Id == eventId && e.UserId == userId);
    }

    public async Task<IEnumerable<Event>> GetEventsByCategoryAsync(Guid categoryId)
    {
        return await _dbSet
            .Where(e => e.EventCategoryId == categoryId)
            .Include(e => e.EventCategory)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(Guid userId, int count = 10)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(e => e.UserId == userId && e.StartDate > now)
            .Include(e => e.EventCategory)
            .OrderBy(e => e.StartDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<bool> HasConflictingEventsAsync(Guid userId, DateTime startDate, DateTime endDate, Guid? excludeEventId = null)
    {
        var query = _dbSet.Where(e => e.UserId == userId &&
                                      !e.IsAllDay &&
                                      ((e.StartDate < endDate && e.EndDate > startDate)));

        if (excludeEventId.HasValue)
        {
            query = query.Where(e => e.Id != excludeEventId);
        }

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<Event>> GetEventsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(e => e.UserId == userId &&
                       e.StartDate >= startDate &&
                       e.StartDate <= endDate)
            .Include(e => e.EventCategory)
            .OrderBy(e => e.StartDate)
            .ThenBy(e => e.Title)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetAllDayEventsForDateAsync(Guid userId, DateTime date)
    {
        var dateOnly = date.Date;
        var nextDay = dateOnly.AddDays(1);

        return await _dbSet
            .Where(e => e.UserId == userId &&
                       e.IsAllDay &&
                       e.StartDate >= dateOnly &&
                       e.StartDate < nextDay)
            .Include(e => e.EventCategory)
            .OrderBy(e => e.StartDate)
            .ThenBy(e => e.Title)
            .ToListAsync();
    }

    public override async Task<Event?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(e => e.EventCategory)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);
    }
}