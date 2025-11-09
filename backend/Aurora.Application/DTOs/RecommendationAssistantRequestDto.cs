using System.Collections.Generic;

namespace Aurora.Application.DTOs;

public class RecommendationAssistantRequestDto
{
    public IList<RecommendationConversationMessageDto> Conversation { get; set; } = new List<RecommendationConversationMessageDto>();

    public int? CurrentMood { get; set; }

    public string? ExternalContext { get; set; }

    public IList<RecommendationDto>? FallbackRecommendations { get; set; }
}

public class RecommendationConversationMessageDto
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

public class RecommendationAssistantChatRequestDto
{
    public IList<RecommendationConversationMessageDto> Conversation { get; set; } = new List<RecommendationConversationMessageDto>();

    public int? CurrentMood { get; set; }

    public string? ExternalContext { get; set; }
}

public class RecommendationAssistantChatResponseDto
{
    public string Message { get; set; } = string.Empty;
}
