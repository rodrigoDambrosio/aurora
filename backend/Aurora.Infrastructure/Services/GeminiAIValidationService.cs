using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Aurora.Application.DTOs;
using Aurora.Application.DTOs.User;
using Aurora.Application.Interfaces;
using Aurora.Infrastructure.DTOs.Gemini;
using Aurora.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aurora.Infrastructure.Services;

/// <summary>
/// Implementaci�n del servicio de validaci�n de IA usando Google Gemini
/// </summary>
public class GeminiAIValidationService : IAIValidationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAIValidationService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public GeminiAIValidationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAIValidationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Obtener la API Key desde la configuraci�n
        _apiKey = _configuration["Gemini:ApiKey"]
                  ?? throw new InvalidOperationException("Gemini API Key no configurada en appsettings");

        _baseUrl = _configuration["Gemini:BaseUrl"]
                   ?? "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent";
    }

    public async Task<AIValidationResult> ValidateEventCreationAsync(
        CreateEventDto eventDto,
        Guid userId,
        IEnumerable<EventDto>? existingEvents = null)
    {
        try
        {
            _logger.LogInformation("Iniciando validaci�n de IA para evento: {Title} con contexto de {EventCount} eventos existentes",
                eventDto.Title, existingEvents?.Count() ?? 0);

            // Construir el prompt para Gemini con contexto del calendario
            var prompt = BuildValidationPrompt(eventDto, existingEvents);

            // Crear la solicitud a Gemini
            var geminiRequest = new GeminiRequest
            {
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = prompt }
                        }
                    }
                }
            };

            // Serializar la solicitud
            var jsonRequest = JsonSerializer.Serialize(geminiRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogDebug("Request a Gemini: {Request}", jsonRequest);

            // Construir la URL con la API Key
            var url = $"{_baseUrl}?key={_apiKey}";

            // Enviar la solicitud
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error en la API de Gemini: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                // Si falla la IA, aprobar por defecto
                return new AIValidationResult
                {
                    IsApproved = true,
                    RecommendationMessage = "No se pudo validar con IA, pero el evento puede crearse.",
                    Severity = AIValidationSeverity.Info
                };
            }

            // Parsear la respuesta
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Response de Gemini: {Response}", responseContent);

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Extraer el texto de la respuesta
            var aiText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(aiText))
            {
                _logger.LogWarning("No se recibi� respuesta v�lida de Gemini");
                return new AIValidationResult
                {
                    IsApproved = true,
                    RecommendationMessage = "No se pudo obtener respuesta de la IA.",
                    Severity = AIValidationSeverity.Info
                };
            }

            // Parsear la respuesta de la IA
            var result = ParseAIResponse(aiText);

            _logger.LogInformation("Validaci�n de IA completada: {IsApproved}", result.IsApproved);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar evento con IA");

            // En caso de error, aprobar por defecto para no bloquear la funcionalidad
            return new AIValidationResult
            {
                IsApproved = true,
                RecommendationMessage = "Error al validar con IA, pero el evento puede crearse.",
                Severity = AIValidationSeverity.Info
            };
        }
    }

    private string BuildValidationPrompt(CreateEventDto eventDto, IEnumerable<EventDto>? existingEvents)
    {
        return BuildUnifiedPrompt(
            eventDto: eventDto,
            existingEvents: existingEvents,
            timezoneOffsetMinutes: eventDto.TimezoneOffsetMinutes ?? 0,
            naturalLanguageText: null,
            availableCategories: null,
            includeParsingInstructions: false);
    }

    private AIValidationResult ParseAIResponse(string aiText)
    {
        try
        {
            var jsonStart = aiText.IndexOf('{');
            var jsonEnd = aiText.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = aiText.Substring(jsonStart, jsonEnd - jsonStart);

                var parsed = JsonSerializer.Deserialize<AIResponseParsed>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null)
                {
                    return new AIValidationResult
                    {
                        IsApproved = parsed.Approved,
                        RecommendationMessage = parsed.Message,
                        Severity = parsed.Severity?.ToLower() switch
                        {
                            "critical" => AIValidationSeverity.Critical,
                            "warning" => AIValidationSeverity.Warning,
                            _ => AIValidationSeverity.Info
                        },
                        Suggestions = parsed.Suggestions ?? new List<string>()
                    };
                }
            }

            _logger.LogWarning("No se pudo parsear la respuesta de la IA como JSON");
            return new AIValidationResult
            {
                IsApproved = true,
                RecommendationMessage = aiText.Length > 500 ? aiText.Substring(0, 500) : aiText,
                Severity = AIValidationSeverity.Info
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear respuesta de IA");
            return new AIValidationResult
            {
                IsApproved = true,
                RecommendationMessage = "No se pudo interpretar la respuesta de la IA.",
                Severity = AIValidationSeverity.Info
            };
        }
    }

    public async Task<GeneratePlanResponseDto> GeneratePlanAsync(
        GeneratePlanRequestDto request,
        Guid userId,
        IEnumerable<EventCategoryDto> availableCategories,
        IEnumerable<EventDto>? existingEvents = null,
        UserPreferencesDto? userPreferences = null)
    {
        try
        {
            _logger.LogInformation("Generando plan multi-día para objetivo: {Goal}", request.Goal);

            var categoryList = availableCategories.ToList();

            // Construir el prompt especializado para generación de planes
            var prompt = BuildPlanGenerationPrompt(request, categoryList, existingEvents, userPreferences);

            // Crear la solicitud a Gemini
            var geminiRequest = new GeminiRequest
            {
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = prompt }
                        }
                    }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(geminiRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogDebug("Request a Gemini para generación de plan: {Request}", jsonRequest);

            var url = $"{_baseUrl}?key={_apiKey}";
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error en la API de Gemini para generación de plan: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                throw new InvalidOperationException($"Error al generar plan con IA: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Response de Gemini para generación de plan: {Response}", responseContent);

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var aiText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(aiText))
            {
                _logger.LogWarning("No se recibió respuesta válida de Gemini para generación de plan");
                throw new InvalidOperationException("No se pudo obtener respuesta de la IA para generar el plan.");
            }

            // Parsear la respuesta del plan
            var planResponse = ParsePlanFromAIResponse(aiText, categoryList, request.TimezoneOffsetMinutes, existingEvents);

            _logger.LogInformation(
                "Plan generado exitosamente: {Title} - {Sessions} sesiones en {Weeks} semanas",
                planResponse.PlanTitle,
                planResponse.TotalSessions,
                planResponse.DurationWeeks);

            return planResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar plan multi-día con IA");
            throw;
        }
    }

    public async Task<ParseNaturalLanguageResponseDto> ParseNaturalLanguageAsync(
        string naturalLanguageText,
        Guid userId,
        IEnumerable<EventCategoryDto> availableCategories,
        int timezoneOffsetMinutes = 0,
        IEnumerable<EventDto>? existingEvents = null,
        UserPreferencesDto? userPreferences = null)
    {
        try
        {
            _logger.LogInformation("Parseando texto natural: {Text} (Timezone offset: {Offset})", naturalLanguageText, timezoneOffsetMinutes);

            var categoryList = availableCategories.ToList();

            // Construir el prompt para parsear el texto (PLAN-131: incluyendo preferencias del usuario)
            var prompt = BuildParsingPrompt(naturalLanguageText, categoryList, timezoneOffsetMinutes, existingEvents, userPreferences);

            // Crear la solicitud a Gemini
            var geminiRequest = new GeminiRequest
            {
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = prompt }
                        }
                    }
                }
            };

            // Serializar la solicitud
            var jsonRequest = JsonSerializer.Serialize(geminiRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogDebug("Request a Gemini para parsing: {Request}", jsonRequest);

            // Construir la URL con la API Key
            var url = $"{_baseUrl}?key={_apiKey}";

            // Enviar la solicitud
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error en la API de Gemini para parsing: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                throw new InvalidOperationException($"Error al parsear texto con IA: {response.StatusCode}");
            }

            // Parsear la respuesta
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Response de Gemini para parsing: {Response}", responseContent);

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Extraer el texto de la respuesta
            var aiText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(aiText))
            {
                _logger.LogWarning("No se recibió respuesta válida de Gemini para parsing");
                throw new InvalidOperationException("No se pudo obtener respuesta de la IA para parsear el texto.");
            }

            // Parsear la respuesta de la IA a CreateEventDto garantizando categoría válida
            using var document = ParseAiJson(aiText);
            var rootElement = document.RootElement.Clone();

            var eventDto = ParseEventFromAIResponse(rootElement, categoryList, timezoneOffsetMinutes);
            var validation = ParseValidationFromJson(rootElement);

            if (validation != null)
            {
                validation.UsedAi = true;
            }

            _logger.LogInformation(
                "Texto parseado exitosamente: {Title} - {StartDate} (Análisis IA: {HasAnalysis})",
                eventDto.Title,
                eventDto.StartDate,
                validation != null);

            return new ParseNaturalLanguageResponseDto
            {
                Success = true,
                Event = eventDto,
                Validation = validation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear texto natural con IA");
            throw;
        }
    }

    private string BuildParsingPrompt(
        string naturalLanguageText,
        IEnumerable<EventCategoryDto> availableCategories,
        int timezoneOffsetMinutes,
        IEnumerable<EventDto>? existingEvents,
        UserPreferencesDto? userPreferences = null)
    {
        return BuildUnifiedPrompt(
            eventDto: null,
            existingEvents: existingEvents,
            timezoneOffsetMinutes: timezoneOffsetMinutes,
            naturalLanguageText: naturalLanguageText,
            availableCategories: availableCategories,
            includeParsingInstructions: true,
            userPreferences: userPreferences);
    }

    private CreateEventDto ParseEventFromAIResponse(JsonElement aiRoot, IReadOnlyList<EventCategoryDto> availableCategories, int timezoneOffsetMinutes)
    {
        try
        {
            var eventElement = aiRoot;

            if (TryGetObjectProperty(aiRoot, out var nestedEvent, "event", "evento", "eventData", "event_details"))
            {
                eventElement = nestedEvent;
            }

            var parsed = JsonSerializer.Deserialize<CreateEventDto>(eventElement.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null || string.IsNullOrWhiteSpace(parsed.Title))
            {
                throw new InvalidOperationException("No se pudo parsear la respuesta de la IA como evento válido");
            }

            var rawStart = ExtractJsonString(eventElement, "startDate");
            var rawEnd = ExtractJsonString(eventElement, "endDate");

            if (!Enum.IsDefined(typeof(EventPriority), parsed.Priority) || parsed.Priority == 0)
            {
                parsed.Priority = EventPriority.Medium;
            }

            var normalizedStart = NormalizeAiDate(rawStart, parsed.StartDate, timezoneOffsetMinutes);
            var normalizedEnd = NormalizeAiDate(rawEnd, parsed.EndDate, timezoneOffsetMinutes);

            if (normalizedStart == default)
            {
                normalizedStart = DateTime.UtcNow;
            }

            if (normalizedEnd == default)
            {
                normalizedEnd = normalizedStart.AddHours(1);
            }

            if (normalizedEnd <= normalizedStart)
            {
                normalizedEnd = normalizedStart.AddHours(1);
            }

            parsed.StartDate = normalizedStart;
            parsed.EndDate = normalizedEnd;

            var resolvedCategoryId = parsed.EventCategoryId;
            string? suggestedCategoryName = null;

            if (resolvedCategoryId == Guid.Empty || !availableCategories.Any(c => c.Id == resolvedCategoryId))
            {
                var categoryName = ExtractCategoryName(eventElement);

                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    var matched = availableCategories
                        .FirstOrDefault(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));

                    if (matched != null)
                    {
                        resolvedCategoryId = matched.Id;
                    }
                    else
                    {
                        // Si no existe, guardar el nombre para crear la categoría automáticamente
                        suggestedCategoryName = categoryName;
                        _logger.LogInformation("La IA sugirió una nueva categoría: '{CategoryName}'", categoryName);
                    }
                }
            }

            if (resolvedCategoryId == Guid.Empty || !availableCategories.Any(c => c.Id == resolvedCategoryId))
            {
                var fallback = availableCategories
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .FirstOrDefault();

                if (fallback == null)
                {
                    throw new InvalidOperationException("No hay categorías disponibles para asignar al evento sugerido por IA.");
                }

                _logger.LogWarning("La IA no devolvió una categoría válida. Usando '{CategoryName}' como fallback.", fallback.Name);
                resolvedCategoryId = fallback.Id;
            }

            parsed.EventCategoryId = resolvedCategoryId;
            parsed.SuggestedCategoryName = suggestedCategoryName;
            parsed.TimezoneOffsetMinutes = timezoneOffsetMinutes;

            return parsed;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error al parsear JSON de la respuesta de IA");
            throw new InvalidOperationException("Error al interpretar la respuesta de la IA", ex);
        }
    }

    private static DateTime NormalizeAiDate(string? rawValue, DateTime fallback, int timezoneOffsetMinutes)
    {
        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            var sanitized = rawValue.Trim();

            if (DateTimeOffset.TryParse(sanitized, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
            {
                return dto.ToUniversalTime().UtcDateTime;
            }

            if (DateTime.TryParse(sanitized, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var localParsed))
            {
                return ConvertLocalToUtc(localParsed, timezoneOffsetMinutes);
            }
        }

        if (fallback == default)
        {
            return DateTime.UtcNow;
        }

        return ConvertLocalToUtc(fallback, timezoneOffsetMinutes);
    }

    private static DateTime ConvertLocalToUtc(DateTime value, int timezoneOffsetMinutes)
    {
        var unspecified = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

        if (timezoneOffsetMinutes != 0)
        {
            unspecified = unspecified.AddMinutes(-timezoneOffsetMinutes);
        }

        return DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);
    }

    private static bool TryGetObjectProperty(JsonElement root, out JsonElement candidate, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object)
            {
                candidate = value;
                return true;
            }
        }

        candidate = default;
        return false;
    }

    private JsonDocument ParseAiJson(string aiText)
    {
        var jsonStart = aiText.IndexOf('{');
        var jsonEnd = aiText.LastIndexOf('}') + 1;

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var jsonFragment = aiText.Substring(jsonStart, jsonEnd - jsonStart);
            return JsonDocument.Parse(jsonFragment);
        }

        _logger.LogWarning("No se encontró contenido JSON en la respuesta de la IA: {Snippet}",
            aiText.Length > 200 ? aiText[..200] : aiText);
        throw new InvalidOperationException("La IA no devolvió un JSON válido para el evento sugerido.");
    }

    private AIValidationResult? ParseValidationFromJson(JsonElement aiRoot)
    {
        if (!TryGetObjectProperty(aiRoot, out var analysisElement, "analysis", "validation", "aiValidation", "review"))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<AIResponseParsed>(analysisElement.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null)
            {
                return null;
            }

            return new AIValidationResult
            {
                IsApproved = parsed.Approved,
                RecommendationMessage = string.IsNullOrWhiteSpace(parsed.Message) ? string.Empty : parsed.Message,
                Severity = parsed.Severity?.ToLower() switch
                {
                    "critical" => AIValidationSeverity.Critical,
                    "warning" => AIValidationSeverity.Warning,
                    _ => AIValidationSeverity.Info
                },
                Suggestions = parsed.Suggestions ?? new List<string>(),
                UsedAi = true
            };
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            _logger.LogWarning(ex, "No se pudo interpretar la sección de análisis devuelta por la IA");
            return null;
        }
    }

    // Clase auxiliar para parsear la respuesta JSON de la IA
    private class AIResponseParsed
    {
        public bool Approved { get; set; }
        public string? Severity { get; set; }
        public string? Message { get; set; }
        public List<string>? Suggestions { get; set; }
    }

    private static string? ExtractCategoryName(JsonElement root)
    {
        var candidateProperties = new[]
        {
            "categoryName",
            "category",
            "eventCategoryName",
            "eventCategory",
            "category_label"
        };

        foreach (var property in candidateProperties)
        {
            if (root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var name = value.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
        }

        return null;
    }

    private static string? ExtractJsonString(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    private static DateTime ConvertToUserLocalTime(DateTime dateTime, int? timezoneOffsetMinutes)
    {
        if (dateTime == default)
        {
            return dateTime;
        }

        DateTime utcDateTime = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };

        if (timezoneOffsetMinutes.HasValue)
        {
            return utcDateTime.AddMinutes(timezoneOffsetMinutes.Value);
        }

        return utcDateTime.ToLocalTime();
    }

    /// <summary>
    /// Método unificado para construir prompts de IA con diferentes modos (validación o parsing)
    /// </summary>
    private string BuildUnifiedPrompt(
        CreateEventDto? eventDto,
        IEnumerable<EventDto>? existingEvents,
        int timezoneOffsetMinutes,
        string? naturalLanguageText,
        IEnumerable<EventCategoryDto>? availableCategories,
        bool includeParsingInstructions,
        UserPreferencesDto? userPreferences = null)
    {
        var sb = new StringBuilder();

        // Sección de introducción y contexto
        if (includeParsingInstructions)
        {
            // Modo parsing NLP
            var utcNow = DateTime.UtcNow;
            var userLocalNow = utcNow.AddMinutes(timezoneOffsetMinutes);
            var offset = TimeSpan.FromMinutes(timezoneOffsetMinutes);
            var timezoneString = timezoneOffsetMinutes == 0 ? "UTC" : $"UTC{offset.TotalHours:+0;-0}";

            sb.AppendLine("Eres un asistente de calendario inteligente. Tu tarea es convertir texto en lenguaje natural a un evento estructurado.");
            sb.AppendLine();
            sb.AppendLine($"Fecha actual del usuario: {userLocalNow:yyyy-MM-dd HH:mm} ({timezoneString})");

            if (timezoneOffsetMinutes == 0)
            {
                sb.AppendLine("El usuario ya está en UTC, no realices ajustes adicionales de zona horaria.");
            }
            else
            {
                var hoursOffset = Math.Abs(offset.TotalHours);
                var adjustmentVerb = timezoneOffsetMinutes > 0 ? "restando" : "sumando";
                sb.AppendLine($"Debes convertir todas las fechas/horas a UTC {adjustmentVerb} {hoursOffset:0.##} horas respecto a la hora local del usuario.");
            }

            sb.AppendLine("Cuando el usuario diga 'mañana 3pm' se refiere a su zona horaria local.");
            sb.AppendLine();
            sb.AppendLine("⚠️ IMPORTANTE: Cuando menciones horarios en tus mensajes y sugerencias, SIEMPRE usa la hora LOCAL del usuario, NO menciones UTC.");
            sb.AppendLine();
            sb.AppendLine("TEXTO DEL USUARIO:");
            sb.AppendLine($"\"{naturalLanguageText}\"");
            sb.AppendLine();
        }
        else
        {
            // Modo validación de evento existente
            if (eventDto == null)
                throw new ArgumentNullException(nameof(eventDto), "eventDto es requerido para modo validación");

            var localStart = ConvertToUserLocalTime(eventDto.StartDate, eventDto.TimezoneOffsetMinutes);
            var localEnd = ConvertToUserLocalTime(eventDto.EndDate, eventDto.TimezoneOffsetMinutes);
            var dayOfWeek = localStart.DayOfWeek.ToString();
            var dateFormatted = localStart.ToString("yyyy-MM-dd HH:mm");
            var duration = (localEnd - localStart).TotalHours;
            var timezoneLabel = FormatUserTimezone(eventDto.TimezoneOffsetMinutes);

            sb.AppendLine("Eres un asistente de calendario inteligente y personal. Analiza el siguiente evento considerando el contexto del calendario del usuario.");
            sb.AppendLine();
            sb.AppendLine("⚠️ IMPORTANTE: Cuando menciones horarios en tus mensajes y sugerencias, SIEMPRE usa la hora LOCAL del usuario, NO menciones UTC.");
            sb.AppendLine();
            sb.AppendLine("EVENTO A VALIDAR:");
            sb.AppendLine($"- Título: {eventDto.Title}");
            sb.AppendLine($"- Descripción: {eventDto.Description ?? "Sin descripción"}");
            sb.AppendLine($"- Fecha y hora: {dateFormatted} ({dayOfWeek}) [{timezoneLabel}]");
            sb.AppendLine($"- Duración: {duration:F1} horas");
            sb.AppendLine($"- Todo el día: {(eventDto.IsAllDay ? "Sí" : "No")}");
            sb.AppendLine($"- Ubicación: {eventDto.Location ?? "Sin ubicación"}");
            sb.AppendLine();
        }

        // Sección de contexto del calendario (compartida)
        AppendCalendarContext(sb, existingEvents, eventDto?.TimezoneOffsetMinutes ?? timezoneOffsetMinutes, includeParsingInstructions);

        // Categorías disponibles (solo para parsing)
        if (includeParsingInstructions && availableCategories != null)
        {
            sb.AppendLine("CATEGORÍAS DISPONIBLES:");
            var categoryList = availableCategories.ToList();
            foreach (var category in categoryList)
            {
                sb.AppendLine($"• {category.Name} - ID: \"{category.Id}\" ({category.Description ?? "Sin descripción"})");
            }
            sb.AppendLine();
        }

        // PLAN-131: Preferencias del usuario (días laborales, horarios, etc.)
        AppendUserPreferences(sb, userPreferences);

        // Instrucciones de parsing NLP (solo para modo parsing)
        if (includeParsingInstructions)
        {
            sb.AppendLine("INSTRUCCIONES:");
            sb.AppendLine("1. Interpreta fechas relativas (hoy, mañana, el lunes, etc.) desde la fecha actual del usuario");
            sb.AppendLine("2. Si no se especifica hora, usa horarios razonables según el tipo de evento");
            sb.AppendLine("3. Si no se especifica duración, infiere una duración apropiada (reuniones: 1h, ejercicio: 1.5h, etc.)");
            sb.AppendLine("4. Detecta la categoría según el contenido del evento y usa el ID exacto de la lista de categorías");
            sb.AppendLine("5. Todas las fechas/horas deben devolverse en formato UTC ISO 8601");
            sb.AppendLine("6. Determina la prioridad del evento: 1=Low, 2=Medium, 3=High, 4=Critical");
            sb.AppendLine("   Usa 4 para emergencias o compromisos imprescindibles, 3 para eventos importantes, 2 como valor por defecto y 1 para actividades opcionales");
            sb.AppendLine();
        }

        // Criterios de análisis (compartidos)
        AppendAnalysisCriteria(sb);

        // Formato de respuesta JSON
        AppendJsonResponseFormat(sb, includeParsingInstructions, availableCategories);

        // Criterios de decisión (compartidos)
        AppendDecisionCriteria(sb, includeParsingInstructions);

        return sb.ToString();
    }

    private void AppendCalendarContext(StringBuilder sb, IEnumerable<EventDto>? existingEvents, int timezoneOffsetMinutes, bool isParsingMode)
    {
        if (existingEvents != null && existingEvents.Any())
        {
            sb.AppendLine("CONTEXTO DEL CALENDARIO (eventos cercanos):");

            if (!isParsingMode)
            {
                sb.AppendLine();
            }

            var sortedEvents = existingEvents
                .OrderBy(e => e.StartDate)
                .Take(isParsingMode ? 10 : int.MaxValue)
                .ToList();

            foreach (var evt in sortedEvents)
            {
                if (isParsingMode)
                {
                    var evtTime = evt.StartDate.ToString("yyyy-MM-dd HH:mm");
                    sb.AppendLine($"• [{evtTime}] \"{evt.Title}\" - {evt.EventCategory?.Name ?? "Sin categoría"}");
                }
                else
                {
                    var evtStartLocal = ConvertToUserLocalTime(evt.StartDate, timezoneOffsetMinutes);
                    var evtEndLocal = ConvertToUserLocalTime(evt.EndDate, timezoneOffsetMinutes);
                    var evtDuration = (evtEndLocal - evtStartLocal).TotalHours;
                    var evtDay = evtStartLocal.DayOfWeek.ToString();
                    var evtTime = evtStartLocal.ToString("yyyy-MM-dd HH:mm");
                    var category = evt.EventCategory?.Name ?? "Sin categoría";
                    sb.AppendLine($"• [{evtTime} ({evtDay})] \"{evt.Title}\" - {evtDuration:F1}h - {category}");
                }
            }

            sb.AppendLine();

            if (!isParsingMode)
            {
                sb.AppendLine($"Total de eventos en el contexto: {sortedEvents.Count}");
                sb.AppendLine();
            }
        }
        else if (!isParsingMode)
        {
            sb.AppendLine("CONTEXTO DEL CALENDARIO: Sin eventos cercanos");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// PLAN-131: Agrega las preferencias del usuario al prompt de IA
    /// </summary>
    private void AppendUserPreferences(StringBuilder sb, UserPreferencesDto? userPreferences)
    {
        if (userPreferences == null)
        {
            return;
        }

        sb.AppendLine("PREFERENCIAS DEL USUARIO:");
        sb.AppendLine();

        // Días laborales
        if (userPreferences.WorkDaysOfWeek != null && userPreferences.WorkDaysOfWeek.Any())
        {
            var workDayNames = userPreferences.WorkDaysOfWeek
                .Select(d => ((DayOfWeek)d).ToString())
                .ToList();
            sb.AppendLine($"Días laborales: {string.Join(", ", workDayNames)}");
        }

        // Horario laboral
        if (!string.IsNullOrEmpty(userPreferences.WorkStartTime) &&
            !string.IsNullOrEmpty(userPreferences.WorkEndTime))
        {
            sb.AppendLine($"Horario laboral: {userPreferences.WorkStartTime} - {userPreferences.WorkEndTime}");
        }

        // Recordatorios predeterminados
        if (userPreferences.DefaultReminderMinutes > 0)
        {
            sb.AppendLine($"Recordatorio predeterminado: {userPreferences.DefaultReminderMinutes} minutos antes");
        }

        sb.AppendLine();
        sb.AppendLine("IMPORTANTE: Respeta estas preferencias al sugerir fechas y horarios:");
        sb.AppendLine("• Si el usuario pide un evento laboral sin especificar día, sugiere solo días laborales configurados");
        sb.AppendLine("• Si no especifica horario, sugiere dentro del horario laboral configurado");
        sb.AppendLine("• Si un evento cae fuera del horario laboral, menciona esta observación en el análisis");
        sb.AppendLine("• Si sugiere un día no laboral para un evento de trabajo, advierte sobre esto");
        sb.AppendLine();
    }

    private void AppendAnalysisCriteria(StringBuilder sb)
    {
        sb.AppendLine("ANALIZA LOS SIGUIENTES ASPECTOS:");
        sb.AppendLine("1. **Conflictos de horario**: ¿Se superpone con otros eventos?");
        sb.AppendLine("2. **Carga de trabajo**: ¿El usuario ya tiene muchos eventos ese día/semana?");
        sb.AppendLine("3. **Balance vida-trabajo**: ¿Hay suficiente tiempo libre y de descanso?");
        sb.AppendLine("4. **Hora apropiada**: ¿Es la hora adecuada para este tipo de actividad?");
        sb.AppendLine("5. **Duración razonable**: ¿La duración es apropiada?");
        sb.AppendLine("6. **Descanso entre eventos**: ¿Hay tiempo suficiente entre eventos?");
        sb.AppendLine("7. **Patrones saludables**: ¿Respeta horarios de descanso y sueño?");
        sb.AppendLine();
    }

    private void AppendJsonResponseFormat(StringBuilder sb, bool includeParsingInstructions, IEnumerable<EventCategoryDto>? availableCategories)
    {
        sb.AppendLine("Responde en formato JSON con esta estructura exacta:");

        if (includeParsingInstructions)
        {
            sb.AppendLine(@"{");
            sb.AppendLine(@"  ""event"": {");
            sb.AppendLine(@"    ""title"": ""Título del evento"",");
            sb.AppendLine(@"    ""description"": ""Descripción opcional"",");
            sb.AppendLine(@"    ""startDate"": ""2025-10-10T15:00:00Z"",");
            sb.AppendLine(@"    ""endDate"": ""2025-10-10T16:00:00Z"",");
            sb.AppendLine(@"    ""isAllDay"": false,");
            sb.AppendLine(@"    ""location"": ""Ubicación opcional"",");
            sb.AppendLine(@"    ""priority"": 2,");

            var sampleCategoryId = availableCategories?.FirstOrDefault()?.Id ?? Guid.Empty;
            sb.AppendLine($@"    ""eventCategoryId"": ""{sampleCategoryId}""");

            sb.AppendLine(@"  },");
            sb.AppendLine(@"  ""analysis"": {");
            sb.AppendLine(@"    ""approved"": true/false,");
            sb.AppendLine(@"    ""severity"": ""info""/""warning""/""critical"",");
            sb.AppendLine(@"    ""message"": ""Tu mensaje personalizado aquí (sé específico y menciona el contexto)"",");
            sb.AppendLine(@"    ""suggestions"": [""sugerencia específica 1"", ""sugerencia específica 2""]");
            sb.AppendLine(@"  }");
            sb.AppendLine(@"}");
        }
        else
        {
            sb.AppendLine(@"{");
            sb.AppendLine(@"  ""approved"": true/false,");
            sb.AppendLine(@"  ""severity"": ""info""/""warning""/""critical"",");
            sb.AppendLine(@"  ""message"": ""Tu mensaje personalizado aquí (sé específico y menciona el contexto)"",");
            sb.AppendLine(@"  ""suggestions"": [""sugerencia específica 1"", ""sugerencia específica 2""]");
            sb.AppendLine(@"}");
        }

        sb.AppendLine();
    }

    private void AppendDecisionCriteria(StringBuilder sb, bool includeParsingInstructions)
    {
        if (includeParsingInstructions)
        {
            sb.AppendLine("IMPORTANTE:");
            sb.AppendLine("- eventCategoryId DEBE ser un string con el GUID exacto de la lista de categorías");
            sb.AppendLine("- priority debe ser un entero entre 1 y 4 siguiendo la escala indicada");
            sb.AppendLine("- Usa formato ISO 8601 con zona horaria Z (UTC) para las fechas");
            sb.AppendLine("- Incluye SIEMPRE el objeto analysis aplicando los mismos criterios que la validación manual");
            sb.AppendLine("- Sé inteligente al inferir contexto y categoría apropiada");
            sb.AppendLine("- SIEMPRE menciona horas en formato local del usuario (NO uses UTC en tus mensajes y sugerencias)");
            sb.AppendLine();
            sb.AppendLine("CRITERIOS DE DECISIÓN PARA EL BLOQUE analysis:");
            sb.AppendLine("- approved = false si detectas conflicto directo, sobrecarga o riesgo evidente");
            sb.AppendLine("- severity = 'critical' para problemas graves (conflicto de horario, más de 12h de trabajo continuo)");
            sb.AppendLine("- severity = 'warning' si hay señales preocupantes pero no bloqueantes (poco descanso, agenda muy cargada)");
            sb.AppendLine("- severity = 'info' solo si son recomendaciones ligeras");
            sb.AppendLine("- Las suggestions deben ser acciones concretas para mejorar la planificación");
            sb.AppendLine("- En tus mensajes, SIEMPRE usa horarios en la zona local del usuario, no menciones UTC");
        }
        else
        {
            sb.AppendLine("CRITERIOS DE DECISIÓN:");
            sb.AppendLine("- **approved = false** si hay conflictos directos, sobrecarga evidente o riesgos para la salud");
            sb.AppendLine("- **severity = 'critical'** si es muy problemático (ej: conflicto de horario, más de 12h de trabajo seguido)");
            sb.AppendLine("- **severity = 'warning'** si es cuestionable pero no crítico (ej: poco descanso, día muy cargado)");
            sb.AppendLine("- **severity = 'info'** si solo son recomendaciones generales");
            sb.AppendLine();
            sb.AppendLine("IMPORTANTE: En tus mensajes y sugerencias, SIEMPRE menciona las horas en formato local del usuario, NO uses UTC.");
            sb.AppendLine("Sé específico y personalizado en tu análisis. Menciona eventos específicos del contexto si son relevantes.");
        }
    }

    private static string FormatUserTimezone(int? timezoneOffsetMinutes)
    {
        if (!timezoneOffsetMinutes.HasValue)
        {
            return "zona horaria no especificada";
        }

        if (timezoneOffsetMinutes.Value == 0)
        {
            return "UTC±00:00";
        }

        var offset = TimeSpan.FromMinutes(timezoneOffsetMinutes.Value);
        var sign = timezoneOffsetMinutes.Value >= 0 ? "+" : "-";
        var formatted = offset.Duration().ToString(@"hh\:mm");
        return $"UTC{sign}{formatted}";
    }

    /// <summary>
    /// Construye el prompt especializado para generación de planes multi-día
    /// </summary>
    private string BuildPlanGenerationPrompt(
        GeneratePlanRequestDto request,
        IEnumerable<EventCategoryDto> availableCategories,
        IEnumerable<EventDto>? existingEvents,
        UserPreferencesDto? userPreferences)
    {
        var sb = new StringBuilder();
        var utcNow = DateTime.UtcNow;
        var userLocalNow = utcNow.AddMinutes(request.TimezoneOffsetMinutes);
        var offset = TimeSpan.FromMinutes(request.TimezoneOffsetMinutes);
        var timezoneString = request.TimezoneOffsetMinutes == 0 ? "UTC" : $"UTC{offset.TotalHours:+0;-0}";

        sb.AppendLine("Eres un asistente de planificación inteligente. Tu tarea es crear un PLAN MULTI-DÍA estructurado y progresivo para alcanzar un objetivo.");
        sb.AppendLine();
        sb.AppendLine($"Fecha actual del usuario: {userLocalNow:yyyy-MM-dd HH:mm} ({timezoneString})");
        sb.AppendLine();
        sb.AppendLine("⚠️ IMPORTANTE: Todas las fechas/horas deben estar en formato UTC ISO 8601, pero en tus mensajes menciona horarios en zona local del usuario.");
        sb.AppendLine();
        sb.AppendLine("OBJETIVO DEL USUARIO:");
        sb.AppendLine($"\"{request.Goal}\"");
        sb.AppendLine();

        // Fecha de inicio del plan
        DateTime planStartDate;
        if (request.StartDate.HasValue)
        {
            planStartDate = request.StartDate.Value.AddMinutes(request.TimezoneOffsetMinutes);
            sb.AppendLine($"FECHA DE INICIO DEL PLAN: {planStartDate:yyyy-MM-dd} (especificada por el usuario)");
        }
        else
        {
            planStartDate = userLocalNow.AddDays(1).Date; // Mañana por defecto
            sb.AppendLine($"FECHA DE INICIO DEL PLAN: {planStartDate:yyyy-MM-dd} (mañana)");
        }
        sb.AppendLine();

        // Preferencias del plan
        if (request.DurationWeeks.HasValue || request.SessionsPerWeek.HasValue || request.SessionDurationMinutes.HasValue || !string.IsNullOrEmpty(request.PreferredTimeOfDay))
        {
            sb.AppendLine("PREFERENCIAS DEL PLAN:");
            if (request.DurationWeeks.HasValue)
                sb.AppendLine($"- Duración deseada: {request.DurationWeeks} semanas");
            if (request.SessionsPerWeek.HasValue)
                sb.AppendLine($"- Sesiones por semana: {request.SessionsPerWeek}");
            if (request.SessionDurationMinutes.HasValue)
                sb.AppendLine($"- Duración de sesión: {request.SessionDurationMinutes} minutos");
            if (!string.IsNullOrEmpty(request.PreferredTimeOfDay))
                sb.AppendLine($"- Horario preferido: {request.PreferredTimeOfDay}");
            sb.AppendLine();
        }

        // Contexto del calendario
        AppendCalendarContext(sb, existingEvents, request.TimezoneOffsetMinutes, isParsingMode: true);

        // Categorías disponibles
        sb.AppendLine("CATEGORÍAS DISPONIBLES:");
        var categoryList = availableCategories.ToList();
        foreach (var category in categoryList)
        {
            sb.AppendLine($"• {category.Name} - ID: \"{category.Id}\" ({category.Description ?? "Sin descripción"})");
        }
        sb.AppendLine();

        // Preferencias del usuario
        AppendUserPreferences(sb, userPreferences);

        // Instrucciones específicas para planes
        sb.AppendLine("INSTRUCCIONES PARA GENERACIÓN DE PLAN:");
        sb.AppendLine("1. Analiza el objetivo y determina una duración apropiada (respeta la preferencia si existe)");
        sb.AppendLine("2. Crea un plan PROGRESIVO con sesiones que aumenten en complejidad/intensidad");
        sb.AppendLine("3. Distribuye las sesiones de forma equilibrada respetando días laborales y horarios del usuario");
        sb.AppendLine("4. Evita conflictos con eventos existentes del calendario");
        sb.AppendLine("5. Asigna títulos descriptivos que indiquen el progreso (ej: 'Semana 1: Fundamentos', 'Sesión 3: Práctica avanzada')");
        sb.AppendLine("6. Incluye descripciones detalladas con objetivos específicos de cada sesión");
        sb.AppendLine("7. Asigna la categoría apropiada del catálogo disponible");
        sb.AppendLine("8. Establece prioridades coherentes (sesiones iniciales: Medium, sesiones clave: High)");
        sb.AppendLine($"9. IMPORTANTE: Inicia el plan desde la FECHA DE INICIO especificada ({planStartDate:yyyy-MM-dd})");
        sb.AppendLine();

        // Formato de respuesta JSON
        sb.AppendLine("Responde en formato JSON con esta estructura exacta:");
        sb.AppendLine(@"{");
        sb.AppendLine(@"  ""planTitle"": ""Título descriptivo del plan (ej: 'Plan de 8 semanas para aprender guitarra')"",");
        sb.AppendLine(@"  ""planDescription"": ""Descripción general del plan y su estructura"",");
        sb.AppendLine(@"  ""durationWeeks"": 8,");
        sb.AppendLine(@"  ""totalSessions"": 24,");
        sb.AppendLine(@"  ""events"": [");
        sb.AppendLine(@"    {");
        sb.AppendLine(@"      ""title"": ""Semana 1 - Sesión 1: Introducción y fundamentos"",");
        sb.AppendLine(@"      ""description"": ""Objetivos específicos de esta sesión..."",");
        sb.AppendLine(@"      ""startDate"": ""2025-01-20T10:00:00Z"",");
        sb.AppendLine(@"      ""endDate"": ""2025-01-20T11:30:00Z"",");
        sb.AppendLine(@"      ""isAllDay"": false,");
        sb.AppendLine(@"      ""location"": """",");
        sb.AppendLine(@"      ""priority"": 2,");
        sb.AppendLine($@"      ""eventCategoryId"": ""{categoryList.FirstOrDefault()?.Id ?? Guid.Empty}""");
        sb.AppendLine(@"    }");
        sb.AppendLine(@"    // ... más eventos");
        sb.AppendLine(@"  ],");
        sb.AppendLine(@"  ""additionalTips"": ""Consejos generales para tener éxito con este plan"",");
        sb.AppendLine(@"  ""conflictWarnings"": [""Advertencia 1 si detectas conflictos""]");
        sb.AppendLine(@"}");
        sb.AppendLine();

        sb.AppendLine("VALIDACIÓN DE CONFLICTOS:");
        sb.AppendLine("- Compara cada sesión del plan con los eventos existentes del calendario");
        sb.AppendLine("- Si una sesión se superpone con un evento existente, agrégala a conflictWarnings");
        sb.AppendLine("- Ejemplo de advertencia: 'Sesión 3 (Lunes 15:00) se superpone con Reunión de equipo'");
        sb.AppendLine();

        sb.AppendLine("RECUERDA:");
        sb.AppendLine("- Todas las fechas en formato UTC ISO 8601 con 'Z' al final");
        sb.AppendLine("- eventCategoryId debe ser un GUID exacto de la lista de categorías");
        sb.AppendLine("- priority debe ser 1-4 (1=Low, 2=Medium, 3=High, 4=Critical)");
        sb.AppendLine("- Inicia el plan desde mañana, no desde hoy");
        sb.AppendLine("- Sé específico y progresivo en los títulos y descripciones");

        return sb.ToString();
    }

    /// <summary>
    /// Parsea la respuesta de IA para extraer el plan generado
    /// </summary>
    private GeneratePlanResponseDto ParsePlanFromAIResponse(
        string aiText,
        IReadOnlyList<EventCategoryDto> availableCategories,
        int timezoneOffsetMinutes,
        IEnumerable<EventDto>? existingEvents)
    {
        try
        {
            using var document = ParseAiJson(aiText);
            var rootElement = document.RootElement.Clone();

            var planResponse = new GeneratePlanResponseDto
            {
                PlanTitle = ExtractJsonString(rootElement, "planTitle") ?? "Plan generado",
                PlanDescription = ExtractJsonString(rootElement, "planDescription") ?? "",
                DurationWeeks = rootElement.TryGetProperty("durationWeeks", out var durationProp) && durationProp.ValueKind == JsonValueKind.Number
                    ? durationProp.GetInt32()
                    : 1,
                TotalSessions = rootElement.TryGetProperty("totalSessions", out var sessionsProp) && sessionsProp.ValueKind == JsonValueKind.Number
                    ? sessionsProp.GetInt32()
                    : 0,
                AdditionalTips = ExtractJsonString(rootElement, "additionalTips")
            };

            // Parsear eventos
            if (rootElement.TryGetProperty("events", out var eventsArray) && eventsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var eventElement in eventsArray.EnumerateArray())
                {
                    try
                    {
                        var eventDto = ParseEventFromAIResponse(eventElement, availableCategories, timezoneOffsetMinutes);
                        planResponse.Events.Add(eventDto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error al parsear un evento del plan, continuando con el siguiente");
                    }
                }
            }

            // Actualizar total de sesiones si no coincide
            if (planResponse.TotalSessions != planResponse.Events.Count)
            {
                planResponse.TotalSessions = planResponse.Events.Count;
            }

            // Parsear advertencias de conflictos
            if (rootElement.TryGetProperty("conflictWarnings", out var warningsArray) && warningsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var warning in warningsArray.EnumerateArray())
                {
                    if (warning.ValueKind == JsonValueKind.String)
                    {
                        var warningText = warning.GetString();
                        if (!string.IsNullOrWhiteSpace(warningText))
                        {
                            planResponse.ConflictWarnings.Add(warningText);
                        }
                    }
                }
            }

            // Detectar conflictos adicionales programáticamente
            if (existingEvents != null && existingEvents.Any())
            {
                foreach (var plannedEvent in planResponse.Events)
                {
                    var conflicts = existingEvents.Where(existing =>
                        (plannedEvent.StartDate >= existing.StartDate && plannedEvent.StartDate < existing.EndDate) ||
                        (plannedEvent.EndDate > existing.StartDate && plannedEvent.EndDate <= existing.EndDate) ||
                        (plannedEvent.StartDate <= existing.StartDate && plannedEvent.EndDate >= existing.EndDate)
                    );

                    foreach (var conflict in conflicts)
                    {
                        var warningMessage = $"'{plannedEvent.Title}' se superpone con '{conflict.Title}' ({conflict.StartDate:dd/MM HH:mm})";
                        if (!planResponse.ConflictWarnings.Contains(warningMessage))
                        {
                            planResponse.ConflictWarnings.Add(warningMessage);
                        }
                    }
                }
            }

            planResponse.HasPotentialConflicts = planResponse.ConflictWarnings.Any();

            return planResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear respuesta del plan de IA");
            throw new InvalidOperationException("No se pudo interpretar el plan generado por la IA", ex);
        }
    }

    public async Task<IEnumerable<ScheduleSuggestionDto>?> GenerateScheduleSuggestionsAsync(
        Guid userId,
        IEnumerable<EventDto> events)
    {
        try
        {
            _logger.LogInformation("Generando sugerencias con IA para {EventCount} eventos", events.Count());

            var prompt = BuildScheduleSuggestionsPrompt(events);

            var geminiRequest = new GeminiRequest
            {
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = prompt }
                        }
                    }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(geminiRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var url = $"{_baseUrl}?key={_apiKey}";
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error en API Gemini: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null; // Fallback a algoritmo manual
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var aiText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(aiText))
            {
                _logger.LogWarning("Respuesta vacía de Gemini");
                return null;
            }

            _logger.LogDebug("Respuesta IA: {Response}", aiText);

            // Parsear JSON de sugerencias
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(aiText, @"\[[\s\S]*\]");
            if (!jsonMatch.Success)
            {
                _logger.LogWarning("No se encontró JSON en respuesta de IA");
                return null;
            }

            var suggestions = JsonSerializer.Deserialize<List<ScheduleSuggestionDto>>(jsonMatch.Value, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("IA generó {Count} sugerencias", suggestions?.Count ?? 0);
            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando sugerencias con IA");
            return null; // Fallback a algoritmo manual
        }
    }

    private string BuildScheduleSuggestionsPrompt(IEnumerable<EventDto> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analiza este calendario y genera sugerencias de optimización en JSON.");
        sb.AppendLine();
        sb.AppendLine("EVENTOS:");

        foreach (var evt in events.OrderBy(e => e.StartDate))
        {
            sb.AppendLine($"- [{evt.Id}] {evt.Title} | {evt.StartDate:yyyy-MM-dd HH:mm} - {evt.EndDate:HH:mm} | Cat: {evt.EventCategory?.Name ?? "Sin categoría"}");
        }

        sb.AppendLine();
        sb.AppendLine("DETECTA: conflictos, sobrecarga, falta descansos, mala distribución, eventos duplicados/similares.");
        sb.AppendLine("Para eventos similares: sugiere unificarlos o agruparlos.");
        sb.AppendLine();
        sb.AppendLine("RESPONDE SOLO con array JSON:");
        sb.AppendLine("[{");
        sb.AppendLine("  \"eventId\": \"guid-del-evento o null\",");
        sb.AppendLine("  \"type\": 1-6 (1=MoveEvent,2=ResolveConflict,3=OptimizeDistribution,4=PatternAlert,5=SuggestBreak,6=GeneralReorganization),");
        sb.AppendLine("  \"description\": \"Texto corto y ACCIONABLE (ej: 'Mover Guitarra a las 10:00', 'Crear descanso de 30min')\",");
        sb.AppendLine("  \"reason\": \"Explicación detallada del POR QUÉ\",");
        sb.AppendLine("  \"priority\": 1-5,");
        sb.AppendLine("  \"suggestedDateTime\": \"2025-11-08T15:00:00Z (OBLIGATORIO para type=5 SuggestBreak)\",");
        sb.AppendLine("  \"confidenceScore\": 70-100,");
        sb.AppendLine("  \"relatedEventTitles\": [\"Título evento 1\", \"Título evento 2\"]");
        sb.AppendLine("}]");
        sb.AppendLine();
        sb.AppendLine("REGLAS OBLIGATORIAS PARA TODOS LOS TIPOS:");
        sb.AppendLine();
        sb.AppendLine("type=1 (MoveEvent):");
        sb.AppendLine("  - eventId: ID del evento a mover");
        sb.AppendLine("  - suggestedDateTime: nueva fecha/hora EXACTA");
        sb.AppendLine("  - description: 'Mover [Nombre] a [hora específica]'");
        sb.AppendLine();
        sb.AppendLine("type=2 (ResolveConflict):");
        sb.AppendLine("  - eventId: ID del evento en conflicto");
        sb.AppendLine("  - relatedEventTitles: títulos de TODOS los eventos en conflicto");
        sb.AppendLine("  - suggestedDateTime: nueva hora propuesta para resolver");
        sb.AppendLine();
        sb.AppendLine("type=3 (OptimizeDistribution):");
        sb.AppendLine("  - relatedEventTitles: títulos de TODOS los eventos a reorganizar (mínimo 2)");
        sb.AppendLine("  - description: acción específica (ej: 'Agrupar Salud en lunes y miércoles')");
        sb.AppendLine();
        sb.AppendLine("type=4 (PatternAlert):");
        sb.AppendLine("  - relatedEventTitles: títulos de TODOS los eventos que forman el patrón (mínimo 2)");
        sb.AppendLine("  - description: patrón detectado + acción sugerida");
        sb.AppendLine("  - OBLIGATORIO: listar eventos específicos involucrados");
        sb.AppendLine();
        sb.AppendLine("type=5 (SuggestBreak):");
        sb.AppendLine("  - suggestedDateTime: fecha/hora EXACTA del descanso propuesto");
        sb.AppendLine("  - relatedEventTitles: eventos entre los que va el descanso");
        sb.AppendLine("  - description: 'Crear descanso de [duración] entre [evento1] y [evento2]'");
        sb.AppendLine();
        sb.AppendLine("type=6 (GeneralReorganization):");
        sb.AppendLine("  - relatedEventTitles: eventos afectados por la reorganización");
        sb.AppendLine();
        sb.AppendLine("CRÍTICO: SIEMPRE incluir relatedEventTitles cuando hay 2+ eventos involucrados. El usuario debe ver qué eventos exactos se afectan.");

        return sb.ToString();
    }

    public async Task<string> GenerateTextAsync(string prompt, string? context = null)
    {
        try
        {
            _logger.LogInformation("Generando texto con IA");

            var fullPrompt = string.IsNullOrEmpty(context)
                ? prompt
                : $"{context}\n\n{prompt}";

            // Construir request a Gemini
            var geminiRequest = new GeminiRequest
            {
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = fullPrompt }
                        }
                    }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(geminiRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var url = $"{_baseUrl}?key={_apiKey}";
            var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            // Timeout de 10 segundos para self-care suggestions
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.PostAsync(url, httpContent, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error en la API de Gemini: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return "";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Respuesta de Gemini: {Response}", responseContent);

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";
            _logger.LogInformation("Texto generado exitosamente: {Length} caracteres", text.Length);
            return text;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Timeout generando texto con IA (10s)");
            return "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando texto con IA");
            return "";
        }
    }
}

