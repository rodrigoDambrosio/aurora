using Aurora.Domain.Enums;

namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para sugerencia de reorganización
/// </summary>
public class ScheduleSuggestionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? EventId { get; set; }
    public string? EventTitle { get; set; }
    public SuggestionType Type { get; set; }
    public string TypeDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime? SuggestedDateTime { get; set; }
    public SuggestionStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public DateTime? RespondedAt { get; set; }
    public int ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
}
