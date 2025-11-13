using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aurora.Application.Services;

/// <summary>
/// Servicio para generar sugerencias personalizadas de autocuidado
/// </summary>
public class SelfCareService : ISelfCareService
{
    private readonly IEventRepository _eventRepository;
    private readonly IAIValidationService _aiService;
    private readonly ILogger<SelfCareService> _logger;

    // Cache de sugerencias usadas recientemente para evitar repetición
    private readonly Dictionary<Guid, List<(string Id, DateTime UsedAt)>> _recentSuggestions = new();

    public SelfCareService(
        IEventRepository eventRepository,
        IAIValidationService aiService,
        ILogger<SelfCareService> logger)
    {
        _eventRepository = eventRepository;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<IEnumerable<SelfCareRecommendationDto>> GetRecommendationsAsync(
        Guid userId,
        SelfCareRequestDto request)
    {
        try
        {
            _logger.LogInformation("Generando sugerencias de autocuidado para usuario {UserId}", userId);

            // Obtener contexto del usuario
            var now = DateTime.UtcNow;
            var last30Days = now.AddDays(-30);
            var events = await _eventRepository.GetEventsByDateRangeAsync(userId, last30Days, now);
            var eventsList = events.ToList();

            // Obtener hora actual y día de la semana
            var currentHour = DateTime.Now.Hour;
            var currentDayOfWeek = DateTime.Now.DayOfWeek;

            // Generar pool de sugerencias basado en contexto
            var allSuggestions = GenerateContextualSuggestions(
                currentHour,
                currentDayOfWeek,
                request.CurrentMood,
                eventsList);

            // Calcular scoring para cada sugerencia
            var scoredSuggestions = allSuggestions
                .Select(s => CalculateScore(s, userId, eventsList))
                .OrderByDescending(s => s.ConfidenceScore)
                .ToList();

            // Filtrar sugerencias usadas recientemente (< 48h)
            var filtered = FilterRecentSuggestions(userId, scoredSuggestions);

            // Tomar top 3 tradicionales
            var traditionalRecommendations = filtered
                .Take(3)
                .ToList();

            // Intentar generar 2 sugerencias con IA
            var aiRecommendations = new List<SelfCareRecommendationDto>();
            try
            {
                _logger.LogInformation("Intentando generar sugerencias con IA...");
                var aiSuggestions = await GenerateAISuggestionsAsync(userId, eventsList, currentHour, currentDayOfWeek, request.CurrentMood);
                aiRecommendations.AddRange(aiSuggestions.Take(2));
                _logger.LogInformation("IA generó {Count} sugerencias exitosamente", aiRecommendations.Count);
            }
            catch (Exception aiEx)
            {
                _logger.LogWarning(aiEx, "No se pudieron generar sugerencias con IA, usando solo tradicionales");
            }

            // Combinar: 3 tradicionales + hasta 2 de IA
            var recommendations = new List<SelfCareRecommendationDto>();
            recommendations.AddRange(traditionalRecommendations);
            recommendations.AddRange(aiRecommendations);

            // Si no hay suficientes, completar con más tradicionales
            if (recommendations.Count < request.Count)
            {
                var needed = request.Count - recommendations.Count;
                var additional = filtered
                    .Skip(traditionalRecommendations.Count)
                    .Take(needed);
                recommendations.AddRange(additional);
            }

            _logger.LogInformation(
                "Generadas {Count} sugerencias: {Traditional} tradicionales + {AI} con IA",
                recommendations.Count,
                traditionalRecommendations.Count,
                aiRecommendations.Count);

            return recommendations.Take(request.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando sugerencias de autocuidado");
            // Fallback a sugerencias genéricas
            return GetGenericRecommendations(request.Count);
        }
    }

    public Task RegisterFeedbackAsync(Guid userId, SelfCareFeedbackDto feedback)
    {
        _logger.LogInformation(
            "Feedback de autocuidado: Usuario {UserId}, Sugerencia {Id}, Acción {Action}",
            userId, feedback.RecommendationId, feedback.Action);

        // Registrar uso de la sugerencia para evitar repetición
        if (!_recentSuggestions.ContainsKey(userId))
        {
            _recentSuggestions[userId] = new List<(string, DateTime)>();
        }

        _recentSuggestions[userId].Add((feedback.RecommendationId, feedback.Timestamp));

        // Limpiar sugerencias antiguas (> 48h)
        var cutoff = DateTime.UtcNow.AddHours(-48);
        _recentSuggestions[userId] = _recentSuggestions[userId]
            .Where(s => s.UsedAt > cutoff)
            .ToList();

        // TODO: Guardar feedback en base de datos para aprendizaje futuro
        return Task.CompletedTask;
    }

    public IEnumerable<SelfCareRecommendationDto> GetGenericRecommendations(int count = 5)
    {
        var genericSuggestions = new List<SelfCareRecommendationDto>
        {
            new()
            {
                Id = "generic-walk",
                Type = SelfCareType.Physical,
                TypeDescription = "Física",
                Title = "Caminar 15 minutos",
                Description = "Una caminata corta para despejar la mente",
                DurationMinutes = 15,
                PersonalizedReason = "El movimiento ayuda a reducir el estrés y mejorar el ánimo",
                ConfidenceScore = 80,
                Icon = "👟"
            },
            new()
            {
                Id = "generic-breathe",
                Type = SelfCareType.Mental,
                TypeDescription = "Mental",
                Title = "Respiración consciente 5 min",
                Description = "Ejercicios de respiración profunda",
                DurationMinutes = 5,
                PersonalizedReason = "La respiración consciente reduce la ansiedad rápidamente",
                ConfidenceScore = 85,
                Icon = "🧘"
            },
            new()
            {
                Id = "generic-stretch",
                Type = SelfCareType.Physical,
                TypeDescription = "Física",
                Title = "Estiramientos",
                Description = "Estira cuello, hombros y espalda",
                DurationMinutes = 10,
                PersonalizedReason = "Previene dolores por estar sentado mucho tiempo",
                ConfidenceScore = 75,
                Icon = "🤸"
            },
            new()
            {
                Id = "generic-tea",
                Type = SelfCareType.Rest,
                TypeDescription = "Descanso",
                Title = "Tomar té o infusión",
                Description = "Prepara tu bebida favorita con calma",
                DurationMinutes = 10,
                PersonalizedReason = "Un momento de pausa consciente reconforta",
                ConfidenceScore = 70,
                Icon = "☕"
            },
            new()
            {
                Id = "generic-music",
                Type = SelfCareType.Creative,
                TypeDescription = "Creativa",
                Title = "Escuchar música",
                Description = "3 canciones que te gustan",
                DurationMinutes = 12,
                PersonalizedReason = "La música eleva el ánimo de forma natural",
                ConfidenceScore = 72,
                Icon = "🎵"
            },
            new()
            {
                Id = "generic-call",
                Type = SelfCareType.Social,
                TypeDescription = "Social",
                Title = "Llamar a un ser querido",
                Description = "Una charla breve con alguien que aprecias",
                DurationMinutes = 15,
                PersonalizedReason = "La conexión social es fundamental para el bienestar",
                ConfidenceScore = 78,
                Icon = "📞"
            },
            new()
            {
                Id = "generic-journal",
                Type = SelfCareType.Creative,
                TypeDescription = "Creativa",
                Title = "Escribir en tu diario",
                Description = "Escribe tus pensamientos y emociones",
                DurationMinutes = 10,
                PersonalizedReason = "Escribir ayuda a procesar emociones",
                ConfidenceScore = 73,
                Icon = "📝"
            },
            new()
            {
                Id = "generic-nap",
                Type = SelfCareType.Rest,
                TypeDescription = "Descanso",
                Title = "Siesta corta",
                Description = "20 minutos de descanso reparador",
                DurationMinutes = 20,
                PersonalizedReason = "Una siesta corta recarga tu energía",
                ConfidenceScore = 68,
                Icon = "😴"
            },
            new()
            {
                Id = "generic-nature",
                Type = SelfCareType.Physical,
                TypeDescription = "Física",
                Title = "Conectar con la naturaleza",
                Description = "Sal al aire libre, aunque sea al balcón",
                DurationMinutes = 10,
                PersonalizedReason = "El contacto con la naturaleza reduce el cortisol",
                ConfidenceScore = 76,
                Icon = "🌳"
            },
            new()
            {
                Id = "generic-screens",
                Type = SelfCareType.Rest,
                TypeDescription = "Descanso",
                Title = "Descanso de pantallas",
                Description = "Aparta todos los dispositivos por un rato",
                DurationMinutes = 15,
                PersonalizedReason = "Tus ojos y mente necesitan un break digital",
                ConfidenceScore = 74,
                Icon = "📵"
            }
        };

        return genericSuggestions
            .OrderByDescending(s => s.ConfidenceScore)
            .Take(count);
    }

    private List<SelfCareRecommendationDto> GenerateContextualSuggestions(
        int currentHour,
        DayOfWeek dayOfWeek,
        int? currentMood,
        List<Domain.Entities.Event> historicalEvents)
    {
        var suggestions = new List<SelfCareRecommendationDto>();

        // Contexto horario
        if (currentHour >= 6 && currentHour < 12)
        {
            // Mañana: Actividades energizantes
            suggestions.AddRange(GetMorningSuggestions());
        }
        else if (currentHour >= 12 && currentHour < 18)
        {
            // Tarde: Pausas activas
            suggestions.AddRange(GetAfternoonSuggestions());
        }
        else
        {
            // Noche: Relajación
            suggestions.AddRange(GetEveningSuggestions());
        }

        // Contexto día de la semana
        if (dayOfWeek == DayOfWeek.Monday)
        {
            suggestions.AddRange(GetMondayBoostSuggestions());
        }
        else if (dayOfWeek == DayOfWeek.Friday)
        {
            suggestions.AddRange(GetFridaySocialSuggestions());
        }
        else if (dayOfWeek == DayOfWeek.Sunday)
        {
            suggestions.AddRange(GetSundayRestSuggestions());
        }

        // Contexto mood
        if (currentMood.HasValue && currentMood.Value <= 2)
        {
            // Mood bajo: actividades probadas
            suggestions.AddRange(GetLowMoodSuggestions());
        }

        return suggestions;
    }

    private SelfCareRecommendationDto CalculateScore(
        SelfCareRecommendationDto suggestion,
        Guid userId,
        List<Domain.Entities.Event> historicalEvents)
    {
        // Scoring: (historical_mood_impact * 0.5) + (completion_rate * 0.3) + (recency_boost * 0.2)

        // 1. Calcular impacto histórico en mood
        var similarEvents = historicalEvents
            .Where(e => e.Title.Contains(suggestion.Title.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
            .ToList();

        double historicalMoodImpact = 50; // Default neutral
        if (similarEvents.Any())
        {
            var eventsWithMood = similarEvents.Where(e => e.MoodRating.HasValue).ToList();
            if (eventsWithMood.Any())
            {
                historicalMoodImpact = eventsWithMood.Average(e => e.MoodRating!.Value) * 20; // 1-5 → 20-100
                suggestion.HistoricalMoodImpact = (int)historicalMoodImpact;
            }
        }

        // 2. Calcular tasa de completitud
        double completionRate = 50; // Default neutral
        if (similarEvents.Any())
        {
            var totalSimilar = similarEvents.Count;
            var completed = similarEvents.Count(e => e.MoodRating.HasValue || e.EndDate < DateTime.UtcNow);
            completionRate = (double)completed / totalSimilar * 100;
            suggestion.CompletionRate = (int)completionRate;
        }

        // 3. Recency boost (actividades recientes pero no muy recientes)
        double recencyBoost = 50;
        if (similarEvents.Any())
        {
            var lastOccurrence = similarEvents.Max(e => e.EndDate);
            var daysSince = (DateTime.UtcNow - lastOccurrence).TotalDays;

            if (daysSince >= 2 && daysSince <= 7)
            {
                recencyBoost = 80; // Buen momento para repetir
            }
            else if (daysSince < 2)
            {
                recencyBoost = 30; // Muy reciente, bajar prioridad
            }
        }

        // Cálculo final
        var finalScore = (historicalMoodImpact * 0.5) + (completionRate * 0.3) + (recencyBoost * 0.2);
        suggestion.ConfidenceScore = Math.Min(100, Math.Max(0, (int)finalScore));

        return suggestion;
    }

    private List<SelfCareRecommendationDto> FilterRecentSuggestions(
        Guid userId,
        List<SelfCareRecommendationDto> suggestions)
    {
        if (!_recentSuggestions.ContainsKey(userId))
        {
            return suggestions;
        }

        var cutoff = DateTime.UtcNow.AddHours(-48);
        var recentIds = _recentSuggestions[userId]
            .Where(s => s.UsedAt > cutoff)
            .Select(s => s.Id)
            .ToHashSet();

        return suggestions
            .Where(s => !recentIds.Contains(s.Id))
            .ToList();
    }

    // Helper methods para generar sugerencias contextuales
    private List<SelfCareRecommendationDto> GetMorningSuggestions()
    {
        return new List<SelfCareRecommendationDto>
        {
            new()
            {
                Id = "morning-walk",
                Type = SelfCareType.Physical,
                TypeDescription = "Física",
                Title = "Caminar 20 minutos",
                Description = "Camina al aire libre para empezar el día con energía",
                DurationMinutes = 20,
                PersonalizedReason = "Las caminatas matutinas activan tu metabolismo y mejoran tu ánimo",
                ConfidenceScore = 85,
                Icon = "🌅"
            },
            new()
            {
                Id = "morning-stretch",
                Type = SelfCareType.Physical,
                TypeDescription = "Física",
                Title = "Estiramientos matutinos",
                Description = "Rutina de estiramientos para despertar el cuerpo",
                DurationMinutes = 10,
                PersonalizedReason = "Estirar por la mañana mejora tu flexibilidad y previene dolores",
                ConfidenceScore = 80,
                Icon = "🧘"
            },
            new()
            {
                Id = "morning-coffee-mindful",
                Type = SelfCareType.Mental,
                TypeDescription = "Mental",
                Title = "Café/desayuno consciente",
                Description = "Toma tu café o desayuno sin pantallas, solo disfrutando",
                DurationMinutes = 15,
                PersonalizedReason = "Empezar el día con atención plena reduce la ansiedad",
                ConfidenceScore = 79,
                Icon = "☕"
            },
            new()
            {
                Id = "morning-planning",
                Type = SelfCareType.Mental,
                TypeDescription = "Mental",
                Title = "Planificar el día",
                Description = "Dedica 10 minutos a organizar tus prioridades",
                DurationMinutes = 10,
                PersonalizedReason = "Planificar reduce el estrés y aumenta la productividad",
                ConfidenceScore = 77,
                Icon = "📋"
            },
            new()
            {
                Id = "morning-music",
                Type = SelfCareType.Creative,
                TypeDescription = "Creativa",
                Title = "Música energizante",
                Description = "Escucha música que te guste mientras te preparas",
                DurationMinutes = 15,
                PersonalizedReason = "La música matutina mejora tu estado de ánimo todo el día",
                ConfidenceScore = 75,
                Icon = "🎵"
            }
        };
    }

    private List<SelfCareRecommendationDto> GetAfternoonSuggestions()
    {
        return new List<SelfCareRecommendationDto>
        {
            new()
            {
                Id = "afternoon-break",
                Type = SelfCareType.Rest,
                TypeDescription = "Descanso",
                Title = "Pausa de 10 minutos",
                Description = "Aparta el trabajo y descansa tus ojos",
                DurationMinutes = 10,
                PersonalizedReason = "Las pausas regulares mejoran tu productividad",
                ConfidenceScore = 78,
                Icon = "☕"
            },
            new()
            {
                Id = "afternoon-walk",
                Type = SelfCareType.Physical,
                TypeDescription = "Física",
                Title = "Caminar después de comer",
                Description = "Una caminata digestiva corta",
                DurationMinutes = 15,
                PersonalizedReason = "Caminar después de comer ayuda a la digestión y da energía",
                ConfidenceScore = 75,
                Icon = "🚶"
            },
            new()
            {
                Id = "afternoon-hydrate",
                Type = SelfCareType.Physical,
                TypeDescription = "Física",
                Title = "Pausa para hidratarte",
                Description = "Bebe agua y muévete un poco",
                DurationMinutes = 5,
                PersonalizedReason = "Mantenerte hidratado mejora tu concentración",
                ConfidenceScore = 72,
                Icon = "💧"
            },
            new()
            {
                Id = "afternoon-snack",
                Type = SelfCareType.Physical,
                TypeDescription = "Física",
                Title = "Snack saludable",
                Description = "Come algo nutritivo para recuperar energía",
                DurationMinutes = 10,
                PersonalizedReason = "Un snack saludable evita la fatiga de la tarde",
                ConfidenceScore = 74,
                Icon = "🍎"
            },
            new()
            {
                Id = "afternoon-eyes",
                Type = SelfCareType.Rest,
                TypeDescription = "Descanso",
                Title = "Descanso visual",
                Description = "Ejercicios para los ojos si trabajas en pantalla",
                DurationMinutes = 5,
                PersonalizedReason = "Tus ojos necesitan descansar de las pantallas",
                ConfidenceScore = 76,
                Icon = "👀"
            }
        };
    }

    private List<SelfCareRecommendationDto> GetEveningSuggestions()
    {
        return new List<SelfCareRecommendationDto>
        {
            new()
            {
                Id = "evening-meditation",
                Type = SelfCareType.Mental,
                TypeDescription = "Mental",
                Title = "Meditación nocturna",
                Description = "10 minutos de meditación para relajarte",
                DurationMinutes = 10,
                PersonalizedReason = "Meditar antes de dormir mejora la calidad del sueño",
                ConfidenceScore = 82,
                Icon = "🌙"
            },
            new()
            {
                Id = "evening-journal",
                Type = SelfCareType.Creative,
                TypeDescription = "Creativa",
                Title = "Diario nocturno",
                Description = "Escribe sobre tu día y tus emociones",
                DurationMinutes = 15,
                PersonalizedReason = "Escribir antes de dormir ayuda a procesar el día",
                ConfidenceScore = 76,
                Icon = "📔"
            },
            new()
            {
                Id = "evening-reading",
                Type = SelfCareType.Rest,
                TypeDescription = "Descanso",
                Title = "Lectura relajante",
                Description = "Lee algo que te guste por 20 minutos",
                DurationMinutes = 20,
                PersonalizedReason = "Leer antes de dormir reduce el estrés y mejora el sueño",
                ConfidenceScore = 80,
                Icon = "📖"
            },
            new()
            {
                Id = "evening-tea",
                Type = SelfCareType.Rest,
                TypeDescription = "Descanso",
                Title = "Té caliente y silencio",
                Description = "Prepara un té relajante y disfrútalo sin pantallas",
                DurationMinutes = 15,
                PersonalizedReason = "Un ritual nocturno tranquilo señala a tu cuerpo que es hora de descansar",
                ConfidenceScore = 78,
                Icon = "🍵"
            },
            new()
            {
                Id = "evening-stretch",
                Type = SelfCareType.Physical,
                TypeDescription = "Física",
                Title = "Estiramientos suaves",
                Description = "Serie de estiramientos lentos para liberar tensión",
                DurationMinutes = 10,
                PersonalizedReason = "Estirar antes de dormir relaja los músculos y mejora el descanso",
                ConfidenceScore = 74,
                Icon = "🧘"
            },
            new()
            {
                Id = "evening-gratitude",
                Type = SelfCareType.Mental,
                TypeDescription = "Mental",
                Title = "Lista de gratitud",
                Description = "Escribe 3 cosas por las que estás agradecido hoy",
                DurationMinutes = 5,
                PersonalizedReason = "La gratitud nocturna mejora el ánimo y la calidad del sueño",
                ConfidenceScore = 77,
                Icon = "💝"
            }
        };
    }

    private List<SelfCareRecommendationDto> GetMondayBoostSuggestions()
    {
        return new List<SelfCareRecommendationDto>
        {
            new()
            {
                Id = "monday-energy",
                Type = SelfCareType.Physical,
                TypeDescription = "Física",
                Title = "Ejercicio energizante",
                Description = "15 minutos de ejercicio para empezar la semana",
                DurationMinutes = 15,
                PersonalizedReason = "Comenzar la semana con ejercicio te da energía para los próximos días",
                ConfidenceScore = 83,
                Icon = "💪"
            }
        };
    }

    private List<SelfCareRecommendationDto> GetFridaySocialSuggestions()
    {
        return new List<SelfCareRecommendationDto>
        {
            new()
            {
                Id = "friday-social",
                Type = SelfCareType.Social,
                TypeDescription = "Social",
                Title = "Conectar con amigos",
                Description = "Llama o envía mensaje a un amigo",
                DurationMinutes = 20,
                PersonalizedReason = "El viernes es perfecto para reconectar socialmente",
                ConfidenceScore = 80,
                Icon = "👥"
            }
        };
    }

    private List<SelfCareRecommendationDto> GetSundayRestSuggestions()
    {
        return new List<SelfCareRecommendationDto>
        {
            new()
            {
                Id = "sunday-rest",
                Type = SelfCareType.Rest,
                TypeDescription = "Descanso",
                Title = "Tiempo para ti",
                Description = "Lee, ve una serie, o simplemente no hagas nada",
                DurationMinutes = 60,
                PersonalizedReason = "El domingo es ideal para recargar energías antes de la semana",
                ConfidenceScore = 85,
                Icon = "🛋️"
            }
        };
    }

    private List<SelfCareRecommendationDto> GetLowMoodSuggestions()
    {
        return new List<SelfCareRecommendationDto>
        {
            new()
            {
                Id = "lowmood-breathe",
                Type = SelfCareType.Mental,
                TypeDescription = "Mental",
                Title = "Respiración 4-7-8",
                Description = "Técnica de respiración para calmar la ansiedad",
                DurationMinutes = 5,
                PersonalizedReason = "Esta técnica reduce rápidamente el estrés y la ansiedad",
                ConfidenceScore = 90,
                Icon = "🫁"
            },
            new()
            {
                Id = "lowmood-call",
                Type = SelfCareType.Social,
                TypeDescription = "Social",
                Title = "Hablar con alguien",
                Description = "Llama a alguien de confianza",
                DurationMinutes = 15,
                PersonalizedReason = "Cuando te sientes mal, hablar ayuda a procesar emociones",
                ConfidenceScore = 88,
                Icon = "💬"
            }
        };
    }

    private async Task<List<SelfCareRecommendationDto>> GenerateAISuggestionsAsync(
        Guid userId,
        List<Domain.Entities.Event> historicalEvents,
        int currentHour,
        DayOfWeek currentDayOfWeek,
        int? currentMood)
    {
        try
        {
            // Construir contexto del usuario
            var recentEvents = historicalEvents
                .OrderByDescending(e => e.EndDate)
                .Take(10)
                .ToList();

            var activitiesWithMood = recentEvents
                .Where(e => e.MoodRating.HasValue)
                .Select(e => new
                {
                    e.Title,
                    e.EventCategory?.Name,
                    MoodAfter = e.MoodRating!.Value,
                    DaysAgo = (DateTime.UtcNow - e.EndDate).Days
                })
                .ToList();

            var bestActivities = activitiesWithMood
                .Where(a => a.MoodAfter >= 4)
                .Select(a => a.Title)
                .Distinct()
                .Take(3);

            var timeOfDay = currentHour switch
            {
                >= 6 and < 12 => "mañana",
                >= 12 and < 18 => "tarde",
                _ => "noche"
            };

            var dayName = currentDayOfWeek switch
            {
                DayOfWeek.Monday => "Lunes (inicio de semana)",
                DayOfWeek.Friday => "Viernes (fin de semana laboral)",
                DayOfWeek.Saturday or DayOfWeek.Sunday => "fin de semana",
                _ => "día laboral"
            };

            // Construir prompt para Gemini
            var prompt = $@"Eres un asistente de bienestar personal experto. Genera 2 sugerencias de autocuidado ÚNICAS y CREATIVAS para este usuario.

CONTEXTO DEL USUARIO:
- Momento: {timeOfDay}, {dayName}
- Mood actual: {(currentMood.HasValue ? $"{currentMood}/5" : "no especificado")}
- Actividades recientes que mejoraron su ánimo: {string.Join(", ", bestActivities)}
- Total de actividades registradas: {historicalEvents.Count} en los últimos 30 días

REQUISITOS ESTRICTOS:
1. NO sugieras actividades genéricas como ""caminar"", ""meditar"" o ""respirar"" (ya las tenemos)
2. SÉ CREATIVO: sugiere actividades específicas, detalladas y poco comunes
3. Duración: 5-30 minutos
4. Apropiado para {timeOfDay}
5. Considera que es {dayName}
6. Basado en lo que ha funcionado antes

FORMATO JSON (responde SOLO el JSON, sin texto adicional):
{{
  ""suggestions"": [
    {{
      ""title"": ""Título específico y descriptivo"",
      ""description"": ""Descripción concreta en 1-2 líneas"",
      ""durationMinutes"": 15,
      ""type"": ""Physical"",
      ""personalizedReason"": ""Por qué esta actividad es perfecta para este usuario ahora"",
      ""confidence"": 85,
      ""icon"": ""🎨""
    }}
  ]
}}

TIPOS VÁLIDOS: Physical, Mental, Social, Creative, Rest

Genera 2 sugerencias ahora:";

            // Llamar a Gemini
            var aiResponse = await _aiService.GenerateTextAsync(prompt);

            if (string.IsNullOrEmpty(aiResponse))
            {
                _logger.LogWarning("Gemini retornó respuesta vacía");
                return new List<SelfCareRecommendationDto>();
            }

            // Parsear respuesta JSON
            var suggestions = ParseAISuggestions(aiResponse);

            _logger.LogInformation("Generadas {Count} sugerencias con IA", suggestions.Count);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando sugerencias con IA");
            return new List<SelfCareRecommendationDto>();
        }
    }

    private List<SelfCareRecommendationDto> ParseAISuggestions(string aiResponse)
    {
        try
        {
            // Limpiar respuesta (Gemini a veces incluye markdown)
            var jsonText = aiResponse.Trim();

            // Remover bloques de código markdown
            if (jsonText.StartsWith("```"))
            {
                // Encontrar el primer salto de línea después de ```
                var firstNewline = jsonText.IndexOf('\n');
                if (firstNewline > 0)
                {
                    jsonText = jsonText.Substring(firstNewline + 1);
                }

                // Remover el ``` de cierre
                var lastBackticks = jsonText.LastIndexOf("```");
                if (lastBackticks > 0)
                {
                    jsonText = jsonText.Substring(0, lastBackticks);
                }

                jsonText = jsonText.Trim();
            }

            _logger.LogDebug("JSON limpio para parsear: {Json}", jsonText);

            var result = System.Text.Json.JsonSerializer.Deserialize<AISuggestionsResponse>(jsonText, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            if (result?.Suggestions == null || !result.Suggestions.Any())
            {
                _logger.LogWarning("No se encontraron sugerencias en la respuesta de IA");
                return new List<SelfCareRecommendationDto>();
            }

            return result.Suggestions.Select(s => new SelfCareRecommendationDto
            {
                Id = $"ai-{Guid.NewGuid().ToString()[..8]}",
                Type = ParseSelfCareType(s.Type),
                TypeDescription = GetTypeDescription(ParseSelfCareType(s.Type)),
                Title = s.Title,
                Description = s.Description,
                DurationMinutes = s.DurationMinutes,
                PersonalizedReason = s.PersonalizedReason,
                ConfidenceScore = s.Confidence,
                Icon = s.Icon ?? "✨"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parseando respuesta de IA: {Response}", aiResponse);
            return new List<SelfCareRecommendationDto>();
        }
    }

    private SelfCareType ParseSelfCareType(string type)
    {
        return type switch
        {
            "Physical" => SelfCareType.Physical,
            "Mental" => SelfCareType.Mental,
            "Social" => SelfCareType.Social,
            "Creative" => SelfCareType.Creative,
            "Rest" => SelfCareType.Rest,
            _ => SelfCareType.Mental
        };
    }

    private string GetTypeDescription(SelfCareType type)
    {
        return type switch
        {
            SelfCareType.Physical => "Física",
            SelfCareType.Mental => "Mental",
            SelfCareType.Social => "Social",
            SelfCareType.Creative => "Creativa",
            SelfCareType.Rest => "Descanso",
            _ => "General"
        };
    }

    // Clases para deserializar respuesta de IA
    private class AISuggestionsResponse
    {
        public List<AISuggestion> Suggestions { get; set; } = new();
    }

    private class AISuggestion
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int DurationMinutes { get; set; }
        public string Type { get; set; } = "";
        public string PersonalizedReason { get; set; } = "";
        public int Confidence { get; set; }
        public string? Icon { get; set; }
    }
}
