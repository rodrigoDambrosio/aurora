using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aurora.Api.Controllers;

/// <summary>
/// Controller para la gestión de eventos del calendario
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IAIValidationService _aiValidationService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventService eventService,
        IAIValidationService aiValidationService,
        ILogger<EventsController> logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _aiValidationService = aiValidationService ?? throw new ArgumentNullException(nameof(aiValidationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene eventos de una semana específica para un usuario
    /// </summary>
    /// <param name="request">Parámetros de la consulta semanal</param>
    /// <returns>Eventos de la semana con categorías disponibles</returns>
    /// <response code="200">Eventos obtenidos exitosamente</response>
    /// <response code="400">Parámetros de consulta inválidos</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("weekly")]
    [ProducesResponseType(typeof(WeeklyEventsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WeeklyEventsResponseDto>> GetWeeklyEvents([FromBody] WeeklyEventsRequestDto request)
    {
        try
        {
            _logger.LogInformation("Obteniendo eventos semanales para fecha: {WeekStart}", request.WeekStart);

            // En desarrollo, usar usuario demo si no se especifica
            var userId = request.UserId ?? DomainConstants.DemoUser.Id;

            var response = await _eventService.GetWeeklyEventsAsync(userId, request.WeekStart);

            _logger.LogInformation("Se encontraron {EventCount} eventos para la semana", response.Events.Count());

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Parámetros inválidos en GetWeeklyEvents: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Parámetros inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo eventos semanales");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Obtiene todos los eventos de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario (opcional, usa usuario demo si no se especifica)</param>
    /// <returns>Lista de eventos del usuario</returns>
    /// <response code="200">Eventos obtenidos exitosamente</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents([FromQuery] Guid? userId = null)
    {
        try
        {
            // En desarrollo, usar usuario demo si no se especifica
            var effectiveUserId = userId ?? DomainConstants.DemoUser.Id;

            _logger.LogInformation("Obteniendo todos los eventos para usuario: {UserId}", effectiveUserId);

            // Por ahora obtener eventos de un rango amplio (último año y próximo año)
            var startDate = DateTime.UtcNow.AddYears(-1);
            var endDate = DateTime.UtcNow.AddYears(1);

            var events = await _eventService.GetEventsByDateRangeAsync(effectiveUserId, startDate, endDate);

            _logger.LogInformation("Se encontraron {EventCount} eventos", events.Count());

            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo eventos del usuario");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Obtiene un evento específico por su ID
    /// </summary>
    /// <param name="id">ID del evento</param>
    /// <returns>Datos del evento</returns>
    /// <response code="200">Evento encontrado</response>
    /// <response code="404">Evento no encontrado</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventDto>> GetEvent(Guid id)
    {
        try
        {
            _logger.LogInformation("Obteniendo evento con ID: {EventId}", id);

            var eventDto = await _eventService.GetEventAsync(id, DomainConstants.DemoUser.Id);

            if (eventDto == null)
            {
                _logger.LogWarning("Evento no encontrado con ID: {EventId}", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Evento no encontrado",
                    Detail = $"No se encontró un evento con ID {id}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(eventDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo evento con ID: {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Crea un nuevo evento
    /// </summary>
    /// <param name="createEventDto">Datos del evento a crear</param>
    /// <returns>Evento creado</returns>
    /// <response code="201">Evento creado exitosamente</response>
    /// <response code="400">Datos de entrada inválidos o recomendación de IA</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventDto createEventDto)
    {
        try
        {
            _logger.LogInformation("Creando nuevo evento: {Title}", createEventDto.Title);

            // En desarrollo, usar usuario demo si no se especifica
            var userId = DomainConstants.DemoUser.Id;

            // 1. Obtener eventos cercanos para dar contexto a la IA
            // Buscar eventos desde 1 día antes hasta 1 semana después del evento a crear
            var contextStartDate = createEventDto.StartDate.AddDays(-1);
            var contextEndDate = createEventDto.StartDate.AddDays(7);
            
            _logger.LogInformation("Obteniendo contexto del calendario para validación de IA");
            var existingEvents = await _eventService.GetEventsByDateRangeAsync(userId, contextStartDate, contextEndDate);
            
            _logger.LogInformation("Se encontraron {EventCount} eventos en el rango de contexto", existingEvents.Count());

            // 2. Validar el evento con IA usando el contexto del calendario
            _logger.LogInformation("Validando evento con IA usando contexto del calendario");
            var aiValidation = await _aiValidationService.ValidateEventCreationAsync(
                createEventDto, 
                userId, 
                existingEvents);

            // 3. Si la IA no aprueba el evento, retornar recomendación
            if (!aiValidation.IsApproved)
            {
                _logger.LogWarning("IA no aprobó el evento: {Message}", aiValidation.RecommendationMessage);
                
                return BadRequest(new ProblemDetails
                {
                    Title = "Recomendación de IA",
                    Detail = aiValidation.RecommendationMessage,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions =
                    {
                        ["severity"] = aiValidation.Severity.ToString(),
                        ["suggestions"] = aiValidation.Suggestions ?? new List<string>()
                    }
                });
            }

            // 4. Si la IA aprueba, crear el evento
            _logger.LogInformation("IA aprobó el evento, procediendo a crear");
            var createdEvent = await _eventService.CreateEventAsync(userId, createEventDto);

            _logger.LogInformation("Evento creado exitosamente con ID: {EventId}", createdEvent.Id);

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = createdEvent.Id },
                createdEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Datos inválidos para crear evento: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Datos inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando evento");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Actualiza un evento existente
    /// </summary>
    /// <param name="id">ID del evento a actualizar</param>
    /// <param name="updateEventDto">Datos actualizados del evento</param>
    /// <returns>Evento actualizado</returns>
    /// <response code="200">Evento actualizado exitosamente</response>
    /// <response code="400">Datos de entrada inválidos</response>
    /// <response code="404">Evento no encontrado</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventDto>> UpdateEvent(Guid id, [FromBody] CreateEventDto updateEventDto)
    {
        try
        {
            _logger.LogInformation("Actualizando evento con ID: {EventId}", id);

            var updatedEvent = await _eventService.UpdateEventAsync(id, DomainConstants.DemoUser.Id, updateEventDto);

            if (updatedEvent == null)
            {
                _logger.LogWarning("Evento no encontrado para actualizar con ID: {EventId}", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Evento no encontrado",
                    Detail = $"No se encontró un evento con ID {id}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Evento actualizado exitosamente con ID: {EventId}", id);

            return Ok(updatedEvent);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Datos inválidos para actualizar evento: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Datos inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando evento con ID: {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Elimina un evento (soft delete)
    /// </summary>
    /// <param name="id">ID del evento a eliminar</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="204">Evento eliminado exitosamente</response>
    /// <response code="404">Evento no encontrado</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteEvent(Guid id)
    {
        try
        {
            _logger.LogInformation("Eliminando evento con ID: {EventId}", id);

            var result = await _eventService.DeleteEventAsync(id, DomainConstants.DemoUser.Id);

            if (!result)
            {
                _logger.LogWarning("Evento no encontrado para eliminar con ID: {EventId}", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Evento no encontrado",
                    Detail = $"No se encontró un evento con ID {id}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Evento eliminado exitosamente con ID: {EventId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando evento con ID: {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}