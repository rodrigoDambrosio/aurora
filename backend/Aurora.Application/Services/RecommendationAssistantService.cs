using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aurora.Application.Services;

public class RecommendationAssistantService : IRecommendationAssistantService
{
    private readonly IAIValidationService _aiValidationService;
    private readonly ILogger<RecommendationAssistantService> _logger;

    private static readonly Regex JsonArrayExtractor = new("\\[[\\s\\S]*\\]", RegexOptions.Compiled);

    public RecommendationAssistantService(
        IAIValidationService aiValidationService,
        ILogger<RecommendationAssistantService> logger)
    {
        _aiValidationService = aiValidationService ?? throw new ArgumentNullException(nameof(aiValidationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyCollection<RecommendationDto>> GenerateConversationalRecommendationsAsync(
        Guid userId,
        RecommendationAssistantRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var conversation = (request.Conversation ?? Array.Empty<RecommendationConversationMessageDto>())
            .Where(message => !string.IsNullOrWhiteSpace(message.Content))
            .TakeLast(10)
            .ToList();

        if (conversation.Count == 0)
        {
            throw new ArgumentException("Se requiere al menos un mensaje en la conversación para generar recomendaciones.", nameof(request));
        }

        var prompt = BuildPrompt(request, conversation);

        _logger.LogInformation("Generando recomendaciones conversacionales para el usuario {UserId} con {MessageCount} mensajes de contexto.", userId, conversation.Count);

        var aiResponse = await _aiValidationService.GenerateTextAsync(prompt);

        if (string.IsNullOrWhiteSpace(aiResponse))
        {
            throw new InvalidOperationException("La IA no devolvió contenido para generar recomendaciones.");
        }

        var recommendations = ParseRecommendations(aiResponse);

        if (recommendations.Count == 0)
        {
            throw new InvalidOperationException("La IA no generó recomendaciones aprovechables.");
        }

        _logger.LogInformation("La IA generó {Count} recomendaciones a partir de la conversación.", recommendations.Count);
        return recommendations;
    }

    public async Task<string> GenerateAssistantReplyAsync(
        Guid userId,
        RecommendationAssistantChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var conversation = request.Conversation
            .Where(message => !string.IsNullOrWhiteSpace(message.Content))
            .TakeLast(12)
            .ToList();

        if (conversation.Count == 0)
        {
            throw new ArgumentException("Se requiere contexto de conversación para generar una respuesta.", nameof(request));
        }

        _logger.LogInformation(
            "Generando respuesta conversacional para el usuario {UserId} con {MessageCount} mensajes de contexto.",
            userId,
            conversation.Count);

        var prompt = BuildAssistantReplyPrompt(request, conversation);
        var response = await _aiValidationService.GenerateTextAsync(prompt);

        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException("La IA no devolvió una respuesta para el asistente.");
        }

        return response.Trim();
    }

    private static string BuildPrompt(
        RecommendationAssistantRequestDto request,
        IReadOnlyCollection<RecommendationConversationMessageDto> conversation)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sos Aurora, una planificadora personal enfocada en bienestar.");
        sb.AppendLine("Contestá con empatía rioplatense, pero devolvé exclusivamente un array JSON (sin texto adicional).");
        sb.AppendLine("Cada elemento del array debe ser un objeto con estos campos: id (string), title (string), subtitle (string opcional), reason (string), recommendationType (string), suggestedStart (ISO 8601), suggestedDurationMinutes (entero), confidence (0-1), categoryName (string opcional), moodImpact (string opcional) y summary (string opcional).");
        sb.AppendLine("Si no tenés suficiente contexto devolvé un array vacío. Máximo 5 recomendaciones.");

        var moodLabel = request.CurrentMood.HasValue
            ? $"{request.CurrentMood.Value}/5"
            : "sin dato";

        var context = string.IsNullOrWhiteSpace(request.ExternalContext)
            ? "Sin contexto adicional"
            : request.ExternalContext.Trim();

        sb.AppendLine();
        sb.AppendLine($"Estado de ánimo reportado: {moodLabel}.");
        sb.AppendLine($"Contexto declarado: {context}.");

        if (request.FallbackRecommendations is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine("Recomendaciones heurísticas recientes (para referencia, no repitas tal cual):");
            foreach (var (item, index) in request.FallbackRecommendations.Take(3).Select((item, index) => (item, index)))
            {
                var label = index + 1;
                var start = TryFormatDate(item.SuggestedStart);
                sb.AppendLine($"{label}. {item.Title} — {item.Reason} {(string.IsNullOrWhiteSpace(start) ? string.Empty : $"({start})")}".Trim());
            }
        }

        sb.AppendLine();
        sb.AppendLine("Fragmentos relevantes de la conversación (del más nuevo al más viejo):");
        foreach (var message in conversation.Reverse())
        {
            var role = string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                ? "Aurora"
                : "Usuario";
            sb.AppendLine($"- {role}: {message.Content.Trim()}");
        }

        sb.AppendLine();
        sb.AppendLine("Recordatorios finales:");
        sb.AppendLine("- Debés devolver solo un array JSON válido.");
        sb.AppendLine("- Proponé actividades realistas que se puedan agendar pronto.");
        sb.AppendLine("- Usá español rioplatense y tono cálido en las descripciones.");

        return sb.ToString();
    }

    private static string BuildAssistantReplyPrompt(
        RecommendationAssistantChatRequestDto request,
        IReadOnlyCollection<RecommendationConversationMessageDto> conversation)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sos Aurora, una asistente de planificación personal enfocada en bienestar.");
        sb.AppendLine("Respondé en español rioplatense con tono cercano y empático.");
        sb.AppendLine("Usá párrafos cortos o listas de hasta cuatro ítems y ofrecé acciones concretas.");
        sb.AppendLine("Si detectás estrés o agotamiento, sugerí respiración breve o micro descansos realistas.");
        sb.AppendLine("No prometas acciones automáticas; guiá al usuario para llevarlas a cabo manualmente.");

        var moodLabel = request.CurrentMood.HasValue
            ? $"{request.CurrentMood.Value}/5"
            : "sin dato";

        var context = string.IsNullOrWhiteSpace(request.ExternalContext)
            ? "Sin contexto adicional"
            : request.ExternalContext.Trim();

        sb.AppendLine();
        sb.AppendLine($"Estado de ánimo reportado: {moodLabel}.");
        sb.AppendLine($"Contexto declarado: {context}.");

        sb.AppendLine();
        sb.AppendLine("Conversación reciente (del mensaje más antiguo al más nuevo):");
        foreach (var message in conversation)
        {
            var role = string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                ? "Aurora"
                : "Usuario";
            sb.AppendLine($"- {role}: {message.Content.Trim()}");
        }

        sb.AppendLine();
        sb.AppendLine("Redactá la próxima respuesta de Aurora siguiendo el estilo indicado.");

        return sb.ToString();
    }

