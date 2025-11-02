using Aurora.Api.Extensions;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aurora.Api.Controllers;

/// <summary>
/// Endpoints para obtener métricas agregadas del bienestar del usuario.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class WellnessController : ControllerBase
{
    private readonly IWellnessInsightsService _wellnessInsightsService;
    private readonly ILogger<WellnessController> _logger;

    public WellnessController(
        IWellnessInsightsService wellnessInsightsService,
        ILogger<WellnessController> logger)
    {
        _wellnessInsightsService = wellnessInsightsService ?? throw new ArgumentNullException(nameof(wellnessInsightsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene un resumen mensual del estado de ánimo y correlaciones.
    /// </summary>
    /// <param name="year">Año solicitado (por defecto el actual).</param>
    /// <param name="month">Mes solicitado (1-12, por defecto el actual).</param>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(WellnessSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WellnessSummaryDto>> GetMonthlySummary(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var targetMonth = month ?? DateTime.UtcNow.Month;

        if (targetMonth is < 1 or > 12)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Mes inválido",
                Detail = "El mes debe estar entre 1 y 12.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!TryGetAuthenticatedUserId(out var userId, out var errorResult))
        {
            return errorResult!;
        }

        try
        {
            _logger.LogInformation(
                "Solicitud de resumen de bienestar {Year}-{Month} para usuario {UserId}",
                targetYear,
                targetMonth,
                userId);

            var summary = await _wellnessInsightsService.GetMonthlySummaryAsync(userId, targetYear, targetMonth);
            return Ok(summary);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Parámetros inválidos para resumen de bienestar {Year}-{Month}", targetYear, targetMonth);
            return BadRequest(new ProblemDetails
            {
                Title = "Parámetros inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo el resumen de bienestar {Year}-{Month}", targetYear, targetMonth);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    private bool TryGetAuthenticatedUserId(out Guid userId, out ActionResult? errorResult)
    {
        var userIdClaim = User.GetUserId();
        if (!userIdClaim.HasValue)
        {
            _logger.LogWarning("No se encontró el identificador de usuario en el token");
            errorResult = Unauthorized(new ProblemDetails
            {
                Title = "Usuario no autenticado",
                Detail = "No se pudo determinar el usuario autenticado.",
                Status = StatusCodes.Status401Unauthorized
            });
            userId = Guid.Empty;
            return false;
        }

        userId = userIdClaim.Value;
        errorResult = null;
        return true;
    }
}
