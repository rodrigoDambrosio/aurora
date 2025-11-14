using Aurora.Api.Extensions;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aurora.Api.Controllers;

/// <summary>
/// Controller para gestionar sugerencias de reorganización del calendario
/// </summary>
[ApiController]
[Route("api/schedule-suggestions")]
public class ScheduleSuggestionsController : ControllerBase
{
    private readonly IScheduleSuggestionService _suggestionService;

    public ScheduleSuggestionsController(IScheduleSuggestionService suggestionService)
    {
        _suggestionService = suggestionService;
    }

    /// <summary>
    /// Obtiene las sugerencias pendientes para el usuario autenticado
    /// </summary>
    /// <returns>Lista de sugerencias pendientes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ScheduleSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ScheduleSuggestionDto>>> GetPendingSuggestions()
    {
        var userId = User.GetUserId() ?? Domain.Constants.DomainConstants.DemoUser.Id;
        var suggestions = await _suggestionService.GetPendingSuggestionsAsync(userId);
        return Ok(suggestions);
    }

    /// <summary>
    /// Genera nuevas sugerencias analizando el calendario del usuario
    /// </summary>
    /// <param name="timezoneOffsetMinutes">Offset de zona horaria en minutos (ej: -180 para GMT-3)</param>
    /// <returns>Lista de sugerencias generadas</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(IEnumerable<ScheduleSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ScheduleSuggestionDto>>> GenerateSuggestions([FromQuery] int? timezoneOffsetMinutes = null)
    {
        var userId = User.GetUserId() ?? Domain.Constants.DomainConstants.DemoUser.Id;
        var offset = timezoneOffsetMinutes ?? -180; // Default GMT-3 (Argentina)
        var suggestions = await _suggestionService.GenerateSuggestionsAsync(userId, offset);
        return Ok(suggestions);
    }

    /// <summary>
    /// Responde a una sugerencia específica (aceptar/rechazar/posponer)
    /// </summary>
    /// <param name="id">ID de la sugerencia</param>
    /// <param name="response">Respuesta del usuario</param>
    /// <returns>Sugerencia actualizada</returns>
    [HttpPost("{id}/respond")]
    [ProducesResponseType(typeof(ScheduleSuggestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduleSuggestionDto>> RespondToSuggestion(
        Guid id,
        [FromBody] RespondToSuggestionDto response)
    {
        try
        {
            var userId = User.GetUserId() ?? Domain.Constants.DomainConstants.DemoUser.Id;
            var suggestion = await _suggestionService.RespondToSuggestionAsync(id, response, userId);
            return Ok(suggestion);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
