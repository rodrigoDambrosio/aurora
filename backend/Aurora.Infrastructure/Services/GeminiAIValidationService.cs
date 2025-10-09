using System.Text;
using System.Text.Json;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Infrastructure.DTOs.Gemini;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aurora.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de validación de IA usando Google Gemini
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

        // Obtener la API Key desde la configuración
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
            _logger.LogInformation("Iniciando validación de IA para evento: {Title} con contexto de {EventCount} eventos existentes", 
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
                _logger.LogWarning("No se recibió respuesta válida de Gemini");
                return new AIValidationResult
                {
                    IsApproved = true,
                    RecommendationMessage = "No se pudo obtener respuesta de la IA.",
                    Severity = AIValidationSeverity.Info
                };
            }

            // Parsear la respuesta de la IA
            var result = ParseAIResponse(aiText);
            
            _logger.LogInformation("Validación de IA completada: {IsApproved}", result.IsApproved);

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
        var dayOfWeek = eventDto.StartDate.DayOfWeek.ToString();
        var dateFormatted = eventDto.StartDate.ToString("yyyy-MM-dd HH:mm");
        var duration = (eventDto.EndDate - eventDto.StartDate).TotalHours;

        var sb = new StringBuilder();
        sb.AppendLine("Eres un asistente de calendario inteligente y personal. Analiza el siguiente evento considerando el contexto del calendario del usuario.");
        sb.AppendLine();
        sb.AppendLine("EVENTO A VALIDAR:");
        sb.AppendLine($"- Título: {eventDto.Title}");
        sb.AppendLine($"- Descripción: {eventDto.Description ?? "Sin descripción"}");
        sb.AppendLine($"- Fecha y hora: {dateFormatted} ({dayOfWeek})");
        sb.AppendLine($"- Duración: {duration:F1} horas");
        sb.AppendLine($"- Todo el día: {(eventDto.IsAllDay ? "Sí" : "No")}");
        sb.AppendLine($"- Ubicación: {eventDto.Location ?? "Sin ubicación"}");
        sb.AppendLine();

        // Agregar contexto del calendario si existe
        if (existingEvents != null && existingEvents.Any())
        {
            sb.AppendLine("CONTEXTO DEL CALENDARIO (eventos cercanos):");
            sb.AppendLine();
            
            var sortedEvents = existingEvents
                .OrderBy(e => e.StartDate)
                .ToList();

            foreach (var evt in sortedEvents)
            {
                var evtDuration = (evt.EndDate - evt.StartDate).TotalHours;
                var evtDay = evt.StartDate.DayOfWeek.ToString();
                var evtTime = evt.StartDate.ToString("yyyy-MM-dd HH:mm");
                var category = evt.EventCategory?.Name ?? "Sin categoría";
                
                sb.AppendLine($"• [{evtTime} ({evtDay})] \"{evt.Title}\" - {evtDuration:F1}h - {category}");
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
        sb.AppendLine("1. **Conflictos de horario**: ¿Se superpone con otros eventos?");
        sb.AppendLine("2. **Carga de trabajo**: ¿El usuario ya tiene muchos eventos ese día/semana?");
        sb.AppendLine("3. **Balance vida-trabajo**: ¿Hay suficiente tiempo libre y de descanso?");
        sb.AppendLine("4. **Hora apropiada**: ¿Es la hora adecuada para este tipo de actividad?");
        sb.AppendLine("5. **Duración razonable**: ¿La duración es apropiada?");
        sb.AppendLine("6. **Descanso entre eventos**: ¿Hay tiempo suficiente entre eventos?");
        sb.AppendLine("7. **Patrones saludables**: ¿Respeta horarios de descanso y sueño?");
        sb.AppendLine();
        sb.AppendLine("Responde en formato JSON con esta estructura exacta:");
        sb.AppendLine(@"{");
        sb.AppendLine(@"  ""approved"": true/false,");
        sb.AppendLine(@"  ""severity"": ""info""/""warning""/""critical"",");
        sb.AppendLine(@"  ""message"": ""Tu mensaje personalizado aquí (sé específico y menciona el contexto)"",");
        sb.AppendLine(@"  ""suggestions"": [""sugerencia específica 1"", ""sugerencia específica 2""]");
        sb.AppendLine(@"}");
        sb.AppendLine();
        sb.AppendLine("CRITERIOS DE DECISIÓN:");
        sb.AppendLine("- **approved = false** si hay conflictos directos, sobrecarga evidente o riesgos para la salud");
        sb.AppendLine("- **severity = 'critical'** si es muy problemático (ej: conflicto de horario, más de 12h de trabajo seguido)");
        sb.AppendLine("- **severity = 'warning'** si es cuestionable pero no crítico (ej: poco descanso, día muy cargado)");
        sb.AppendLine("- **severity = 'info'** si solo son recomendaciones generales");
        sb.AppendLine();
        sb.AppendLine("Sé específico y personalizado en tu análisis. Menciona eventos específicos del contexto si son relevantes.");

        return sb.ToString();
    }

    private AIValidationResult ParseAIResponse(string aiText)
    {
        try
        {
            // Intentar extraer JSON de la respuesta
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

            // Si no se puede parsear, aprobar por defecto
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

    // Clase auxiliar para parsear la respuesta JSON de la IA
    private class AIResponseParsed
    {
        public bool Approved { get; set; }
        public string? Severity { get; set; }
        public string? Message { get; set; }
        public List<string>? Suggestions { get; set; }
    }
}
