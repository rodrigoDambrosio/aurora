using Aurora.Api.Extensions;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Aurora.Api.Controllers;

/// <summary>
/// Controller para la gestión de eventos del calendario
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IAIValidationService _aiValidationService;
    private readonly IEventCategoryRepository _eventCategoryRepository;
    private readonly IUserService _userService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventService eventService,
        IAIValidationService aiValidationService,
        IEventCategoryRepository eventCategoryRepository,
        IUserService userService,
        ILogger<EventsController> logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _aiValidationService = aiValidationService ?? throw new ArgumentNullException(nameof(aiValidationService));
        _eventCategoryRepository = eventCategoryRepository ?? throw new ArgumentNullException(nameof(eventCategoryRepository));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

    /// <summary>
    /// Obtiene eventos de una semana específica para el usuario autenticado
    /// </summary>
    /// <param name="request">Parámetros de la consulta semanal</param>
    /// <param name="categoryId">ID de categoría para filtrar (opcional)</param>
    /// <returns>Eventos de la semana con categorías disponibles</returns>
    /// <response code="200">Eventos obtenidos exitosamente</response>
    /// <response code="400">Parámetros de consulta inválidos</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("weekly")]
    [ProducesResponseType(typeof(WeeklyEventsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WeeklyEventsResponseDto>> GetWeeklyEvents(
        [FromBody] WeeklyEventsRequestDto request,
        [FromQuery] Guid? categoryId = null)
    {
        try
        {
            if (!TryGetAuthenticatedUserId(out var userId, out var errorResult))
            {
                return errorResult!;
            }

            _logger.LogInformation(
                "Obteniendo eventos semanales para usuario: {UserId}, fecha: {WeekStart}, categoría: {CategoryId}",
                userId, request.WeekStart, categoryId);

            var response = await _eventService.GetWeeklyEventsAsync(userId, request.WeekStart, categoryId);

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
    /// Obtiene eventos de un mes específico para el usuario autenticado
    /// </summary>
    /// <param name="year">Año del mes a consultar</param>
    /// <param name="month">Mes a consultar (1-12)</param>
    /// <param name="categoryId">ID de categoría para filtrar (opcional)</param>
    /// <returns>Eventos del mes con categorías disponibles</returns>
    /// <response code="200">Eventos obtenidos exitosamente</response>
    /// <response code="400">Parámetros de consulta inválidos</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(WeeklyEventsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WeeklyEventsResponseDto>> GetMonthlyEvents(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] Guid? categoryId = null)
    {
        try
        {
            // Usar fecha actual si no se especifican año/mes
            var currentYear = year ?? DateTime.Now.Year;
            var currentMonth = month ?? DateTime.Now.Month;

            if (currentMonth < 1 || currentMonth > 12)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Mes inválido",
                    Detail = "El mes debe estar entre 1 y 12",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (!TryGetAuthenticatedUserId(out var userId, out var errorResult))
            {
                return errorResult!;
            }

            _logger.LogInformation(
                "Obteniendo eventos mensuales para usuario: {UserId}, {Year}-{Month}, categoría: {CategoryId}",
                userId, currentYear, currentMonth, categoryId);

            var response = await _eventService.GetMonthlyEventsAsync(userId, currentYear, currentMonth, categoryId);

            _logger.LogInformation("Se encontraron {EventCount} eventos para el mes", response.Events.Count());

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo eventos mensuales");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Obtiene todos los eventos del usuario autenticado
    /// </summary>
    /// <returns>Lista de eventos del usuario</returns>
    /// <response code="200">Eventos obtenidos exitosamente</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
    {
        try
        {
            if (!TryGetAuthenticatedUserId(out var userId, out var errorResult))
            {
                return errorResult!;
            }

            _logger.LogInformation("Obteniendo todos los eventos para usuario: {UserId}", userId);

            // Por ahora obtener eventos de un rango amplio (último año y próximo año)
            var startDate = DateTime.UtcNow.AddYears(-1);
            var endDate = DateTime.UtcNow.AddYears(1);

            var events = await _eventService.GetEventsByDateRangeAsync(userId, startDate, endDate);

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
            if (!TryGetAuthenticatedUserId(out var userId, out var errorResult))
            {
                return errorResult!;
            }

            _logger.LogInformation("Obteniendo evento con ID: {EventId} para usuario: {UserId}", id, userId);

            var eventDto = await _eventService.GetEventAsync(id, userId);

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
    /// <response code="400">Datos de entrada inválidos</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventDto createEventDto)
    {
        try
        {
            if (!TryGetAuthenticatedUserId(out var userId, out var errorResult))
            {
                return errorResult!;
            }

            _logger.LogInformation(
                "Creando nuevo evento: {Title} para usuario: {UserId}, categoría: {CategoryId}",
                createEventDto.Title, userId, createEventDto.EventCategoryId);

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
    /// Ejecuta un análisis con IA sobre los datos de un evento sin crearlo
    /// </summary>
    /// <param name="createEventDto">Datos del evento a validar</param>
    /// <returns>Resultado del análisis de IA</returns>
    /// <response code="200">Análisis ejecutado exitosamente</response>
    /// <response code="400">Datos de entrada inválidos</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(AIValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AIValidationResult>> ValidateEvent([FromBody] CreateEventDto createEventDto)
    {
        try
        {
            if (!TryGetAuthenticatedUserId(out var userId, out var errorResult))
            {
                return errorResult!;
            }

            _logger.LogInformation(
                "Ejecutando validación manual con IA para evento: {Title} del usuario: {UserId}",
                createEventDto.Title,
                userId);

            var contextStartDate = createEventDto.StartDate.AddDays(-1);
            var contextEndDate = createEventDto.StartDate.AddDays(7);

            var existingEvents = await _eventService.GetEventsByDateRangeAsync(userId, contextStartDate, contextEndDate);
            _logger.LogInformation("Contexto cargado con {EventCount} eventos", existingEvents.Count());

            AIValidationResult validation;

            try
            {
                validation = await _aiValidationService.ValidateEventCreationAsync(createEventDto, userId, existingEvents);
            }
            catch (Exception aiEx)
            {
                _logger.LogError(aiEx, "La validación de IA falló; aplicando validación básica de respaldo");
                validation = RunFallbackValidation(createEventDto, existingEvents);
            }

            return Ok(validation);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Datos inválidos para validación manual: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Datos inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando validación manual de IA");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Parsea texto en lenguaje natural a un evento estructurado usando IA
    /// </summary>
    /// <param name="request">Texto en lenguaje natural</param>
    /// <returns>Evento parseado y validación de IA</returns>
    /// <response code="200">Texto parseado exitosamente</response>
    /// <response code="400">Texto inválido o no se pudo parsear</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("from-text")]
    [ProducesResponseType(typeof(ParseNaturalLanguageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ParseNaturalLanguageResponseDto>> ParseFromText(
        [FromBody] ParseNaturalLanguageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Parseando texto natural: {Text}", request.Text);

            if (!TryGetAuthenticatedUserId(out var userId, out var errorResult))
            {
                return errorResult!;
            }

            // Obtener categorías disponibles
            var categories = await _eventCategoryRepository.GetAvailableCategoriesForUserAsync(userId);
            var categoryDtos = categories.Select(c => new EventCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Color = c.Color,
                Icon = c.Icon,
                IsSystemDefault = c.IsSystemDefault,
                SortOrder = c.SortOrder
            }).ToList();

            _logger.LogInformation("Categorías disponibles: {CategoryCount}", categoryDtos.Count);

            // Obtener eventos cercanos para dar contexto a la IA
            var now = DateTime.UtcNow;
            var contextStartDate = now.AddDays(-1);
            var contextEndDate = now.AddDays(7);

            var existingEvents = await _eventService.GetEventsByDateRangeAsync(userId, contextStartDate, contextEndDate);
            _logger.LogInformation("Contexto: {EventCount} eventos existentes", existingEvents.Count());

            // Obtener preferencias del usuario para recomendaciones personalizadas (PLAN-131)
            var userPreferences = await _userService.GetPreferencesAsync(userId, cancellationToken);
            _logger.LogInformation("Preferencias cargadas: WorkDays={WorkDays}, WorkHours={WorkStart}-{WorkEnd}, Reminders={Reminder}min",
                userPreferences.WorkDaysOfWeek?.Count ?? 0,
                userPreferences.WorkStartTime,
                userPreferences.WorkEndTime,
                userPreferences.DefaultReminderMinutes);

            // Parsear el texto con IA (incluyendo análisis)
            var parseResult = await _aiValidationService.ParseNaturalLanguageAsync(
                request.Text,
                userId,
                categoryDtos,
                request.TimezoneOffsetMinutes,
                existingEvents,
                userPreferences);  // ← PLAN-131: Pasar preferencias a la IA

            _logger.LogInformation("Evento parseado: {Title} - {StartDate}", parseResult.Event.Title, parseResult.Event.StartDate);

            if (parseResult.Validation == null)
            {
                _logger.LogWarning("La respuesta de la IA no incluyó análisis; aplicando validación básica");
                parseResult.Validation = RunFallbackValidation(parseResult.Event, existingEvents);
            }
            else
            {
                // Siempre verificar conflictos explícitos aunque la IA haya respondido
                var fallbackValidation = RunFallbackValidation(parseResult.Event, existingEvents);

                // Si el fallback detectó un conflicto crítico, fusionar con la validación de la IA
                if (fallbackValidation.Severity == AIValidationSeverity.Critical)
                {
                    parseResult.Validation = MergeValidations(parseResult.Validation, fallbackValidation);
                    _logger.LogWarning("Se detectó conflicto crítico. Severidad actualizada a Critical");
                }
            }

            _logger.LogInformation(
                "Validación: {IsApproved} - {Message} (Usó IA: {UsedAi})",
                parseResult.Validation.IsApproved,
                parseResult.Validation.RecommendationMessage,
                parseResult.Validation.UsedAi);

            parseResult.Success = true;
            return Ok(parseResult);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("No se pudo parsear el texto: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Texto no válido",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parseando texto natural");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando el texto",
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
            if (!TryGetAuthenticatedUserId(out var userId, out var errorResult))
            {
                return errorResult!;
            }

            _logger.LogInformation("Actualizando evento con ID: {EventId} para usuario: {UserId}", id, userId);

            var updatedEvent = await _eventService.UpdateEventAsync(id, userId, updateEventDto);

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
            if (!TryGetAuthenticatedUserId(out var userId, out var errorResult))
            {
                return errorResult!;
            }

            _logger.LogInformation("Eliminando evento con ID: {EventId} para usuario: {UserId}", id, userId);

            var result = await _eventService.DeleteEventAsync(id, userId);

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

    private static AIValidationSeverity MaxSeverity(AIValidationSeverity first, AIValidationSeverity second)
    {
        return (AIValidationSeverity)Math.Max((int)first, (int)second);
    }

    private AIValidationResult MergeValidations(AIValidationResult aiValidation, AIValidationResult fallbackValidation)
    {
        // Si el fallback detectó problemas críticos, priorizarlos
        if (fallbackValidation.Severity == AIValidationSeverity.Critical)
        {
            return new AIValidationResult
            {
                IsApproved = false,
                Severity = AIValidationSeverity.Critical,
                RecommendationMessage = fallbackValidation.RecommendationMessage +
                    (aiValidation.RecommendationMessage != null && aiValidation.RecommendationMessage.Length > 0
                        ? " " + aiValidation.RecommendationMessage
                        : ""),
                Suggestions = (fallbackValidation.Suggestions ?? new List<string>())
                    .Concat(aiValidation.Suggestions ?? new List<string>())
                    .Distinct()
                    .ToList(),
                UsedAi = aiValidation.UsedAi
            };
        }

        // Si no hay conflictos críticos, mantener la validación de la IA
        return aiValidation;
    }

    private AIValidationResult RunFallbackValidation(CreateEventDto createEventDto, IEnumerable<EventDto>? existingEvents)
    {
        // Validación crítica: fin anterior al inicio
        if (createEventDto.EndDate <= createEventDto.StartDate)
        {
            return new AIValidationResult
            {
                IsApproved = false,
                Severity = AIValidationSeverity.Critical,
                RecommendationMessage = "La hora de fin debe ser posterior a la hora de inicio.",
                Suggestions = new List<string>
                {
                    "Revisa la duración del evento y ajusta la hora de finalización."
                },
                UsedAi = false
            };
        }

        var issues = new List<string>();
        var suggestions = new List<string>();
        var severity = AIValidationSeverity.Info;

        if (!createEventDto.IsAllDay)
        {
            var duration = createEventDto.EndDate - createEventDto.StartDate;
            if (duration > TimeSpan.FromHours(12))
            {
                severity = MaxSeverity(severity, AIValidationSeverity.Warning);
                issues.Add("El evento dura más de 12 horas seguidas.");
                suggestions.Add("Divide el evento en bloques más cortos o márcalo como 'Todo el día'.");
            }
        }

        var contextEvents = existingEvents?.ToList() ?? new List<EventDto>();
        if (contextEvents.Any())
        {
            var overlappingEvents = contextEvents.Where(e =>
                e.StartDate < createEventDto.EndDate &&
                createEventDto.StartDate < e.EndDate).ToList();

            if (overlappingEvents.Any())
            {
                severity = MaxSeverity(severity, AIValidationSeverity.Critical);

                // Crear mensaje detallado con los eventos que se superponen
                var overlappingDetails = overlappingEvents.Select(e =>
                {
                    var eventStart = e.StartDate.ToString("HH:mm");
                    var eventEnd = e.EndDate.ToString("HH:mm");
                    return $"\"{e.Title}\" ({eventStart} - {eventEnd})";
                }).ToList();

                if (overlappingEvents.Count == 1)
                {
                    issues.Add($"⚠️ CONFLICTO DE HORARIO: Este evento se superpone con {overlappingDetails[0]}");
                }
                else
                {
                    issues.Add($"⚠️ CONFLICTO DE HORARIO: Este evento se superpone con {overlappingEvents.Count} eventos: {string.Join(", ", overlappingDetails)}");
                }

                suggestions.Add("Cambia la fecha/hora del evento o ajusta la duración para evitar el conflicto.");
                suggestions.Add("Considera reprogramar uno de los eventos en conflicto.");
            }
        }

        var message = issues.Count > 0
            ? string.Join(" ", issues)
            : "Validación básica sin IA: no se detectaron problemas importantes.";

        return new AIValidationResult
        {
            IsApproved = issues.Count == 0,
            Severity = severity,
            RecommendationMessage = message,
            Suggestions = issues.Count > 0 ? suggestions : new List<string>(),
            UsedAi = false
        };
    }

    /// <summary>
    /// Genera un plan multi-día estructurado a partir de un objetivo de alto nivel usando IA
    /// </summary>
    /// <param name="request">Solicitud con el objetivo y preferencias del plan</param>
    /// <returns>Plan generado con eventos estructurados</returns>
    /// <response code="200">Plan generado exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="500">Error al generar el plan</response>
    [HttpPost("generate-plan")]
    [ProducesResponseType(typeof(GeneratePlanResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GeneratePlanResponseDto>> GeneratePlan([FromBody] GeneratePlanRequestDto request)
    {
        try
        {
            if (!TryGetAuthenticatedUserId(out var userId, out var errorResult))
            {
                return errorResult!;
            }

            if (string.IsNullOrWhiteSpace(request.Goal))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Objetivo requerido",
                    Detail = "Debe proporcionar un objetivo para generar el plan.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation(
                "Generando plan multi-día para usuario: {UserId}, objetivo: {Goal}",
                userId, request.Goal);

            // Obtener categorías disponibles del usuario
            var categories = await _eventCategoryRepository.GetAvailableCategoriesForUserAsync(userId);
            var categoryDtos = categories.Select(c => new EventCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Color = c.Color,
                Description = c.Description,
                SortOrder = c.SortOrder
            }).ToList();

            _logger.LogInformation(
                "Categorías disponibles para el usuario {UserId}: {CategoryCount} - IDs: {CategoryIds}",
                userId, categoryDtos.Count, string.Join(", ", categoryDtos.Select(c => $"{c.Name}({c.Id})")));

            // Obtener eventos existentes para contexto (próximos 90 días)
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(90);
            var existingEvents = await _eventService.GetEventsByDateRangeAsync(userId, startDate, endDate);

            // TODO PLAN-131: Obtener preferencias del usuario cuando estén implementadas
            // var userPreferences = await _userService.GetUserPreferencesAsync(userId);

            // Generar el plan con IA
            var planResponse = await _aiValidationService.GeneratePlanAsync(
                request,
                userId,
                categoryDtos,
                existingEvents,
                userPreferences: null);

            _logger.LogInformation(
                "Plan generado exitosamente: {PlanTitle} - {TotalSessions} sesiones en {DurationWeeks} semanas",
                planResponse.PlanTitle, planResponse.TotalSessions, planResponse.DurationWeeks);

            if (planResponse.HasPotentialConflicts)
            {
                _logger.LogWarning(
                    "El plan generado tiene {ConflictCount} posibles conflictos",
                    planResponse.ConflictWarnings.Count);
            }

            return Ok(planResponse);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Parámetros inválidos en GeneratePlan: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Parámetros inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error al generar plan para usuario: {UserId}", User.GetUserId());
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error al generar plan",
                Detail = "No se pudo generar el plan con la IA. Por favor, intente nuevamente.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al generar plan para usuario: {UserId}", User.GetUserId());
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno del servidor",
                Detail = "Ocurrió un error al procesar la solicitud.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}