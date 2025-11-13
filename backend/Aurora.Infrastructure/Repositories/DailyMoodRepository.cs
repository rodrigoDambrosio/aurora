using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aurora.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de registros de estado de ánimo diario.
/// </summary>
public class DailyMoodRepository : Repository<DailyMoodEntry>, IDailyMoodRepository
{
    public DailyMoodRepository(AuroraDbContext context) : base(context)
    {
    }

    public async Task<DailyMoodEntry?> GetByDateAsync(Guid userId, DateTime entryDateUtc)
    {
        var normalizedDate = DateTime.SpecifyKind(entryDateUtc.Date, DateTimeKind.Utc);
        var nextDay = normalizedDate.AddDays(1);

        return await _dbSet
            .FirstOrDefaultAsync(entry => entry.UserId == userId
                                         && entry.EntryDate >= normalizedDate
                                         && entry.EntryDate < nextDay
                                         && entry.IsActive);
    }

    public async Task<IReadOnlyList<DailyMoodEntry>> GetMonthlyEntriesAsync(Guid userId, int year, int month)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        return await _dbSet
            .AsNoTracking()
            .Where(entry => entry.UserId == userId
                            && entry.EntryDate >= monthStart
                            && entry.EntryDate < monthEnd
                            && entry.IsActive)
            .OrderBy(entry => entry.EntryDate)
            .ToListAsync();
    }

    public async Task RemoveEntriesBeforeAsync(Guid userId, DateTime cutoffDateUtc)
    {
        var normalizedCutoff = cutoffDateUtc.Date;

        var entries = await _dbSet
            .Where(entry => entry.UserId == userId
                            && entry.EntryDate < normalizedCutoff
                            && entry.IsActive)
            .ToListAsync();

        if (entries.Count == 0)
        {
            return;
        }

        foreach (var entry in entries)
        {
            entry.IsActive = false;
            entry.UpdatedAt = DateTime.UtcNow;
        }

        UpdateRange(entries);
        await SaveChangesAsync();
    }
}
