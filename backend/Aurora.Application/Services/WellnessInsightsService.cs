using System;
using System.Collections.Generic;
using System.Linq;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Aurora.Application.Services;

/// <summary>
/// Servicio encargado de calcular métricas agregadas para el tablero de bienestar.
/// </summary>
public class WellnessInsightsService : IWellnessInsightsService
{
    private const int PositiveThreshold = 4;
    private const int NegativeThreshold = 2;

    private readonly IDailyMoodRepository _dailyMoodRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<WellnessInsightsService> _logger;

    public WellnessInsightsService(
        IDailyMoodRepository dailyMoodRepository,
        IEventRepository eventRepository,
        ILogger<WellnessInsightsService> logger)
    {
        _dailyMoodRepository = dailyMoodRepository ?? throw new ArgumentNullException(nameof(dailyMoodRepository));
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WellnessSummaryDto> GetMonthlySummaryAsync(Guid? userId, int year, int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "El mes debe estar entre 1 y 12.");
        }

        var currentUserId = DevelopmentUserService.GetCurrentUserId(userId);
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        _logger.LogInformation(
            "Calculando resumen de bienestar para usuario {UserId} - {Year}-{Month}",
            currentUserId,
            year,
            month);

        var moodEntries = await _dailyMoodRepository.GetMonthlyEntriesAsync(currentUserId, year, month);
        var monthlyEvents = await _eventRepository.GetMonthlyEventsAsync(currentUserId, year, month);

        var orderedMoodEntries = moodEntries
            .Where(entry => entry.IsActive)
            .OrderBy(entry => entry.EntryDate)
            .ToList();

        var summary = BuildSummary(year, month, monthStart, orderedMoodEntries, monthlyEvents);
        summary.HasEventMoodData = monthlyEvents.Any(evt => evt.MoodRating.HasValue);

        _logger.LogInformation(
            "Resumen de bienestar calculado: promedio={Average:F2}, días rastreados={TrackedDays}, eventos con ánimo={HasEventMoodData}",
            summary.AverageMood,
            summary.TotalTrackedDays,
            summary.HasEventMoodData);

