using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Aurora.Application.Services;

/// <summary>
/// Servicio de aplicación para gestionar los registros de estado de ánimo diario.
/// </summary>
public class DailyMoodService : IDailyMoodService
{
    private readonly IDailyMoodRepository _dailyMoodRepository;
    private readonly ILogger<DailyMoodService> _logger;

    public DailyMoodService(IDailyMoodRepository dailyMoodRepository, ILogger<DailyMoodService> logger)
    {
        _dailyMoodRepository = dailyMoodRepository ?? throw new ArgumentNullException(nameof(dailyMoodRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MonthlyMoodResponseDto> GetMonthlyMoodEntriesAsync(Guid? userId, int year, int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "El mes debe estar entre 1 y 12.");
        }

        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        _logger.LogInformation("Recuperando registros de ánimo para usuario {UserId} - {Year}-{Month}", currentUserId, year, month);

        var entries = await _dailyMoodRepository.GetMonthlyEntriesAsync(currentUserId, year, month);

        var dtoEntries = entries
            .OrderBy(e => e.EntryDate)
            .Select(MapToDto)
            .ToList();

        return new MonthlyMoodResponseDto
        {
            Year = year,
            Month = month,
            Entries = dtoEntries
        };
    }

    public async Task<DailyMoodEntryDto> UpsertDailyMoodAsync(Guid? userId, UpsertDailyMoodDto dto)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (dto.MoodRating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.MoodRating), "La calificación debe estar entre 1 y 5.");
        }

        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);

        // Normalizar fecha a UTC sin componente de hora
        var normalizedDateUtc = DateTime.SpecifyKind(dto.EntryDate.Date, DateTimeKind.Utc);

        _logger.LogInformation("Registrando estado de ánimo para {UserId} en {EntryDate}", currentUserId, normalizedDateUtc.ToString("yyyy-MM-dd"));

        var existingEntry = await _dailyMoodRepository.GetByDateAsync(currentUserId, normalizedDateUtc);

        var trimmedNotes = string.IsNullOrWhiteSpace(dto.Notes)
            ? null
            : dto.Notes.Trim();

        if (existingEntry == null)
        {
            var newEntry = new DailyMoodEntry
            {
                EntryDate = normalizedDateUtc,
                MoodRating = dto.MoodRating,
                Notes = trimmedNotes,
                UserId = currentUserId,
                IsActive = true
            };

            await _dailyMoodRepository.AddAsync(newEntry);
            await _dailyMoodRepository.SaveChangesAsync();

            return MapToDto(newEntry);
        }

        existingEntry.MoodRating = dto.MoodRating;
        existingEntry.Notes = trimmedNotes;
        existingEntry.UpdatedAt = DateTime.UtcNow;

        await _dailyMoodRepository.UpdateAsync(existingEntry);
        await _dailyMoodRepository.SaveChangesAsync();

        return MapToDto(existingEntry);
    }

    public async Task<bool> DeleteDailyMoodAsync(Guid? userId, DateTime entryDate)
    {
        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);
        var normalizedDateUtc = DateTime.SpecifyKind(entryDate.Date, DateTimeKind.Utc);

        _logger.LogInformation("Eliminando registro de ánimo para {UserId} en {EntryDate}", currentUserId, normalizedDateUtc.ToString("yyyy-MM-dd"));

        var existingEntry = await _dailyMoodRepository.GetByDateAsync(currentUserId, normalizedDateUtc);
        if (existingEntry == null)
        {
            return false;
        }

        existingEntry.IsActive = false;
        existingEntry.UpdatedAt = DateTime.UtcNow;
        await _dailyMoodRepository.UpdateAsync(existingEntry);
        await _dailyMoodRepository.SaveChangesAsync();
        return true;
    }

    private static DailyMoodEntryDto MapToDto(DailyMoodEntry entry)
    {
        return new DailyMoodEntryDto
        {
            Id = entry.Id,
            EntryDate = DateTime.SpecifyKind(entry.EntryDate.Date, DateTimeKind.Utc),
            MoodRating = entry.MoodRating,
            Notes = entry.Notes,
            UserId = entry.UserId,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }
}
