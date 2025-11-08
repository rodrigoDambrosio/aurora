using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Domain.Enums;
using Aurora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aurora.Infrastructure.Repositories;

/// <summary>
/// Repositorio para sugerencias de reorganización
/// </summary>
public class ScheduleSuggestionRepository : IScheduleSuggestionRepository
{
    private readonly AuroraDbContext _context;

    public ScheduleSuggestionRepository(AuroraDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ScheduleSuggestion>> GetPendingSuggestionsByUserIdAsync(Guid userId)
    {
        return await _context.ScheduleSuggestions
            .Include(s => s.Event)
            .Where(s => s.UserId == userId && s.Status == SuggestionStatus.Pending)
            .OrderByDescending(s => s.Priority)
            .ThenByDescending(s => s.ConfidenceScore)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<ScheduleSuggestion?> GetByIdAsync(Guid id)
    {
        return await _context.ScheduleSuggestions
            .Include(s => s.Event)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<ScheduleSuggestion> CreateAsync(ScheduleSuggestion suggestion)
    {
        _context.ScheduleSuggestions.Add(suggestion);
        await _context.SaveChangesAsync();
        return suggestion;
    }

    public async Task UpdateAsync(ScheduleSuggestion suggestion)
    {
        _context.ScheduleSuggestions.Update(suggestion);
        await _context.SaveChangesAsync();
    }

    public async Task ExpireOldSuggestionsAsync(Guid userId, DateTime beforeDate)
    {
        var oldSuggestions = await _context.ScheduleSuggestions
            .Where(s => s.UserId == userId && 
                       s.Status == SuggestionStatus.Pending && 
                       s.CreatedAt < beforeDate)
            .ToListAsync();

        foreach (var suggestion in oldSuggestions)
        {
            suggestion.Status = SuggestionStatus.Expired;
        }

        await _context.SaveChangesAsync();
    }
}
