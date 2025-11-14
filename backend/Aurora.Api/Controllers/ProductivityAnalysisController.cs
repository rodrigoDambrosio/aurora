using Aurora.Api.Extensions;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aurora.Api.Controllers;

/// <summary>
/// Controlador para análisis de productividad del usuario
/// </summary>
[ApiController]
[Route("api/user")]
public class ProductivityAnalysisController : ControllerBase
{
    private readonly IProductivityAnalysisService _productivityAnalysisService;
    private readonly ILogger<ProductivityAnalysisController> _logger;

    public ProductivityAnalysisController(
        IProductivityAnalysisService productivityAnalysisService,
        ILogger<ProductivityAnalysisController> logger)
    {
        _productivityAnalysisService = productivityAnalysisService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el análisis de productividad del usuario
    /// </summary>
    /// <param name="periodDays">Cantidad de días hacia atrás a analizar (por defecto 30)</param>
    /// <returns>Análisis completo de productividad</returns>
    [HttpGet("productivity-analysis")]
    [ProducesResponseType(typeof(ProductivityAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductivityAnalysisDto>> GetProductivityAnalysis(
        [FromQuery] int periodDays = 30,
        [FromQuery] int? timezoneOffsetMinutes = null)
    {
        try
        {
            if (periodDays < 1 || periodDays > 365)
            {
                return BadRequest(new { error = "El período debe estar entre 1 y 365 días" });
            }

            var userIdClaim = User.GetUserId();

            if (!userIdClaim.HasValue)
            {
                _logger.LogWarning("No se encontró el identificador de usuario en el token al solicitar el análisis de productividad");
                return Unauthorized(new ProblemDetails
                {
                    Title = "Usuario no autenticado",
                    Detail = "No se pudo determinar el usuario autenticado.",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            var userId = DevelopmentUserService.GetCurrentUserId(userIdClaim);

            _logger.LogInformation(
                "Obteniendo análisis de productividad para usuario {UserId} con período de {Days} días y offset {Offset}",
                userId,
                periodDays,
                timezoneOffsetMinutes);

            var analysis = await _productivityAnalysisService.AnalyzeProductivityAsync(userId, periodDays, timezoneOffsetMinutes);

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener análisis de productividad");
            return StatusCode(500, new { error = "Error al procesar el análisis de productividad" });
        }
    }
}
