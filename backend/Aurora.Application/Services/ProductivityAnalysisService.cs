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

    public async Task<ProductivityAnalysisDto> AnalyzeProductivityAsync(Guid userId, int periodDays = 30)
    {
        _logger.LogInformation("Iniciando análisis de productividad para usuario {UserId} con período de {Days} días", userId, periodDays);

        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-periodDays);

        // Obtener eventos del período
        var events = await _eventRepository.GetEventsByDateRangeAsync(userId, startDate, endDate);
        var eventsList = events.ToList();

        _logger.LogInformation("Se encontraron {Count} eventos para analizar", eventsList.Count);

        // Calcular productividad por hora
        var hourlyProductivity = CalculateHourlyProductivity(eventsList);

        // Calcular productividad por día de la semana
        var dailyProductivity = CalculateDailyProductivity(eventsList);

        // Identificar horas doradas
        var goldenHours = IdentifyGoldenHours(hourlyProductivity);

        // Identificar horas de baja energía
        var lowEnergyHours = IdentifyLowEnergyHours(hourlyProductivity);

        // Calcular productividad por categoría
        var categoryProductivity = CalculateCategoryProductivity(eventsList);

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
            AnalysisPeriodStart = startDate,
            AnalysisPeriodEnd = endDate,
            TotalEventsAnalyzed = eventsList.Count,
            TotalMoodRecordsAnalyzed = 0 // Por ahora, sin correlación con mood
        };
    }

    private List<HourlyProductivityDto> CalculateHourlyProductivity(List<Event> events)
    {
        var hourlyStats = new List<HourlyProductivityDto>();

        for (int hour = 0; hour < 24; hour++)
        {
            var eventsInHour = events.Where(e => e.StartDate.Hour == hour).ToList();

            if (!eventsInHour.Any())
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

            var completedEvents = eventsInHour.Count(e => e.MoodRating.HasValue || e.EndDate < DateTime.UtcNow);
            var completionRate = (double)completedEvents / eventsInHour.Count * 100;

            // Score de productividad basado en:
            // - Tasa de completitud (60%)
            // - Cantidad de eventos (20%)
            // - Dispersión horaria (20%)
            var activityScore = Math.Min(eventsInHour.Count * 10, 100) * 0.2;
            var completionScore = completionRate * 0.6;
            var consistencyScore = CalculateConsistencyScore(eventsInHour) * 0.2;

            var productivityScore = completionScore + activityScore + consistencyScore;

            hourlyStats.Add(new HourlyProductivityDto
            {
                Hour = hour,
                AverageMood = 3.5, // Por ahora valor neutral, integrar con DailyMoodEntry después
                EventsCompleted = completedEvents,
                TotalEvents = eventsInHour.Count,
                CompletionRate = completionRate,
                ProductivityScore = Math.Round(productivityScore, 2)
            });
        }

        return hourlyStats;
    }

    private double CalculateConsistencyScore(List<Event> events)
    {
        if (events.Count < 2) return 50;

        // Calcular varianza de días de la semana donde ocurren eventos
        var daysOfWeek = events.Select(e => (int)e.StartDate.DayOfWeek).Distinct().Count();
        
        // Mayor diversidad de días = mayor consistencia
        return Math.Min(daysOfWeek * 14.28, 100); // 100 / 7 días ≈ 14.28
    }

    private List<DailyProductivityDto> CalculateDailyProductivity(List<Event> events)
    {
        var dailyStats = new List<DailyProductivityDto>();
        var dayNames = new[] { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };

        for (int day = 0; day < 7; day++)
        {
            var eventsInDay = events.Where(e => (int)e.StartDate.DayOfWeek == day).ToList();

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

            var completedEvents = eventsInDay.Count(e => e.MoodRating.HasValue || e.EndDate < DateTime.UtcNow);
            var completionRate = (double)completedEvents / eventsInDay.Count * 100;
            var productivityScore = completionRate * 0.7 + Math.Min(eventsInDay.Count * 5, 100) * 0.3;

            dailyStats.Add(new DailyProductivityDto
            {
                DayOfWeek = day,
                DayName = dayNames[day],
                AverageMood = 3.5, // Valor neutral por ahora
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

    private List<CategoryProductivityDto> CalculateCategoryProductivity(List<Event> events)
    {
        var categoryStats = events
            .Where(e => e.EventCategory != null)
            .GroupBy(e => new { e.EventCategory!.Id, e.EventCategory.Name, e.EventCategory.Color })
            .Select(g =>
            {
                var categoryEvents = g.ToList();
                var completedEvents = categoryEvents.Count(e => e.MoodRating.HasValue || e.EndDate < DateTime.UtcNow);
                var completionRate = (double)completedEvents / categoryEvents.Count * 100;

                // Encontrar las mejores horas para esta categoría
                var hourDistribution = categoryEvents
                    .GroupBy(e => e.StartDate.Hour)
                    .Select(h => new { Hour = h.Key, Count = h.Count() })
                    .OrderByDescending(h => h.Count)
                    .Take(3)
                    .Select(h => h.Hour)
                    .ToList();

                // Encontrar el mejor día de la semana
                var bestDay = categoryEvents
                    .GroupBy(e => (int)e.StartDate.DayOfWeek)
                    .OrderByDescending(d => d.Count())
                    .FirstOrDefault()?.Key ?? 0;

                return new CategoryProductivityDto
                {
                    CategoryId = g.Key.Id,
                    CategoryName = g.Key.Name,
                    CategoryColor = g.Key.Color,
                    OptimalHours = hourDistribution,
                    AverageProductivityScore = Math.Round(completionRate * 0.8 + 20, 2),
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
            recommendations.Add(new ProductivityRecommendationDto
            {
                Title = "Aprovecha tus horas más productivas",
                Description = $"Tu mejor rendimiento es entre las {topHours.First().Hour:D2}:00 y {(topHours.Last().Hour + 1):D2}:00. Programa tareas importantes en estos horarios.",
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
            recommendations.Add(new ProductivityRecommendationDto
            {
                Title = "Evita tareas exigentes en horas de baja energía",
                Description = $"Tu rendimiento disminuye entre las {lowHours.First().Hour:D2}:00 y {(lowHours.Last().Hour + 1):D2}:00. Reserva estos momentos para tareas ligeras o descansos.",
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
}
