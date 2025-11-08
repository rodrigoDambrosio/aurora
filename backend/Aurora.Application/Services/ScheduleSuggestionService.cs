using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;
using Aurora.Domain.Enums;

namespace Aurora.Application.Services;

/// <summary>
/// Servicio para generar y gestionar sugerencias de reorganización del calendario
/// </summary>
public class ScheduleSuggestionService : IScheduleSuggestionService
{
    private readonly IScheduleSuggestionRepository _suggestionRepository;
    private readonly IEventRepository _eventRepository;

    public ScheduleSuggestionService(
        IScheduleSuggestionRepository suggestionRepository,
        IEventRepository eventRepository)
    {
        _suggestionRepository = suggestionRepository;
        _eventRepository = eventRepository;
    }

    public async Task<IEnumerable<ScheduleSuggestionDto>> GenerateSuggestionsAsync(Guid userId)
    {
        // Expirar sugerencias antiguas (más de 7 días)
        await _suggestionRepository.ExpireOldSuggestionsAsync(userId, DateTime.UtcNow.AddDays(-7));

        var suggestions = new List<ScheduleSuggestion>();

        // 1. Detectar conflictos de horario
        var conflicts = await DetectScheduleConflictsAsync(userId);
        suggestions.AddRange(conflicts);

        // 2. Identificar patrones problemáticos
        var patterns = await IdentifyProblematicPatternsAsync(userId);
        suggestions.AddRange(patterns);

        // 3. Sugerir optimizaciones de distribución
        var optimizations = await SuggestDistributionOptimizationsAsync(userId);
        suggestions.AddRange(optimizations);

        // Guardar sugerencias en la base de datos
        foreach (var suggestion in suggestions)
        {
            await _suggestionRepository.CreateAsync(suggestion);
        }

        return suggestions.Select(MapToDto);
    }

    public async Task<IEnumerable<ScheduleSuggestionDto>> GetPendingSuggestionsAsync(Guid userId)
    {
        var suggestions = await _suggestionRepository.GetPendingSuggestionsByUserIdAsync(userId);
        return suggestions.Select(MapToDto);
    }

    public async Task<ScheduleSuggestionDto> RespondToSuggestionAsync(
        Guid suggestionId, 
        RespondToSuggestionDto response, 
        Guid userId)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(suggestionId);
        
        if (suggestion == null)
            throw new KeyNotFoundException("Sugerencia no encontrada");

        if (suggestion.UserId != userId)
            throw new UnauthorizedAccessException("No tienes permiso para responder a esta sugerencia");

        suggestion.Status = response.Status;
        suggestion.RespondedAt = DateTime.UtcNow;
        
        // Guardar el comentario del usuario en metadata si existe
        if (!string.IsNullOrWhiteSpace(response.UserComment))
        {
            suggestion.Metadata = System.Text.Json.JsonSerializer.Serialize(new 
            { 
                userComment = response.UserComment,
                respondedAt = DateTime.UtcNow
            });
        }

        // Si aceptó la sugerencia, aplicar el cambio
        if (response.Status == SuggestionStatus.Accepted)
        {
            await ApplySuggestionAsync(suggestion);
        }

        await _suggestionRepository.UpdateAsync(suggestion);

