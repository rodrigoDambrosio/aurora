using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Api.Extensions;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aurora.Api.Controllers;

/// <summary>
/// Expone recomendaciones personalizadas basadas en hábitos y estados de ánimo.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IRecommendationService recommendationService,
        ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene una lista de recomendaciones personalizadas para el usuario autenticado.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<RecommendationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<RecommendationDto>>> GetRecommendations(
        [FromQuery] RecommendationRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Solicitud de recomendaciones sin usuario autenticado");
            return Unauthorized(new ProblemDetails
            {
                Title = "Usuario no autenticado",
                Detail = "No se pudo determinar el usuario asociado a la solicitud.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        try
        {
            var recommendations = await _recommendationService.GetRecommendationsAsync(userId, request, cancellationToken);
            return Ok(recommendations);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parámetros inválidos al generar recomendaciones");
            return BadRequest(new ProblemDetails
            {
                Title = "Parámetros inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar recomendaciones");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "No pudimos generar recomendaciones en este momento.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Registra feedback sobre una recomendación para refinar el motor heurístico.
    /// </summary>
    [HttpPost("feedback")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitFeedback(
        [FromBody] RecommendationFeedbackDto feedback,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Feedback de recomendaciones sin usuario autenticado");
            return Unauthorized(new ProblemDetails
            {
                Title = "Usuario no autenticado",
                Detail = "No se pudo determinar el usuario asociado a la solicitud.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        try
        {
            await _recommendationService.RecordFeedbackAsync(userId, feedback, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Feedback inválido");
            return BadRequest(new ProblemDetails
            {
                Title = "Feedback inválido",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Obtiene un resumen del feedback entregado por el usuario en el período solicitado.
    /// </summary>
    [HttpGet("feedback/summary")]
    [ProducesResponseType(typeof(RecommendationFeedbackSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RecommendationFeedbackSummaryDto>> GetFeedbackSummary(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Usuario no autenticado",
                Detail = "No se pudo determinar el usuario asociado a la solicitud.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        if (days <= 0 || days > 180)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Rango inválido",
                Detail = "El parámetro 'days' debe estar entre 1 y 180.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var periodStart = DateTime.UtcNow.AddDays(-days);
        var summary = await _recommendationService
            .GetFeedbackSummaryAsync(userId, periodStart, cancellationToken)
            .ConfigureAwait(false);

        return Ok(summary);
    }
}
