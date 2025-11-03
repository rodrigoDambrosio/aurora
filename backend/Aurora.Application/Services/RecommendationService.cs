using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Aurora.Application.Services;

/// <summary>
/// Servicio que genera recomendaciones heurísticas basadas en eventos y estados de ánimo históricos.
/// </summary>
public class RecommendationService : IRecommendationService
{
    private const int DefaultRecommendationCount = 6;
    private const int MinimumRecommendationCount = 5;
    private const int MaximumRecommendationCount = 10;
    private const int LookbackDays = 45;
    private const int LookaheadDays = 7;
    private const int DefaultDurationMinutes = 60;

    private readonly IDailyMoodRepository _dailyMoodRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRecommendationFeedbackRepository _feedbackRepository;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        IDailyMoodRepository dailyMoodRepository,
        IEventRepository eventRepository,
        IRecommendationFeedbackRepository feedbackRepository,
        ILogger<RecommendationService> logger)
    {
        _dailyMoodRepository = dailyMoodRepository ?? throw new ArgumentNullException(nameof(dailyMoodRepository));
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyCollection<RecommendationDto>> GetRecommendationsAsync(
        Guid? userId,
        RecommendationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var effectiveUserId = DevelopmentUserService.GetCurrentUserId(userId);
        var referenceDate = (request.ReferenceDate ?? DateTime.UtcNow).Date;
        var desiredAmount = ClampRecommendationCount(request.Limit);

        var lookbackStart = referenceDate.AddDays(-LookbackDays);
        var lookaheadLimit = referenceDate.AddDays(LookaheadDays);

        var (historicalEvents, upcomingEvents) = await FetchEventWindowsAsync(
            effectiveUserId,
            lookbackStart,
            referenceDate,
            lookaheadLimit,
            cancellationToken);

        var moodEntries = await FetchMoodWindowAsync(effectiveUserId, referenceDate, cancellationToken);

        var analytics = BuildAnalytics(historicalEvents, moodEntries);
        var recommendations = new List<RecommendationDto>(desiredAmount);

        recommendations.AddRange(BuildCategoryBasedRecommendations(referenceDate, analytics, upcomingEvents, desiredAmount));

        if (recommendations.Count < MinimumRecommendationCount)
        {
            recommendations.AddRange(BuildMoodTrendRecommendations(referenceDate, analytics, request.CurrentMood, desiredAmount));
        }

        if (recommendations.Count < MinimumRecommendationCount)
        {
            recommendations.AddRange(BuildRoutineRecommendations(referenceDate, analytics, upcomingEvents, desiredAmount));
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(CreateFallbackRecommendation(referenceDate));
        }

        var unique = recommendations
            .GroupBy(rec => rec.Id)
            .Select(group => group.First())
            .OrderByDescending(rec => rec.Confidence)
            .ThenBy(rec => rec.SuggestedStart)
            .Take(desiredAmount)
            .ToList();

        _logger.LogInformation(
            "Generated {Count} recommendations for user {UserId} on {Date}",
            unique.Count,
            effectiveUserId,
            referenceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        return unique;
    }

    public async Task RecordFeedbackAsync(
        Guid? userId,
        RecommendationFeedbackDto feedback,
        CancellationToken cancellationToken = default)
    {
        var effectiveUserId = DevelopmentUserService.GetCurrentUserId(userId);

        if (string.IsNullOrWhiteSpace(feedback.RecommendationId))
        {
            throw new ArgumentException("RecommendationId is required", nameof(feedback));
        }

        if (feedback.MoodAfter.HasValue && (feedback.MoodAfter < 1 || feedback.MoodAfter > 5))
        {
            throw new ArgumentException("MoodAfter debe estar entre 1 y 5", nameof(feedback));
        }

        var sanitizedId = feedback.RecommendationId.Trim();
        var sanitizedNotes = SanitizeNotes(feedback.Notes);
        var timestamp = (feedback.SubmittedAtUtc ?? DateTime.UtcNow).ToUniversalTime();

        var existing = await _feedbackRepository
            .GetByUserAndRecommendationAsync(effectiveUserId, sanitizedId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var entity = new RecommendationFeedback
            {
                RecommendationId = sanitizedId,
                Accepted = feedback.Accepted,
                Notes = sanitizedNotes,
                MoodAfter = feedback.MoodAfter,
                SubmittedAtUtc = timestamp,
                UserId = effectiveUserId
            };

            await _feedbackRepository.AddAsync(entity).ConfigureAwait(false);
        }
        else
        {
            existing.Accepted = feedback.Accepted;
            existing.Notes = sanitizedNotes;
            existing.MoodAfter = feedback.MoodAfter;
            existing.SubmittedAtUtc = timestamp;

            await _feedbackRepository.UpdateAsync(existing).ConfigureAwait(false);
        }

        await _feedbackRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Recommendation feedback persisted. User: {UserId}, Recommendation: {RecommendationId}, Accepted: {Accepted}, MoodAfter: {MoodAfter}",
            effectiveUserId,
            sanitizedId,
            feedback.Accepted,
            feedback.MoodAfter);
    }

    public async Task<RecommendationFeedbackSummaryDto> GetFeedbackSummaryAsync(
        Guid? userId,
        DateTime periodStartUtc,
        CancellationToken cancellationToken = default)
    {
        var effectiveUserId = DevelopmentUserService.GetCurrentUserId(userId);
        var normalizedStart = periodStartUtc.Kind == DateTimeKind.Utc
            ? periodStartUtc
            : periodStartUtc.ToUniversalTime();

        var nowUtc = DateTime.UtcNow;

        if (normalizedStart > nowUtc)
        {
            throw new ArgumentException("La fecha de inicio no puede estar en el futuro", nameof(periodStartUtc));
        }

        var feedbackEntries = await _feedbackRepository
            .GetFromDateAsync(effectiveUserId, normalizedStart, cancellationToken)
            .ConfigureAwait(false);

        if (feedbackEntries.Count == 0)
        {
            return new RecommendationFeedbackSummaryDto
            {
                TotalFeedback = 0,
                AcceptedCount = 0,
                RejectedCount = 0,
                AcceptanceRate = 0,
                AverageMoodAfter = null,
                PeriodStartUtc = normalizedStart,
                PeriodEndUtc = nowUtc
            };
        }

        var accepted = feedbackEntries.Count(entry => entry.Accepted);
        var rejected = feedbackEntries.Count - accepted;
        var acceptanceRate = Math.Round((double)accepted / feedbackEntries.Count * 100, 1);
        var moodValues = feedbackEntries.Where(entry => entry.MoodAfter.HasValue).Select(entry => entry.MoodAfter!.Value).ToList();
        var averageMood = moodValues.Count > 0 ? Math.Round(moodValues.Average(), 2) : (double?)null;

        return new RecommendationFeedbackSummaryDto
        {
            TotalFeedback = feedbackEntries.Count,
            AcceptedCount = accepted,
            RejectedCount = rejected,
            AcceptanceRate = acceptanceRate,
            AverageMoodAfter = averageMood,
            PeriodStartUtc = normalizedStart,
            PeriodEndUtc = nowUtc
        };
    }

    private static int ClampRecommendationCount(int? requested)
    {
        if (!requested.HasValue) return DefaultRecommendationCount;
        return Math.Clamp(requested.Value, MinimumRecommendationCount, MaximumRecommendationCount);
    }

    private async Task<(IReadOnlyList<Event> Historical, IReadOnlyList<Event> Upcoming)> FetchEventWindowsAsync(
        Guid userId,
        DateTime historyStart,
        DateTime historyEndExclusive,
        DateTime upcomingEnd,
        CancellationToken cancellationToken)
    {
        var historyTask = _eventRepository.GetEventsByDateRangeAsync(userId, historyStart, historyEndExclusive);
        var upcomingTask = _eventRepository.GetEventsByDateRangeAsync(userId, historyEndExclusive, upcomingEnd);

        await Task.WhenAll(historyTask, upcomingTask).ConfigureAwait(false);

        return (
            historyTask.Result.OrderBy(e => e.StartDate).ToList(),
            upcomingTask.Result.OrderBy(e => e.StartDate).ToList());
    }

    private async Task<IReadOnlyList<DailyMoodEntry>> FetchMoodWindowAsync(
        Guid userId,
        DateTime referenceDate,
        CancellationToken cancellationToken)
    {
        var currentMonth = await _dailyMoodRepository.GetMonthlyEntriesAsync(userId, referenceDate.Year, referenceDate.Month)
            .ConfigureAwait(false);

        var previousMonthDate = referenceDate.AddMonths(-1);
        var previousMonth = await _dailyMoodRepository.GetMonthlyEntriesAsync(userId, previousMonthDate.Year, previousMonthDate.Month)
            .ConfigureAwait(false);

        return currentMonth.Concat(previousMonth)
            .Where(entry => entry != null)
            .OrderBy(entry => entry.EntryDate)
            .ToList();
    }

    private static RecommendationDto CreateFallbackRecommendation(DateTime referenceDate)
    {
        return new RecommendationDto
        {
            Id = $"fallback-{referenceDate:yyyyMMdd}",
            Title = "Tomate un momento de respiro",
            Summary = "No tenemos suficiente información, probá con un paseo breve.",
            Reason = "Cuando no hay datos recientes es saludable agendar una pausa consciente.",
            RecommendationType = "wellbeing",
            SuggestedStart = referenceDate.AddHours(18),
            SuggestedDurationMinutes = 20,
            Confidence = 0.3,
            MoodImpact = "Reduce el estrés y ayuda a reconectar con el cuerpo"
        };
    }

    private static RecommendationAnalytics BuildAnalytics(
        IReadOnlyList<Event> events,
        IReadOnlyList<DailyMoodEntry> moods)
    {
        var byCategory = events
            .Where(evt => evt.EventCategoryId != Guid.Empty)
            .GroupBy(evt => evt.EventCategoryId)
            .Select(group => new CategorySnapshot
            {
                CategoryId = group.Key,
                CategoryName = group.First().EventCategory?.Name ?? "Actividad",
                CategoryColor = group.First().EventCategory?.Color,
                Events = group.ToList(),
                MoodAverage = group.Where(evt => evt.MoodRating.HasValue).DefaultIfEmpty()
                    .Average(evt => evt?.MoodRating ?? 0),
                PositiveShare = CalculatePositiveShare(group.ToList())
            })
            .OrderByDescending(snapshot => snapshot.MoodAverage)
            .ThenByDescending(snapshot => snapshot.Events.Count)
            .ToList();

        var moodTrend = moods
            .Select(entry => new MoodSnapshot(entry.EntryDate.Date, entry.MoodRating))
            .ToList();

        var recentMoodAverage = moodTrend.Any()
            ? Math.Round(moodTrend.TakeLast(7).Average(snapshot => snapshot.MoodRating), 2)
            : (double?)null;

        return new RecommendationAnalytics(byCategory, moodTrend, recentMoodAverage);
    }

    private static double CalculatePositiveShare(IReadOnlyCollection<Event> events)
    {
        if (events.Count == 0) return 0;
        var positives = events.Count(evt => evt.MoodRating.HasValue && evt.MoodRating.Value >= 4);
        return Math.Round((double)positives / events.Count, 2);
    }

    private static IEnumerable<RecommendationDto> BuildCategoryBasedRecommendations(
        DateTime referenceDate,
        RecommendationAnalytics analytics,
        IReadOnlyList<Event> upcomingEvents,
        int desiredAmount)
    {
        var suggestions = new List<RecommendationDto>();
        var occupiedSlots = upcomingEvents.Select(evt => (evt.StartDate, evt.EndDate)).ToList();

        foreach (var category in analytics.Categories.Take(desiredAmount))
        {
            if (!category.Events.Any()) continue;

            var peakEvent = category.Events
                .Where(evt => evt.MoodRating.HasValue)
                .OrderByDescending(evt => evt.MoodRating!.Value)
                .ThenBy(evt => evt.StartDate)
                .FirstOrDefault() ?? category.Events.First();

            var preferredTime = peakEvent.StartDate.TimeOfDay;
            var suggestedStart = FindNextAvailableSlot(referenceDate, preferredTime, occupiedSlots);
            var confidence = CalculateConfidence(category, analytics.RecentMoodAverage);

            var descriptor = category.MoodAverage >= 4.5 ? "excelentes" : category.MoodAverage >= 4 ? "muy buenos" : "positivos";

            var recommendation = new RecommendationDto
            {
                Id = $"category-{category.CategoryId}-{referenceDate:yyyyMMdd}",
                Title = $"Retomá {category.CategoryName}",
                Subtitle = "Basado en los mejores momentos recientes",
                Reason = $"Tus eventos de {category.CategoryName} tuvieron resultados {descriptor} ({category.MoodAverage:F1}/5).",
                Summary = "Repetir lo que funciona refuerza tu energía.",
                RecommendationType = "activity",
                SuggestedStart = suggestedStart,
                SuggestedDurationMinutes = (int)Math.Max(30, (peakEvent.EndDate - peakEvent.StartDate).TotalMinutes),
                Confidence = confidence,
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                MoodImpact = category.MoodAverage > 0 ? $"Impacto esperado: {category.MoodAverage:F1}/5" : null
            };

            suggestions.Add(recommendation);
        }

        return suggestions;
    }

    private static IEnumerable<RecommendationDto> BuildMoodTrendRecommendations(
        DateTime referenceDate,
        RecommendationAnalytics analytics,
        int? currentMood,
        int desiredAmount)
    {
        var suggestions = new List<RecommendationDto>();

        var moodSamples = analytics.MoodTrend.TakeLast(5).ToList();
        var hasDowntrend = moodSamples.Count >= 3 && moodSamples.Average(sample => sample.MoodRating) <= 3;
        var lastMood = moodSamples.LastOrDefault()?.MoodRating;
        var moodReference = currentMood ?? lastMood;

        if (hasDowntrend || (moodReference.HasValue && moodReference.Value <= 2))
        {
            suggestions.Add(new RecommendationDto
            {
                Id = $"mood-reset-{referenceDate:yyyyMMdd}",
                Title = "Micro-break restaurador",
                Subtitle = "Cuando la energía cae, 20 minutos hacen la diferencia",
                Reason = "Detectamos varios días desafiantes consecutivos. Una pausa activa corta ayuda a cortar la racha.",
                RecommendationType = "wellbeing",
                SuggestedStart = referenceDate.AddHours(17),
                SuggestedDurationMinutes = 20,
                Confidence = 0.65,
                MoodImpact = "Favorece la recuperación y libera tensión",
                Summary = "Respiración + estiramiento consciente"
            });
        }

        if (analytics.RecentMoodAverage.HasValue && analytics.RecentMoodAverage.Value >= 4)
        {
            suggestions.Add(new RecommendationDto
            {
                Id = $"mood-celebration-{referenceDate:yyyyMMdd}",
                Title = "Celebrá tus avances",
                Subtitle = "Reconocer lo que funciona también es parte del progreso",
                Reason = $"Tu promedio emocional de la última semana fue de {analytics.RecentMoodAverage.Value:F1}/5. Consolidemos esa buena racha.",
                RecommendationType = "reflection",
                SuggestedStart = referenceDate.AddDays(1).AddHours(21),
                SuggestedDurationMinutes = 15,
                Confidence = 0.55,
                MoodImpact = "Potencia la motivación",
                Summary = "Escribí 3 cosas que funcionaron esta semana"
            });
        }

        return suggestions;
    }

    private static IEnumerable<RecommendationDto> BuildRoutineRecommendations(
        DateTime referenceDate,
        RecommendationAnalytics analytics,
        IReadOnlyList<Event> upcomingEvents,
        int desiredAmount)
    {
        var suggestions = new List<RecommendationDto>();
        var occupiedSlots = upcomingEvents.Select(evt => (evt.StartDate, evt.EndDate)).ToList();

        var positiveMorningEvents = analytics.Categories
            .SelectMany(category => category.Events)
            .Where(evt => evt.MoodRating.HasValue && evt.MoodRating.Value >= 4 && evt.StartDate.TimeOfDay < TimeSpan.FromHours(12))
            .ToList();

        var morningAverage = positiveMorningEvents.Any()
            ? positiveMorningEvents.Average(evt => evt.MoodRating!.Value)
            : 0d;

        if (morningAverage >= 4)
        {
            var morningSlot = FindNextAvailableSlot(referenceDate, TimeSpan.FromHours(8), occupiedSlots);
            suggestions.Add(new RecommendationDto
            {
                Id = $"routine-morning-{referenceDate:yyyyMMdd}",
                Title = "Rutina energizante AM",
                Subtitle = "Anclá tu mañana a actividades que ya te funcionan",
                Reason = "Los días con mejor ánimo arrancaron temprano. Replicar esa estructura sostiene la energía.",
                RecommendationType = "routine",
                SuggestedStart = morningSlot,
                SuggestedDurationMinutes = 25,
                Confidence = 0.5,
                MoodImpact = "Incrementa claridad y foco",
                Summary = "Respiración + planificación rápida"
            });
        }

        if (!analytics.Categories.Any())
        {
            var eveningSlot = FindNextAvailableSlot(referenceDate, TimeSpan.FromHours(19), occupiedSlots);
            suggestions.Add(new RecommendationDto
            {
                Id = $"routine-evening-{referenceDate:yyyyMMdd}",
                Title = "Desconexión guiada",
                Subtitle = "Prepará tu descanso con intención",
                Reason = "Aunque no tenemos actividades previas, reservar un cierre del día ayuda a dormir mejor.",
                RecommendationType = "rest",
                SuggestedStart = eveningSlot,
                SuggestedDurationMinutes = 30,
                Confidence = 0.45,
                MoodImpact = "Favorece el sueño reparador",
                Summary = "Estiramiento suave + lectura breve"
            });
        }

        return suggestions;
    }

    private static DateTime FindNextAvailableSlot(
        DateTime referenceDate,
        TimeSpan preferredTime,
        IReadOnlyCollection<(DateTime Start, DateTime End)> occupiedSlots)
    {
        var candidate = referenceDate.Date.Add(preferredTime);
        if (candidate <= DateTime.UtcNow)
        {
            candidate = candidate.AddDays(1);
        }

        for (var offset = 0; offset < LookaheadDays; offset++)
        {
            var start = candidate.AddDays(offset);
            var end = start.AddMinutes(DefaultDurationMinutes);

            var overlaps = occupiedSlots.Any(slot => slot.Start < end && slot.End > start);
            if (!overlaps)
            {
                return start;
            }
        }

        return candidate;
    }

    private static double CalculateConfidence(CategorySnapshot snapshot, double? recentMoodAverage)
    {
        var baseConfidence = snapshot.MoodAverage > 0 ? Math.Clamp(snapshot.MoodAverage / 5d, 0.3, 0.95) : 0.4;
        var participationBonus = Math.Min(0.2, snapshot.Events.Count * 0.02);
        var moodBonus = recentMoodAverage.HasValue ? Math.Clamp((recentMoodAverage.Value - 3) * 0.05, -0.1, 0.1) : 0;

        var confidence = baseConfidence + participationBonus + moodBonus;
        return Math.Round(Math.Clamp(confidence, 0.2, 0.95), 2);
    }

    private static string? SanitizeNotes(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }

    private sealed record RecommendationAnalytics(
        IReadOnlyList<CategorySnapshot> Categories,
        IReadOnlyList<MoodSnapshot> MoodTrend,
        double? RecentMoodAverage);

    private sealed record CategorySnapshot
    {
        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string? CategoryColor { get; init; }
        public IReadOnlyList<Event> Events { get; init; } = Array.Empty<Event>();
        public double MoodAverage { get; init; }
        public double PositiveShare { get; init; }
    }

    private sealed record MoodSnapshot(DateTime Date, int MoodRating);
}