        return MapToDto(suggestion);
    }

    #region Private Methods

    private async Task<List<ScheduleSuggestion>> DetectScheduleConflictsAsync(Guid userId)
    {
        var suggestions = new List<ScheduleSuggestion>();
        var nextWeek = DateTime.UtcNow.AddDays(7);
        var events = await _eventRepository.GetEventsByDateRangeAsync(userId, DateTime.UtcNow, nextWeek);

        // Agrupar eventos por día
        var eventsByDay = events
            .GroupBy(e => e.StartDate.Date)
            .Where(g => g.Count() > 1);

        foreach (var dayGroup in eventsByDay)
        {
            var dayEvents = dayGroup.OrderBy(e => e.StartDate).ToList();

            for (int i = 0; i < dayEvents.Count - 1; i++)
            {
                var current = dayEvents[i];
                var next = dayEvents[i + 1];

                // Detectar solapamiento
                if (current.EndDate > next.StartDate)
                {
                    suggestions.Add(new ScheduleSuggestion
                    {
                        UserId = userId,
                        EventId = next.Id,
                        Type = SuggestionType.ResolveConflict,
                        Description = $"Conflicto detectado: '{next.Title}' se solapa con '{current.Title}'",
                        Reason = $"El evento comienza a las {next.StartDate:HH:mm} pero '{current.Title}' termina a las {current.EndDate:HH:mm}",
                        Priority = 5, // Alta prioridad
                        SuggestedDateTime = current.EndDate.AddMinutes(15), // Mover 15 minutos después del evento anterior
                        ConfidenceScore = 95
                    });
                }
                // Detectar eventos muy juntos (menos de 15 minutos)
                else if ((next.StartDate - current.EndDate).TotalMinutes < 15)
                {
                    suggestions.Add(new ScheduleSuggestion
                    {
                        UserId = userId,
                        EventId = next.Id,
                        Type = SuggestionType.SuggestBreak,
                        Description = $"Muy poco tiempo entre '{current.Title}' y '{next.Title}'",
                        Reason = "Se recomienda al menos 15 minutos de descanso entre eventos",
                        Priority = 3,
                        SuggestedDateTime = current.EndDate.AddMinutes(15),
                        ConfidenceScore = 80
                    });
                }
            }
        }

        return suggestions;
    }

    private async Task<List<ScheduleSuggestion>> IdentifyProblematicPatternsAsync(Guid userId)
    {
        var suggestions = new List<ScheduleSuggestion>();
        var nextWeek = DateTime.UtcNow.AddDays(7);
        var events = await _eventRepository.GetEventsByDateRangeAsync(userId, DateTime.UtcNow, nextWeek);

        // Agrupar eventos por día
        var eventsByDay = events.GroupBy(e => e.StartDate.Date);

        foreach (var dayGroup in eventsByDay)
        {
            var dayEvents = dayGroup.OrderBy(e => e.StartDate).ToList();

            // Detectar días sobrecargados (más de 8 horas de eventos)
            var totalHours = dayEvents.Sum(e => (e.EndDate - e.StartDate).TotalHours);
            if (totalHours > 8)
            {
                suggestions.Add(new ScheduleSuggestion
                {
                    UserId = userId,
                    Type = SuggestionType.PatternAlert,
                    Description = $"Día sobrecargado: {dayGroup.Key:dd/MM/yyyy}",
                    Reason = $"Tienes {totalHours:F1} horas de eventos programadas. Se recomienda redistribuir algunas tareas",
                    Priority = 4,
                    ConfidenceScore = 85
                });
            }

            // Detectar falta de descansos (más de 4 horas seguidas)
            for (int i = 0; i < dayEvents.Count - 1; i++)
            {
                var timeSpan = dayEvents[i + 1].StartDate - dayEvents[i].StartDate;
                if (timeSpan.TotalHours > 4 && (dayEvents[i + 1].StartDate - dayEvents[i].EndDate).TotalMinutes < 30)
                {
                    suggestions.Add(new ScheduleSuggestion
                    {
                        UserId = userId,
                        Type = SuggestionType.SuggestBreak,
                        Description = "Período largo sin descansos significativos",
                        Reason = "Se detectó un bloque de más de 4 horas sin descanso adecuado",
                        Priority = 3,
                        SuggestedDateTime = dayEvents[i].EndDate.AddHours(2),
                        ConfidenceScore = 75
                    });
                    break; // Solo una sugerencia por día
                }
            }
        }

        return suggestions;
    }

    private async Task<List<ScheduleSuggestion>> SuggestDistributionOptimizationsAsync(Guid userId)
    {
        var suggestions = new List<ScheduleSuggestion>();
        var nextTwoWeeks = DateTime.UtcNow.AddDays(14);
        var events = await _eventRepository.GetEventsByDateRangeAsync(userId, DateTime.UtcNow, nextTwoWeeks);

        // Agrupar por semana
        var eventsByWeek = events.GroupBy(e => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            e.StartDate, 
            System.Globalization.CalendarWeekRule.FirstFourDayWeek, 
            DayOfWeek.Monday));

        foreach (var weekGroup in eventsByWeek)
        {
            var weekEvents = weekGroup.ToList();
            var eventsByDay = weekEvents.GroupBy(e => e.StartDate.DayOfWeek);

            // Detectar distribución desigual
            var daysWithEvents = eventsByDay.Count();
            var avgEventsPerDay = weekEvents.Count / (double)daysWithEvents;

            var overloadedDays = eventsByDay.Where(g => g.Count() > avgEventsPerDay * 1.5).ToList();
            var lightDays = eventsByDay.Where(g => g.Count() < avgEventsPerDay * 0.5).ToList();

            if (overloadedDays.Any() && lightDays.Any())
            {
                var overloadedDay = overloadedDays.First();
                var lightDay = lightDays.First();

                suggestions.Add(new ScheduleSuggestion
                {
                    UserId = userId,
                    Type = SuggestionType.OptimizeDistribution,
                    Description = "Distribución desigual de eventos en la semana",
                    Reason = $"Tienes {overloadedDay.Count()} eventos el {overloadedDay.Key} pero solo {lightDay.Count()} el {lightDay.Key}",
                    Priority = 2,
                    ConfidenceScore = 70
                });
            }
        }

        return suggestions;
    }

    private async Task ApplySuggestionAsync(ScheduleSuggestion suggestion)
    {
        // Si la sugerencia es mover un evento y tiene una fecha sugerida
        if (suggestion.Type == SuggestionType.MoveEvent && 
            suggestion.EventId.HasValue && 
            suggestion.SuggestedDateTime.HasValue)
        {
            var eventToMove = await _eventRepository.GetByIdAsync(suggestion.EventId.Value);
            if (eventToMove != null)
            {
                var duration = eventToMove.EndDate - eventToMove.StartDate;
                eventToMove.StartDate = suggestion.SuggestedDateTime.Value;
                eventToMove.EndDate = suggestion.SuggestedDateTime.Value + duration;
                await _eventRepository.UpdateAsync(eventToMove);
            }
        }
        
        // Otras acciones según el tipo de sugerencia pueden implementarse aquí
    }

    private static ScheduleSuggestionDto MapToDto(ScheduleSuggestion suggestion)
    {
        return new ScheduleSuggestionDto
        {
            Id = suggestion.Id,
            UserId = suggestion.UserId,
            EventId = suggestion.EventId,
            EventTitle = suggestion.Event?.Title,
            Type = suggestion.Type,
            TypeDescription = GetTypeDescription(suggestion.Type),
            Description = suggestion.Description,
            Reason = suggestion.Reason,
            Priority = suggestion.Priority,
            SuggestedDateTime = suggestion.SuggestedDateTime,
            Status = suggestion.Status,
            StatusDescription = GetStatusDescription(suggestion.Status),
            RespondedAt = suggestion.RespondedAt,
            ConfidenceScore = suggestion.ConfidenceScore,
            CreatedAt = suggestion.CreatedAt
        };
    }

    private static string GetTypeDescription(SuggestionType type) => type switch
    {
        SuggestionType.MoveEvent => "Mover evento",
        SuggestionType.ResolveConflict => "Resolver conflicto",
        SuggestionType.OptimizeDistribution => "Optimizar distribución",
        SuggestionType.PatternAlert => "Alerta de patrón",
        SuggestionType.SuggestBreak => "Sugerir descanso",
        SuggestionType.GeneralReorganization => "Reorganización general",
        _ => "Desconocido"
    };

    private static string GetStatusDescription(SuggestionStatus status) => status switch
    {
        SuggestionStatus.Pending => "Pendiente",
        SuggestionStatus.Accepted => "Aceptada",
        SuggestionStatus.Rejected => "Rechazada",
        SuggestionStatus.Postponed => "Pospuesta",
        SuggestionStatus.Expired => "Expirada",
        _ => "Desconocido"
    };

    #endregion
}