        return summary;
    }

    private static WellnessSummaryDto BuildSummary(
        int year,
        int month,
        DateTime monthStart,
        IReadOnlyCollection<DailyMoodEntry> moodEntries,
        IEnumerable<Event> monthlyEvents)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var totalTrackedDays = moodEntries.Count;
        var averageMood = totalTrackedDays > 0
            ? Math.Round(moodEntries.Average(entry => entry.MoodRating), 2)
            : 0d;

        var moodTrend = BuildMoodTrend(monthStart, daysInMonth, moodEntries);
        var distribution = BuildMoodDistribution(moodEntries, totalTrackedDays);
        var (positiveDays, neutralDays, negativeDays) = ClassifyMoodDays(moodEntries);
        var streaks = CalculateStreaks(monthStart, daysInMonth, moodEntries);
        var categoryImpacts = BuildCategoryImpacts(monthlyEvents);

        var summary = new WellnessSummaryDto
        {
            Year = year,
            Month = month,
            AverageMood = averageMood,
            MoodTrend = moodTrend,
            MoodDistribution = distribution,
            TotalTrackedDays = totalTrackedDays,
            PositiveDays = positiveDays,
            NeutralDays = neutralDays,
            NegativeDays = negativeDays,
            Streaks = streaks,
            CategoryImpacts = categoryImpacts,
            TrackingCoverage = daysInMonth > 0
                ? Math.Round((double)totalTrackedDays / daysInMonth, 4)
                : 0d,
            BestDay = MapToSnapshot(moodEntries
                .OrderByDescending(entry => entry.MoodRating)
                .ThenBy(entry => entry.EntryDate)
                .FirstOrDefault()),
            WorstDay = MapToSnapshot(moodEntries
                .OrderBy(entry => entry.MoodRating)
                .ThenBy(entry => entry.EntryDate)
                .FirstOrDefault())
        };

        return summary;
    }

    private static IReadOnlyCollection<MoodTrendPointDto> BuildMoodTrend(
        DateTime monthStart,
        int daysInMonth,
        IReadOnlyCollection<DailyMoodEntry> moodEntries)
    {
        var entriesByDate = moodEntries
            .GroupBy(entry => entry.EntryDate.Date)
            .ToDictionary(group => group.Key, group => group.ToList());

        var trend = new List<MoodTrendPointDto>(capacity: daysInMonth);

        for (var dayOffset = 0; dayOffset < daysInMonth; dayOffset++)
        {
            var date = monthStart.AddDays(dayOffset);
            var dateKey = date.Date;

            if (entriesByDate.TryGetValue(dateKey, out var dayEntries) && dayEntries.Count > 0)
            {
                var average = Math.Round(dayEntries.Average(entry => entry.MoodRating), 2);
                trend.Add(new MoodTrendPointDto
                {
                    Date = DateTime.SpecifyKind(dateKey, DateTimeKind.Utc),
                    AverageMood = average,
                    Entries = dayEntries.Count
                });
            }
            else
            {
                trend.Add(new MoodTrendPointDto
                {
                    Date = DateTime.SpecifyKind(dateKey, DateTimeKind.Utc),
                    AverageMood = null,
                    Entries = 0
                });
            }
        }

        return trend;
    }

    private static IReadOnlyCollection<MoodDistributionSliceDto> BuildMoodDistribution(
        IReadOnlyCollection<DailyMoodEntry> moodEntries,
        int totalTrackedDays)
    {
        const int minRating = 1;
        const int maxRating = 5;

        var slices = new List<MoodDistributionSliceDto>(capacity: maxRating - minRating + 1);

        for (var rating = minRating; rating <= maxRating; rating++)
        {
            var count = moodEntries.Count(entry => entry.MoodRating == rating);
            var percentage = totalTrackedDays > 0
                ? Math.Round((double)count / totalTrackedDays, 4)
                : 0d;

            slices.Add(new MoodDistributionSliceDto
            {
                MoodRating = rating,
                Count = count,
                Percentage = percentage
            });
        }

        return slices;
    }

    private static (int positive, int neutral, int negative) ClassifyMoodDays(
        IReadOnlyCollection<DailyMoodEntry> moodEntries)
    {
        var positiveDays = moodEntries.Count(entry => entry.MoodRating >= PositiveThreshold);
        var negativeDays = moodEntries.Count(entry => entry.MoodRating <= NegativeThreshold);
        var neutralDays = moodEntries.Count(entry => entry.MoodRating == 3);

        return (positiveDays, neutralDays, negativeDays);
    }

    private static MoodStreaksDto CalculateStreaks(
        DateTime monthStart,
        int daysInMonth,
        IReadOnlyCollection<DailyMoodEntry> moodEntries)
    {
        var entriesByDate = moodEntries
            .GroupBy(entry => entry.EntryDate.Date)
            .ToDictionary(group => group.Key, group => group.First());

        var currentPositive = 0;
        var longestPositive = 0;
        var currentNegative = 0;
        var longestNegative = 0;

        for (var dayOffset = 0; dayOffset < daysInMonth; dayOffset++)
        {
            var date = monthStart.AddDays(dayOffset).Date;

            if (entriesByDate.TryGetValue(date, out var entry))
            {
                if (entry.MoodRating >= PositiveThreshold)
                {
                    currentPositive++;
                }
                else
                {
                    currentPositive = 0;
                }

                if (entry.MoodRating <= NegativeThreshold)
                {
                    currentNegative++;
                }
                else
                {
                    currentNegative = 0;
                }
            }
            else
            {
                currentPositive = 0;
                currentNegative = 0;
            }

            longestPositive = Math.Max(longestPositive, currentPositive);
            longestNegative = Math.Max(longestNegative, currentNegative);
        }

        return new MoodStreaksDto
        {
            CurrentPositive = currentPositive,
            LongestPositive = longestPositive,
            CurrentNegative = currentNegative,
            LongestNegative = longestNegative
        };
    }

    private static IReadOnlyCollection<CategoryMoodImpactDto> BuildCategoryImpacts(IEnumerable<Event> monthlyEvents)
    {
        var eventsWithMood = monthlyEvents
            .Where(evt => evt.MoodRating.HasValue)
            .ToList();

        if (eventsWithMood.Count == 0)
        {
            return Array.Empty<CategoryMoodImpactDto>();
        }

        var grouped = eventsWithMood
            .GroupBy(evt => evt.EventCategoryId)
            .Select(group =>
            {
                var first = group.First();
                var averageMood = Math.Round(group.Average(evt => evt.MoodRating!.Value), 2);
                var positiveCount = group.Count(evt => evt.MoodRating >= PositiveThreshold);
                var negativeCount = group.Count(evt => evt.MoodRating <= NegativeThreshold);

                return new CategoryMoodImpactDto
                {
                    CategoryId = group.Key,
                    CategoryName = first.EventCategory?.Name ?? "Sin categoría",
                    CategoryColor = first.EventCategory?.Color,
                    AverageMood = averageMood,
                    EventCount = group.Count(),
                    PositiveCount = positiveCount,
                    NegativeCount = negativeCount
                };
            })
            .OrderByDescending(dto => dto.AverageMood)
            .ThenByDescending(dto => dto.EventCount)
            .ToList();

        return grouped;
    }

    private static MoodDaySnapshotDto? MapToSnapshot(DailyMoodEntry? entry)
    {
        if (entry == null)
        {
            return null;
        }

        return new MoodDaySnapshotDto
        {
            Date = DateTime.SpecifyKind(entry.EntryDate.Date, DateTimeKind.Utc),
            MoodRating = entry.MoodRating,
            Notes = entry.Notes
        };
    }
}