    private static string TryFormatDate(DateTime dateTime)
    {
        if (dateTime == default)
        {
            return string.Empty;
        }

        var utcDate = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };

        return utcDate.ToLocalTime().ToString("dd/MM HH:mm", CultureInfo.InvariantCulture);
    }

    private static List<RecommendationDto> ParseRecommendations(string raw)
    {
        var jsonSegment = ExtractJsonArray(raw);
        if (string.IsNullOrWhiteSpace(jsonSegment))
        {
            throw new InvalidOperationException("La IA no devolvió un array JSON de recomendaciones.");
        }

        try
        {
            using var document = JsonDocument.Parse(jsonSegment);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("La respuesta de la IA no es un array JSON.");
            }

            var result = new List<RecommendationDto>();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var index = 0;

            foreach (var element in document.RootElement.EnumerateArray())
            {
                index++;
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var title = GetString(element, "title") ?? $"Recomendación {index}";
                var reason = GetString(element, "reason") ?? "Basado en tu conversación reciente.";
                var recommendationType = GetString(element, "recommendationType");
                if (string.IsNullOrWhiteSpace(recommendationType))
                {
                    recommendationType = "ai";
                }
                var id = GetString(element, "id") ?? $"ai-{timestamp}-{index}";

                var suggestedStart = NormalizeSuggestedStart(GetDate(element, "suggestedStart"));
                var duration = GetInt(element, "suggestedDurationMinutes") ?? 30;
                var confidence = GetDouble(element, "confidence") ?? 0.6;

                var recommendation = new RecommendationDto
                {
                    Id = id,
                    Title = title,
                    Subtitle = GetString(element, "subtitle"),
                    Reason = reason,
                    RecommendationType = recommendationType,
                    SuggestedStart = suggestedStart,
                    SuggestedDurationMinutes = Math.Max(10, duration),
                    Confidence = Math.Clamp(confidence, 0.2, 0.95),
                    CategoryId = GetGuid(element, "categoryId"),
                    CategoryName = GetString(element, "categoryName"),
                    MoodImpact = GetString(element, "moodImpact"),
                    Summary = GetString(element, "summary")
                };

                result.Add(recommendation);
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("No se pudo interpretar el JSON generado por la IA.", ex);
        }
    }

    private static DateTime NormalizeSuggestedStart(DateTime? candidate)
    {
        var utcCandidate = candidate.HasValue
            ? candidate.Value.Kind switch
            {
                DateTimeKind.Utc => candidate.Value,
                DateTimeKind.Local => candidate.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(candidate.Value, DateTimeKind.Utc)
            }
            : DateTime.UtcNow;

        var earliestAllowed = DateTime.UtcNow.AddMinutes(5);
        return utcCandidate < earliestAllowed ? earliestAllowed : utcCandidate;
    }

    private static string? ExtractJsonArray(string raw)
    {
        var match = JsonArrayExtractor.Match(raw);
        return match.Success ? match.Value : null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static DateTime? GetDate(JsonElement element, string propertyName)
    {
        var raw = GetString(element, propertyName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
        }

        return null;
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDouble(out var number) => number,
            JsonValueKind.String when double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static Guid? GetGuid(JsonElement element, string propertyName)
    {
        var raw = GetString(element, propertyName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return Guid.TryParse(raw, out var guid) ? guid : null;
    }
}
