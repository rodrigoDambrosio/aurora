using System.Text;
using System.Text.Json;
using System.Linq;
using System.Globalization;
using Aurora.Application.DTOs;
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
        var localStart = ConvertToUserLocalTime(eventDto.StartDate, eventDto.TimezoneOffsetMinutes);
        var localEnd = ConvertToUserLocalTime(eventDto.EndDate, eventDto.TimezoneOffsetMinutes);

        var dayOfWeek = localStart.DayOfWeek.ToString();
        var dateFormatted = localStart.ToString("yyyy-MM-dd HH:mm");
        var duration = (localEnd - localStart).TotalHours;
        var timezoneLabel = FormatUserTimezone(eventDto.TimezoneOffsetMinutes);

        var sb = new StringBuilder();
        sb.AppendLine("Eres un asistente de calendario inteligente y personal. Analiza el siguiente evento considerando el contexto del calendario del usuario.");
        sb.AppendLine();
        sb.AppendLine("EVENTO A VALIDAR:");
        sb.AppendLine($"- T�tulo: {eventDto.Title}");
        sb.AppendLine($"- Descripci�n: {eventDto.Description ?? "Sin descripci�n"}");
        sb.AppendLine($"- Fecha y hora: {dateFormatted} ({dayOfWeek}) [{timezoneLabel}]");
        sb.AppendLine($"- Duraci�n: {duration:F1} horas");
        sb.AppendLine($"- Todo el d�a: {(eventDto.IsAllDay ? "S�" : "No")}");
        sb.AppendLine($"- Ubicaci�n: {eventDto.Location ?? "Sin ubicaci�n"}");
        sb.AppendLine();

        if (existingEvents != null && existingEvents.Any())
        {
            sb.AppendLine("CONTEXTO DEL CALENDARIO (eventos cercanos):");
            sb.AppendLine();

            var sortedEvents = existingEvents
                .OrderBy(e => e.StartDate)
                .ToList();

            foreach (var evt in sortedEvents)
            {
                var evtStartLocal = ConvertToUserLocalTime(evt.StartDate, eventDto.TimezoneOffsetMinutes);
                var evtEndLocal = ConvertToUserLocalTime(evt.EndDate, eventDto.TimezoneOffsetMinutes);

                var evtDuration = (evtEndLocal - evtStartLocal).TotalHours;
                var evtDay = evtStartLocal.DayOfWeek.ToString();
                var evtTime = evtStartLocal.ToString("yyyy-MM-dd HH:mm");
                var category = evt.EventCategory?.Name ?? "Sin categor�a";

                sb.AppendLine($"� [{evtTime} ({evtDay})] \"{evt.Title}\" - {evtDuration:F1}h - {category}");
            }

            sb.AppendLine();
            sb.AppendLine($"Total de eventos en el contexto: {sortedEvents.Count}");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("CONTEXTO DEL CALENDARIO: Sin eventos cercanos");
            sb.AppendLine();
        }

        sb.AppendLine("ANALIZA LOS SIGUIENTES ASPECTOS:");
        sb.AppendLine("1. **Conflictos de horario**: �Se superpone con otros eventos?");
        sb.AppendLine("2. **Carga de trabajo**: �El usuario ya tiene muchos eventos ese d�a/semana?");
        sb.AppendLine("3. **Balance vida-trabajo**: �Hay suficiente tiempo libre y de descanso?");
        sb.AppendLine("4. **Hora apropiada**: �Es la hora adecuada para este tipo de actividad?");
        sb.AppendLine("5. **Duraci�n razonable**: �La duraci�n es apropiada?");
        sb.AppendLine("6. **Descanso entre eventos**: �Hay tiempo suficiente entre eventos?");
        sb.AppendLine("7. **Patrones saludables**: �Respeta horarios de descanso y sue�o?");
        sb.AppendLine();
        sb.AppendLine("Responde en formato JSON con esta estructura exacta:");
        sb.AppendLine(@"{");
        sb.AppendLine(@"  ""approved"": true/false,");
        sb.AppendLine(@"  ""severity"": ""info""/""warning""/""critical"",");
        sb.AppendLine(@"  ""message"": ""Tu mensaje personalizado aqu� (s� espec�fico y menciona el contexto)"",");
        sb.AppendLine(@"  ""suggestions"": [""sugerencia espec�fica 1"", ""sugerencia espec�fica 2""]");
        sb.AppendLine(@"}");
        sb.AppendLine();
        sb.AppendLine("CRITERIOS DE DECISI�N:");
        sb.AppendLine("- **approved = false** si hay conflictos directos, sobrecarga evidente o riesgos para la salud");
        sb.AppendLine("- **severity = 'critical'** si es muy problem�tico (ej: conflicto de horario, m�s de 12h de trabajo seguido)");
        sb.AppendLine("- **severity = 'warning'** si es cuestionable pero no cr�tico (ej: poco descanso, d�a muy cargado)");
        sb.AppendLine("- **severity = 'info'** si solo son recomendaciones generales");
        sb.AppendLine();
        sb.AppendLine("S� espec�fico y personalizado en tu an�lisis. Menciona eventos espec�ficos del contexto si son relevantes.");

        return sb.ToString();
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

    public async Task<CreateEventDto> ParseNaturalLanguageAsync(
        string naturalLanguageText,
        Guid userId,
        IEnumerable<EventCategoryDto> availableCategories,
        int timezoneOffsetMinutes = 0,
        IEnumerable<EventDto>? existingEvents = null)
    {
        try
        {
            _logger.LogInformation("Parseando texto natural: {Text} (Timezone offset: {Offset})", naturalLanguageText, timezoneOffsetMinutes);

            var categoryList = availableCategories.ToList();

            // Construir el prompt para parsear el texto
            var prompt = BuildParsingPrompt(naturalLanguageText, categoryList, timezoneOffsetMinutes, existingEvents);

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
            var eventDto = ParseEventFromAIResponse(aiText, categoryList, timezoneOffsetMinutes);

            _logger.LogInformation("Texto parseado exitosamente: {Title} - {StartDate}",
                eventDto.Title, eventDto.StartDate);

            return eventDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear texto natural con IA");
            throw;
        }
    }

    private string BuildParsingPrompt(string naturalLanguageText, IEnumerable<EventCategoryDto> availableCategories, int timezoneOffsetMinutes, IEnumerable<EventDto>? existingEvents)
    {
        var utcNow = DateTime.UtcNow;
        var userLocalNow = utcNow.AddMinutes(timezoneOffsetMinutes);
        var offset = TimeSpan.FromMinutes(timezoneOffsetMinutes);
        var timezoneString = timezoneOffsetMinutes == 0
            ? "UTC"
            : $"UTC{offset.TotalHours:+0;-0}";

        var sb = new StringBuilder();

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
        sb.AppendLine("TEXTO DEL USUARIO:");
        sb.AppendLine($"\"{naturalLanguageText}\"");
        sb.AppendLine();

        if (existingEvents != null && existingEvents.Any())
        {
            sb.AppendLine("CONTEXTO DEL CALENDARIO (eventos cercanos):");
            var sortedEvents = existingEvents
                .OrderBy(e => e.StartDate)
                .Take(10)
                .ToList();

            foreach (var evt in sortedEvents)
            {
                var evtTime = evt.StartDate.ToString("yyyy-MM-dd HH:mm");
                sb.AppendLine($"• [{evtTime}] \"{evt.Title}\" - {evt.EventCategory?.Name ?? "Sin categoría"}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("CATEGORÍAS DISPONIBLES:");
        var categoryList = availableCategories.ToList();
        foreach (var category in categoryList)
        {
            sb.AppendLine($"• {category.Name} - ID: \"{category.Id}\" ({category.Description ?? "Sin descripción"})");
        }
        sb.AppendLine();

        sb.AppendLine("INSTRUCCIONES:");
        sb.AppendLine("1. Interpreta fechas relativas (hoy, mañana, el lunes, etc.) desde la fecha actual del usuario");
        sb.AppendLine("2. Si no se especifica hora, usa horarios razonables según el tipo de evento");
        sb.AppendLine("3. Si no se especifica duración, infiere una duración apropiada (reuniones: 1h, ejercicio: 1.5h, etc.)");
        sb.AppendLine("4. Detecta la categoría según el contenido del evento y usa el ID exacto de la lista de categorías");
        sb.AppendLine("5. Todas las fechas/horas deben devolverse en formato UTC ISO 8601");
        sb.AppendLine("6. Determina la prioridad del evento: 1=Low, 2=Medium, 3=High, 4=Critical");
        sb.AppendLine("   Usa 4 para emergencias o compromisos imprescindibles, 3 para eventos importantes, 2 como valor por defecto y 1 para actividades opcionales");
        sb.AppendLine();
        sb.AppendLine("Responde en formato JSON con esta estructura exacta:");
        sb.AppendLine(@"{");
        sb.AppendLine(@"  ""title"": ""Título del evento"",");
        sb.AppendLine(@"  ""description"": ""Descripción opcional"",");
        sb.AppendLine(@"  ""startDate"": ""2025-10-10T15:00:00Z"",");
        sb.AppendLine(@"  ""endDate"": ""2025-10-10T16:00:00Z"",");
        sb.AppendLine(@"  ""isAllDay"": false,");
        sb.AppendLine(@"  ""location"": ""Ubicación opcional"",");
        sb.AppendLine(@"  ""priority"": 2,");
        var sampleCategoryId = categoryList.FirstOrDefault()?.Id ?? Guid.Empty;
        sb.AppendLine($@"  ""eventCategoryId"": ""{sampleCategoryId}""");
        sb.AppendLine(@"}");
        sb.AppendLine();
        sb.AppendLine("IMPORTANTE:");
        sb.AppendLine("- eventCategoryId DEBE ser un string con el GUID exacto de la lista de categorías");
        sb.AppendLine("- priority debe ser un entero entre 1 y 4 siguiendo la escala indicada");
        sb.AppendLine("- Usa formato ISO 8601 con zona horaria Z (UTC) para las fechas");
        sb.AppendLine("- Sé inteligente al inferir contexto y categoría apropiada");

        return sb.ToString();
    }

    private CreateEventDto ParseEventFromAIResponse(string aiText, IReadOnlyList<EventCategoryDto> availableCategories, int timezoneOffsetMinutes)
    {
        try
        {
            // Intentar extraer JSON de la respuesta
            var jsonStart = aiText.IndexOf('{');
            var jsonEnd = aiText.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = aiText.Substring(jsonStart, jsonEnd - jsonStart);

                using var document = JsonDocument.Parse(jsonText);

                var parsed = JsonSerializer.Deserialize<CreateEventDto>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null && !string.IsNullOrEmpty(parsed.Title))
                {
                    var rawStart = ExtractJsonString(document.RootElement, "startDate");
                    var rawEnd = ExtractJsonString(document.RootElement, "endDate");

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

                    if (resolvedCategoryId == Guid.Empty || !availableCategories.Any(c => c.Id == resolvedCategoryId))
                    {
                        var categoryName = ExtractCategoryName(document.RootElement);

                        if (!string.IsNullOrWhiteSpace(categoryName))
                        {
                            var matched = availableCategories
                                .FirstOrDefault(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));

                            if (matched != null)
                            {
                                resolvedCategoryId = matched.Id;
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
                    parsed.TimezoneOffsetMinutes = timezoneOffsetMinutes;

                    return parsed;
                }
            }

            throw new InvalidOperationException("No se pudo parsear la respuesta de la IA como evento válido");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error al parsear JSON de la respuesta de IA: {Text}", aiText);
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
}
