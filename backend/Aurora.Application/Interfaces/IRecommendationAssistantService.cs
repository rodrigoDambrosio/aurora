using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Application.DTOs;

namespace Aurora.Application.Interfaces;

public interface IRecommendationAssistantService
{
    Task<IReadOnlyCollection<RecommendationDto>> GenerateConversationalRecommendationsAsync(
        Guid userId,
        RecommendationAssistantRequestDto request,
        CancellationToken cancellationToken = default);

    Task<string> GenerateAssistantReplyAsync(
        Guid userId,
        RecommendationAssistantChatRequestDto request,
        CancellationToken cancellationToken = default);
}
