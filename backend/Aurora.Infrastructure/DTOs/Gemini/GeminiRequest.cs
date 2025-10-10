using System.Text.Json.Serialization;

namespace Aurora.Infrastructure.DTOs.Gemini;

/// <summary>
/// Request para la API de Gemini
/// </summary>
public class GeminiRequest
{
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = new();
}

/// <summary>
/// Contenido del mensaje para Gemini
/// </summary>
public class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

/// <summary>
/// Parte del contenido (texto)
/// </summary>
public class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
