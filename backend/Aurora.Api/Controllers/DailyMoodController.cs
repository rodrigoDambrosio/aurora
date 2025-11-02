using Aurora.Api.Extensions;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aurora.Api.Controllers;

/// <summary>
/// Endpoints para gestionar registros diarios de estado de ánimo.
/// </summary>
[ApiController]
[Route("api/moods")]
[Produces("application/json")]
[Authorize]
public class DailyMoodController : ControllerBase
{
    private readonly IDailyMoodService _dailyMoodService;
    private readonly IValidator<UpsertDailyMoodDto> _upsertValidator;
    private readonly ILogger<DailyMoodController> _logger;

    public DailyMoodController(
        IDailyMoodService dailyMoodService,
        IValidator<UpsertDailyMoodDto> upsertValidator,
        ILogger<DailyMoodController> logger)
    {
        _dailyMoodService = dailyMoodService ?? throw new ArgumentNullException(nameof(dailyMoodService));
        _upsertValidator = upsertValidator ?? throw new ArgumentNullException(nameof(upsertValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("monthly")]
    [ProducesResponseType(typeof(MonthlyMoodResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MonthlyMoodResponseDto>> GetMonthlyMoodEntries([FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var targetMonth = month ?? DateTime.UtcNow.Month;

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Usuario no autenticado",
                Detail = "No se pudo determinar el usuario.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        try
        {
            var response = await _dailyMoodService.GetMonthlyMoodEntriesAsync(userId, targetYear, targetMonth);
            return Ok(response);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Parámetros inválidos al solicitar registros de ánimo mensuales");
            return BadRequest(new ProblemDetails
            {
                Title = "Parámetros inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(DailyMoodEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DailyMoodEntryDto>> UpsertMood([FromBody] UpsertDailyMoodDto request)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Usuario no autenticado",
                Detail = "No se pudo determinar el usuario.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var validation = await _upsertValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _dailyMoodService.UpsertDailyMoodAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar el registro de ánimo");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error al guardar el estado de ánimo.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteMood([FromQuery] DateTime date)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Usuario no autenticado",
                Detail = "No se pudo determinar el usuario.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var normalizedDate = date.Date;

        var removed = await _dailyMoodService.DeleteDailyMoodAsync(userId, normalizedDate);
        if (!removed)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Registro no encontrado",
                Detail = "No se encontró un registro de estado de ánimo para la fecha indicada.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }
}
