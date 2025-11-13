using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aurora.Api.Controllers;

/// <summary>
/// API para sugerencias de autocuidado
/// </summary>
[ApiController]
[Route("api/recommendations/self-care")]
public class SelfCareController : ControllerBase
{
    private readonly ISelfCareService _selfCareService;
    private readonly ILogger<SelfCareController> _logger;

    public SelfCareController(
        ISelfCareService selfCareService,
        ILogger<SelfCareController> logger)
    {
        _selfCareService = selfCareService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene sugerencias personalizadas de autocuidado
    /// </summary>
    /// <param name="request">Contexto para personalización</param>
    /// <returns>Lista de 3-5 sugerencias con scoring</returns>
    [HttpPost]
    public async Task<ActionResult<IEnumerable<SelfCareRecommendationDto>>> GetRecommendations(
        [FromBody] SelfCareRequestDto request)
    {
        try
        {
            var userId = DevelopmentUserService.GetCurrentUserId();

            // Validar request
            if (request.Count < 1 || request.Count > 10)
            {
                return BadRequest(new { error = "El número de sugerencias debe estar entre 1 y 10" });
            }

            if (request.CurrentMood.HasValue && (request.CurrentMood.Value < 1 || request.CurrentMood.Value > 5))
            {
                return BadRequest(new { error = "El mood debe estar entre 1 y 5" });
            }

            var recommendations = await _selfCareService.GetRecommendationsAsync(userId, request);

            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo sugerencias de autocuidado");

            // Fallback a sugerencias genéricas
            var genericSuggestions = _selfCareService.GetGenericRecommendations(request.Count);
            return Ok(genericSuggestions);
        }
    }

    /// <summary>
    /// Registra feedback sobre una sugerencia (para aprendizaje)
    /// </summary>
    /// <param name="feedback">Acción tomada y resultado</param>
    [HttpPost("feedback")]
    public async Task<ActionResult> RegisterFeedback([FromBody] SelfCareFeedbackDto feedback)
    {
        try
        {
            var userId = DevelopmentUserService.GetCurrentUserId();

            await _selfCareService.RegisterFeedbackAsync(userId, feedback);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando feedback de autocuidado");
            return StatusCode(500, new { error = "Error procesando feedback" });
        }
    }

    /// <summary>
    /// Obtiene sugerencias genéricas (fallback sin personalización)
    /// </summary>
    /// <param name="count">Número de sugerencias (default 5)</param>
    [HttpGet("generic")]
    public ActionResult<IEnumerable<SelfCareRecommendationDto>> GetGenericRecommendations(
        [FromQuery] int count = 5)
    {
        if (count < 1 || count > 10)
        {
            return BadRequest(new { error = "El número de sugerencias debe estar entre 1 y 10" });
        }

        var suggestions = _selfCareService.GetGenericRecommendations(count);
        return Ok(suggestions);
    }
}
