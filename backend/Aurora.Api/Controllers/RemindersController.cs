using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Aurora.Api.Controllers;

/// <summary>
/// Controller para gestión de recordatorios de eventos
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _reminderService;
    private readonly ILogger<RemindersController> _logger;

    public RemindersController(
        IReminderService reminderService,
        ILogger<RemindersController> logger)
    {
        _reminderService = reminderService;
        _logger = logger;
    }

    /// <summary>
    /// Crea un nuevo recordatorio para un evento
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReminderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReminderDto>> CreateReminder([FromBody] CreateReminderDto dto)
    {
        try
        {
            var reminder = await _reminderService.CreateReminderAsync(dto);
            _logger.LogInformation("Recordatorio creado: {ReminderId} para evento {EventId}",
                reminder.Id, reminder.EventId);

            return CreatedAtAction(nameof(GetReminderById), new { id = reminder.Id }, reminder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error al crear recordatorio");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene recordatorios pendientes (próximos a dispararse)
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<ReminderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReminderDto>>> GetPendingReminders()
    {
        var reminders = await _reminderService.GetPendingRemindersAsync();
        return Ok(reminders);
    }

    /// <summary>
    /// Obtiene un recordatorio por su ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReminderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReminderDto>> GetReminderById(Guid id)
    {
        try
        {
            var reminder = await _reminderService.GetReminderByIdAsync(id);
            return Ok(reminder);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = $"Recordatorio con ID {id} no encontrado" });
        }
    }

    /// <summary>
    /// Obtiene todos los recordatorios de un evento específico
    /// </summary>
    [HttpGet("event/{eventId}")]
    [ProducesResponseType(typeof(IEnumerable<ReminderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReminderDto>>> GetRemindersByEventId(Guid eventId)
    {
        var reminders = await _reminderService.GetRemindersByEventIdAsync(eventId);
        return Ok(reminders);
    }

    /// <summary>
    /// Elimina un recordatorio
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReminder(Guid id)
    {
        try
        {
            await _reminderService.DeleteReminderAsync(id);
            _logger.LogInformation("Recordatorio eliminado: {ReminderId}", id);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = $"Recordatorio con ID {id} no encontrado" });
        }
    }

    /// <summary>
    /// Elimina todos los recordatorios del sistema
    /// </summary>
    [HttpDelete("all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAllReminders()
    {
        await _reminderService.DeleteAllRemindersAsync();
        _logger.LogInformation("Todos los recordatorios han sido eliminados");
        return NoContent();
    }

    /// <summary>
    /// Marca un recordatorio como enviado
    /// </summary>
    [HttpPut("{id}/mark-sent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsSent(Guid id)
    {
        try
        {
            await _reminderService.MarkAsSentAsync(id);
            _logger.LogInformation("Recordatorio marcado como enviado: {ReminderId}", id);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = $"Recordatorio con ID {id} no encontrado" });
        }
    }
}
