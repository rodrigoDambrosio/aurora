using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Aurora.Application.Services;

/// <summary>
/// Servicio para análisis de productividad del usuario
/// </summary>
public class ProductivityAnalysisService : IProductivityAnalysisService
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<ProductivityAnalysisService> _logger;

    public ProductivityAnalysisService(
        IEventRepository eventRepository,
        ILogger<ProductivityAnalysisService> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<ProductivityAnalysisDto> AnalyzeProductivityAsync(Guid userId, int periodDays = 30, int? timezoneOffsetMinutes = null)
    {
        var offsetMinutes = timezoneOffsetMinutes ?? -180; // UTC-3 (Buenos Aires) como valor por defecto temporal
        var offsetTimeSpan = TimeSpan.FromMinutes(offsetMinutes);
        var userNow = DateTimeOffset.UtcNow.ToOffset(offsetTimeSpan);

        var localStart = new DateTimeOffset(userNow.Year, userNow.Month, userNow.Day, 0, 0, 0, offsetTimeSpan)
            .AddDays(-(periodDays - 1));
        var localEndExclusive = localStart.AddDays(periodDays);

        var startDateUtc = localStart.UtcDateTime;
        var endDateUtcExclusive = localEndExclusive.UtcDateTime;
        var endDateUtcInclusive = endDateUtcExclusive.AddTicks(-1);

        _logger.LogInformation(
            "Iniciando análisis de productividad para usuario {UserId} con período de {Days} días (offset {Offset}) entre {Start} y {End}",
            userId,
            periodDays,
            offsetMinutes,
            startDateUtc,
            endDateUtcInclusive);

        // Obtener eventos del período
        var eventsInPeriod = (await _eventRepository.GetEventsByDateRangeAsync(userId, startDateUtc, endDateUtcExclusive)).ToList();

        if (!eventsInPeriod.Any())
        {
            _logger.LogWarning(
                "No se encontraron eventos en el rango {Start} - {End}. Intentando obtener todos los eventos del usuario para validar.",
                startDateUtc,
                endDateUtcInclusive);

            var allUserEvents = await _eventRepository.GetEventsByUserIdAsync(userId);
            eventsInPeriod = allUserEvents
                .Where(e => e.StartDate < endDateUtcExclusive && e.EndDate > startDateUtc)
                .ToList();
        }

        var eventsList = eventsInPeriod
            .Where(e => IsWorkCategory(e.EventCategory))
            .ToList();

        if (!eventsList.Any())
        {
            _logger.LogInformation("Sin eventos de trabajo identificados en el período; usando todos los eventos disponibles para evitar un análisis vacío");
            eventsList = eventsInPeriod;
        }

        _logger.LogInformation("Se encontraron {Count} eventos de trabajo para analizar", eventsList.Count);

        var localizedEvents = eventsList
            .Select(e => new EventLocalSnapshot(
                e,
                ConvertToLocal(e.StartDate, offsetTimeSpan),
                ConvertToLocal(e.EndDate, offsetTimeSpan)))
            .Where(e => e.EndLocal > e.StartLocal)
            .ToList();

        // Calcular ventana de 7 días (excluyendo el día actual) para el heatmap horario
        var hourlyWindowEndLocal = new DateTimeOffset(userNow.Year, userNow.Month, userNow.Day, 0, 0, 0, offsetTimeSpan);
        var hourlyWindowStartLocal = hourlyWindowEndLocal.AddDays(-7);

        if (hourlyWindowStartLocal < localStart)
        {
            hourlyWindowStartLocal = localStart;
        }

        var hourlyEvents = localizedEvents
            .Where(e => e.StartLocal < hourlyWindowEndLocal && e.EndLocal > hourlyWindowStartLocal)
            .ToList();

        _logger.LogInformation(
            "Ventana horario local: {Start} - {End} (Eventos: {Count})",
            hourlyWindowStartLocal,
            hourlyWindowEndLocal,
            hourlyEvents.Count);

        // Calcular productividad por hora con la ventana acotada
        var hourlyProductivity = CalculateHourlyProductivity(hourlyEvents, hourlyWindowStartLocal, hourlyWindowEndLocal);

        // Calcular productividad por día de la semana
        var dailyProductivity = CalculateDailyProductivity(localizedEvents);

        // Identificar horas doradas
        var goldenHours = IdentifyGoldenHours(hourlyProductivity);

        // Identificar horas de baja energía
        var lowEnergyHours = IdentifyLowEnergyHours(hourlyProductivity);

        // Calcular productividad por categoría
        var categoryProductivity = CalculateCategoryProductivity(localizedEvents);

        // Generar recomendaciones
        var recommendations = GenerateRecommendations(hourlyProductivity, dailyProductivity, categoryProductivity);

        return new ProductivityAnalysisDto
        {
            HourlyProductivity = hourlyProductivity,
            DailyProductivity = dailyProductivity,
            GoldenHours = goldenHours,
            LowEnergyHours = lowEnergyHours,
            CategoryProductivity = categoryProductivity,
            Recommendations = recommendations,
            AnalysisPeriodStart = startDateUtc,
            AnalysisPeriodEnd = endDateUtcInclusive,
            TotalEventsAnalyzed = localizedEvents.Count,
            TotalMoodRecordsAnalyzed = localizedEvents.Count(e => e.Event.MoodRating.HasValue)
        };
    }

    private List<HourlyProductivityDto> CalculateHourlyProductivity(
        IReadOnlyList<EventLocalSnapshot> events,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd)
    {
        var accumulators = Enumerable.Range(0, 24)
            .Select(_ => new HourAccumulator())
            .ToArray();

        foreach (var snapshot in events)
        {
            var effectiveStart = snapshot.StartLocal < windowStart ? windowStart : snapshot.StartLocal;
            var effectiveEnd = snapshot.EndLocal > windowEnd ? windowEnd : snapshot.EndLocal;

            if (effectiveEnd <= effectiveStart)
            {
                continue;
            }

            var cursor = effectiveStart;

            while (cursor < effectiveEnd)
            {
                var hourStart = new DateTimeOffset(cursor.Year, cursor.Month, cursor.Day, cursor.Hour, 0, 0, cursor.Offset);
                var hourEnd = hourStart.AddHours(1);
                var segmentStart = cursor > hourStart ? cursor : hourStart;
                var segmentEnd = effectiveEnd < hourEnd ? effectiveEnd : hourEnd;
                var overlapMinutes = (segmentEnd - segmentStart).TotalMinutes;

                if (overlapMinutes <= 0)
                {
                    cursor = hourEnd;
                    continue;
                }

                var accumulator = accumulators[hourStart.Hour];
                accumulator.TotalMinutes += overlapMinutes;
                accumulator.EventIds.Add(snapshot.Event.Id);
                accumulator.ActivityDates.Add(hourStart.Date);

                if (snapshot.Event.MoodRating.HasValue)
                {
                    accumulator.MoodMinutes += overlapMinutes;
                    accumulator.MoodWeightedSum += snapshot.Event.MoodRating.Value * overlapMinutes;
                    accumulator.EventsWithMood.Add(snapshot.Event.Id);
                }

                cursor = hourEnd;
            }
        }

        var hourlyStats = new List<HourlyProductivityDto>(24);

        for (int hour = 0; hour < 24; hour++)
        {
            var accumulator = accumulators[hour];

            if (accumulator.TotalMinutes <= 0)
            {
                hourlyStats.Add(new HourlyProductivityDto
                {
                    Hour = hour,
                    AverageMood = 0,
                    EventsCompleted = 0,
                    TotalEvents = 0,
                    CompletionRate = 0,
                    ProductivityScore = 0
                });
                continue;
            }

            var averageMood = accumulator.MoodMinutes > 0
                ? Math.Round(accumulator.MoodWeightedSum / accumulator.MoodMinutes, 2)
                : 0;

            var feedbackCoverage = accumulator.TotalMinutes > 0
                ? accumulator.MoodMinutes / accumulator.TotalMinutes
                : 0;

            var moodScore = accumulator.MoodMinutes > 0 ? NormalizeMoodScore(averageMood) : 0;
            var consistencyScore = Math.Min(accumulator.ActivityDates.Count / 5.0, 1.0);
            var volumeScore = Math.Min(accumulator.EventIds.Count / 4.0, 1.0);
            var productivityScore = (moodScore * 0.6 + feedbackCoverage * 0.25 + consistencyScore * 0.1 + volumeScore * 0.05) * 100;

            hourlyStats.Add(new HourlyProductivityDto
            {
                Hour = hour,
                AverageMood = averageMood,
                EventsCompleted = accumulator.EventsWithMood.Count,
                TotalEvents = accumulator.EventIds.Count,
                CompletionRate = Math.Round(feedbackCoverage * 100, 2),
                ProductivityScore = Math.Round(productivityScore, 2)
            });
        }

        return hourlyStats;
    }

    private List<DailyProductivityDto> CalculateDailyProductivity(IReadOnlyList<EventLocalSnapshot> events)
    {
        var dailyStats = new List<DailyProductivityDto>();
        var dayNames = new[] { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };

        for (int day = 0; day < 7; day++)
        {
            var eventsInDay = events.Where(e => (int)e.StartLocal.DayOfWeek == day).ToList();

            if (!eventsInDay.Any())
            {
                dailyStats.Add(new DailyProductivityDto
                {
                    DayOfWeek = day,
                    DayName = dayNames[day],
                    AverageMood = 0,
                    ProductivityScore = 0,
                    TotalEvents = 0
                });
                continue;
            }

            var feedbackEvents = eventsInDay.Where(e => e.Event.MoodRating.HasValue).ToList();
            var averageMood = feedbackEvents.Any() ? feedbackEvents.Average(e => e.Event.MoodRating!.Value) : 0;
            var feedbackCoverage = feedbackEvents.Any()
                ? (double)feedbackEvents.Count / eventsInDay.Count
                : 0;

            var moodScore = feedbackEvents.Any() ? NormalizeMoodScore(averageMood) : 0;
            var volumeScore = Math.Min(eventsInDay.Count / 6.0, 1.0);
            var productivityScore = (moodScore * 0.55 + feedbackCoverage * 0.3 + volumeScore * 0.15) * 100;

            dailyStats.Add(new DailyProductivityDto
            {
                DayOfWeek = day,
                DayName = dayNames[day],
                AverageMood = feedbackEvents.Any() ? Math.Round(averageMood, 2) : 0,
                ProductivityScore = Math.Round(productivityScore, 2),
                TotalEvents = eventsInDay.Count
            });
        }

        return dailyStats;
    }

    private List<GoldenHourDto> IdentifyGoldenHours(List<HourlyProductivityDto> hourlyStats)
    {
        var goldenHours = new List<GoldenHourDto>();
        var threshold = 70.0; // Score mínimo para ser considerado "hora dorada"

        var highProductivityHours = hourlyStats
            .Where(h => h.ProductivityScore >= threshold && h.TotalEvents > 0)
            .OrderByDescending(h => h.ProductivityScore)
            .ToList();

        // Agrupar horas consecutivas
        var groups = new List<List<HourlyProductivityDto>>();
        List<HourlyProductivityDto>? currentGroup = null;

        foreach (var hour in highProductivityHours.OrderBy(h => h.Hour))
        {
            if (currentGroup == null || hour.Hour != currentGroup.Last().Hour + 1)
            {
                currentGroup = new List<HourlyProductivityDto> { hour };
                groups.Add(currentGroup);
            }
            else
            {
                currentGroup.Add(hour);
            }
        }

        foreach (var group in groups)
        {
            var startHour = group.First().Hour;
            var endHour = group.Last().Hour;
            var avgScore = group.Average(h => h.ProductivityScore);

            var timeDescription = GetTimeOfDayDescription(startHour);

            goldenHours.Add(new GoldenHourDto
            {
                StartHour = startHour,
                EndHour = endHour + 1, // Incluir hora completa
                AverageProductivityScore = Math.Round(avgScore, 2),
                Description = $"{timeDescription}: {startHour:D2}:00 - {(endHour + 1):D2}:00",
                ApplicableDays = null // Aplica a todos los días por defecto
            });
        }

        return goldenHours;
    }

    private List<LowEnergyHourDto> IdentifyLowEnergyHours(List<HourlyProductivityDto> hourlyStats)
    {
        var lowEnergyHours = new List<LowEnergyHourDto>();
        var threshold = 30.0; // Score máximo para ser considerado "baja energía"

        var lowProductivityHours = hourlyStats
            .Where(h => h.ProductivityScore > 0 && h.ProductivityScore <= threshold && h.TotalEvents > 0)
            .OrderBy(h => h.ProductivityScore)
            .ToList();

        // Agrupar horas consecutivas
        var groups = new List<List<HourlyProductivityDto>>();
        List<HourlyProductivityDto>? currentGroup = null;

        foreach (var hour in lowProductivityHours.OrderBy(h => h.Hour))
        {
            if (currentGroup == null || hour.Hour != currentGroup.Last().Hour + 1)
            {
                currentGroup = new List<HourlyProductivityDto> { hour };
                groups.Add(currentGroup);
            }
            else
            {
                currentGroup.Add(hour);
            }
        }

        foreach (var group in groups)
        {
            var startHour = group.First().Hour;
            var endHour = group.Last().Hour;
            var avgScore = group.Average(h => h.ProductivityScore);

            var timeDescription = GetTimeOfDayDescription(startHour);

            lowEnergyHours.Add(new LowEnergyHourDto
            {
                StartHour = startHour,
                EndHour = endHour + 1,
                AverageProductivityScore = Math.Round(avgScore, 2),
                Description = $"{timeDescription}: {startHour:D2}:00 - {(endHour + 1):D2}:00"
            });
        }

        return lowEnergyHours;
    }

    private List<CategoryProductivityDto> CalculateCategoryProductivity(IReadOnlyList<EventLocalSnapshot> events)
    {
        var categoryStats = events
            .Where(e => e.Event.EventCategory != null)
            .GroupBy(e => new { e.Event.EventCategory!.Id, e.Event.EventCategory.Name, e.Event.EventCategory.Color })
            .Select(g =>
            {
                var categoryEvents = g.ToList();
                var feedbackEvents = categoryEvents.Where(e => e.Event.MoodRating.HasValue).ToList();
                var averageMood = feedbackEvents.Any() ? feedbackEvents.Average(e => e.Event.MoodRating!.Value) : 0;
                var feedbackCoverage = feedbackEvents.Any()
                    ? (double)feedbackEvents.Count / categoryEvents.Count
                    : 0;

                var moodScore = feedbackEvents.Any() ? NormalizeMoodScore(averageMood) : 0;
                var productivityScore = (moodScore * 0.6 + feedbackCoverage * 0.4) * 100;

                // Encontrar las mejores horas para esta categoría
                var hourDistribution = categoryEvents
                    .GroupBy(e => e.StartLocal.Hour)
                    .Select(h => new { Hour = h.Key, Count = h.Count() })
                    .OrderByDescending(h => h.Count)
                    .Take(3)
                    .Select(h => h.Hour)
                    .ToList();

                // Encontrar el mejor día de la semana
                var bestDay = categoryEvents
                    .GroupBy(e => (int)e.StartLocal.DayOfWeek)
                    .OrderByDescending(d => d.Count())
                    .FirstOrDefault()?.Key ?? 0;

                return new CategoryProductivityDto
                {
                    CategoryId = g.Key.Id,
                    CategoryName = g.Key.Name,
                    CategoryColor = g.Key.Color,
                    OptimalHours = hourDistribution,
                    AverageProductivityScore = Math.Round(productivityScore, 2),
                    BestDayOfWeek = bestDay
                };
            })
            .ToList();

        return categoryStats;
    }

    private List<ProductivityRecommendationDto> GenerateRecommendations(
        List<HourlyProductivityDto> hourlyStats,
        List<DailyProductivityDto> dailyStats,
        List<CategoryProductivityDto> categoryStats)
    {
        var recommendations = new List<ProductivityRecommendationDto>();

        // Recomendación 1: Aprovechar horas doradas
        var topHours = hourlyStats
            .Where(h => h.ProductivityScore >= 70 && h.TotalEvents > 0)
            .OrderByDescending(h => h.ProductivityScore)
            .Take(3)
            .ToList();

        if (topHours.Any())
        {
            var moodText = topHours.Any(h => h.AverageMood > 0)
                ? $" (ánimo promedio {topHours.Average(h => h.AverageMood):F1}/5)"
                : string.Empty;

            recommendations.Add(new ProductivityRecommendationDto
            {
                Title = "Aprovecha tus horas más productivas",
                Description = $"Tu mejor rendimiento es entre las {topHours.First().Hour:D2}:00 y {(topHours.Last().Hour + 1):D2}:00{moodText}. Programa tareas importantes en estos horarios.",
                Priority = 5,
                Type = "golden-hours",
                SuggestedHours = topHours.Select(h => h.Hour).ToList()
            });
        }

        // Recomendación 2: Evitar sobrecarga en horas de baja energía
        var lowHours = hourlyStats
            .Where(h => h.ProductivityScore > 0 && h.ProductivityScore < 40 && h.TotalEvents > 0)
            .ToList();

        if (lowHours.Any())
        {
            var moodText = lowHours.Any(h => h.AverageMood > 0)
                ? $" (ánimo promedio {lowHours.Average(h => h.AverageMood):F1}/5)"
                : string.Empty;

            recommendations.Add(new ProductivityRecommendationDto
            {
                Title = "Evita tareas exigentes en horas de baja energía",
                Description = $"Tu rendimiento disminuye entre las {lowHours.First().Hour:D2}:00 y {(lowHours.Last().Hour + 1):D2}:00{moodText}. Reserva estos momentos para tareas ligeras o descansos.",
                Priority = 4,
                Type = "low-energy-warning",
                SuggestedHours = lowHours.Select(h => h.Hour).ToList()
            });
        }

        // Recomendación 3: Mejor día para actividades específicas
        var bestDay = dailyStats.OrderByDescending(d => d.ProductivityScore).FirstOrDefault();
        if (bestDay != null && bestDay.TotalEvents > 0)
        {
            recommendations.Add(new ProductivityRecommendationDto
            {
                Title = $"Los {bestDay.DayName.ToLower()} son tus días más productivos",
                Description = $"Considera programar tus actividades más importantes los días {bestDay.DayName.ToLower()}, donde tu productividad promedio es de {bestDay.ProductivityScore:F1}%.",
                Priority = 3,
                Type = "best-day"
            });
        }

        // Recomendación 4: Optimización por categoría
        foreach (var category in categoryStats.OrderByDescending(c => c.AverageProductivityScore).Take(2))
        {
            if (category.OptimalHours.Any())
            {
                recommendations.Add(new ProductivityRecommendationDto
                {
                    Title = $"Horario óptimo para {category.CategoryName}",
                    Description = $"Tus actividades de '{category.CategoryName}' funcionan mejor alrededor de las {category.OptimalHours.First():D2}:00. Considera programarlas en estos horarios.",
                    Priority = 2,
                    Type = "category-optimization",
                    AffectedCategories = new List<string> { category.CategoryName },
                    SuggestedHours = category.OptimalHours
                });
            }
        }

        return recommendations.OrderByDescending(r => r.Priority).ToList();
    }

    private static DateTimeOffset ConvertToLocal(DateTime dateTime, TimeSpan offset)
    {
        var utcDateTime = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };

        return new DateTimeOffset(utcDateTime, TimeSpan.Zero).ToOffset(offset);
    }

    private sealed record EventLocalSnapshot(Event Event, DateTimeOffset StartLocal, DateTimeOffset EndLocal);

    private sealed class HourAccumulator
    {
        public HashSet<Guid> EventIds { get; } = new();
        public HashSet<Guid> EventsWithMood { get; } = new();
        public HashSet<DateTime> ActivityDates { get; } = new();
        public double TotalMinutes { get; set; }
        public double MoodMinutes { get; set; }
        public double MoodWeightedSum { get; set; }
    }

    private string GetTimeOfDayDescription(int hour)
    {
        return hour switch
        {
            >= 5 and < 12 => "Mañana",
            >= 12 and < 14 => "Mediodía",
            >= 14 and < 20 => "Tarde",
            >= 20 and < 24 => "Noche",
            _ => "Madrugada"
        };
    }

    private static bool IsWorkCategory(EventCategory? category)
    {
        if (category == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(category.Icon))
        {
            var normalizedIcon = category.Icon.Trim().ToLowerInvariant();

            if (normalizedIcon is "work" or "briefcase" or "briefcase-business" or "business" or "office" or "suitcase" or "laptop")
            {
                return true;
            }

            if (normalizedIcon.Contains("work", StringComparison.Ordinal) ||
                normalizedIcon.Contains("briefcase", StringComparison.Ordinal) ||
                normalizedIcon.Contains("business", StringComparison.Ordinal) ||
                normalizedIcon.Contains("office", StringComparison.Ordinal))
            {
                return true;
            }

            if (category.Icon.Contains("💼", StringComparison.Ordinal) ||
                category.Icon.Contains("👔", StringComparison.Ordinal))
            {
                return true;
            }
        }

        var normalizedName = category.Name?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalizedName))
        {
            return false;
        }

        return normalizedName.Contains("trabajo") ||
               normalizedName.Contains("work") ||
               normalizedName.Contains("laboral");
    }

    private static double NormalizeMoodScore(double mood)
    {
        if (double.IsNaN(mood) || double.IsInfinity(mood))
        {
            return 0;
        }

        return Math.Clamp((mood - 1) / 4.0, 0, 1);
    }
}
