using Aurora.Api.Extensions;
using Aurora.Application.DTOs;
using Aurora.Application.Interfaces;
using Aurora.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aurora.Api.Controllers;

/// <summary>
/// Controller para la gestión de categorías de eventos
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventCategoriesController : ControllerBase
{
    private readonly IEventCategoryRepository _eventCategoryRepository;
    private readonly ILogger<EventCategoriesController> _logger;

    public EventCategoriesController(IEventCategoryRepository eventCategoryRepository, ILogger<EventCategoriesController> logger)
    {
        _eventCategoryRepository = eventCategoryRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todas las categorías disponibles para un usuario
    /// </summary>
    /// <param name="userId">ID del usuario (opcional, usa usuario demo si no se especifica)</param>
    /// <returns>Lista de categorías disponibles</returns>
    /// <response code="200">Categorías obtenidas exitosamente</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<EventCategoryDto>>> GetCategories([FromQuery] Guid? userId = null)
    {
        try
        {
            var effectiveUserId = ResolveUserId(userId);

            _logger.LogInformation("Obteniendo categorías para usuario: {UserId}", effectiveUserId);

            var categories = await _eventCategoryRepository.GetAvailableCategoriesForUserAsync(effectiveUserId);

            // Mapear a DTOs
            var categoryDtos = categories.Select(c => new EventCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Color = c.Color,
                Icon = c.Icon,
                IsSystemDefault = c.IsSystemDefault,
                SortOrder = c.SortOrder
            });

            _logger.LogInformation("Se encontraron {CategoryCount} categorías", categoryDtos.Count());

            return Ok(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo categorías del usuario");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Obtiene una categoría específica por su ID
    /// </summary>
    /// <param name="id">ID de la categoría</param>
    /// <returns>Datos de la categoría</returns>
    /// <response code="200">Categoría encontrada</response>
    /// <response code="404">Categoría no encontrada</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventCategoryDto>> GetCategory(Guid id)
    {
        try
        {
            _logger.LogInformation("Obteniendo categoría con ID: {CategoryId}", id);

            var category = await _eventCategoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                _logger.LogWarning("Categoría no encontrada con ID: {CategoryId}", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Categoría no encontrada",
                    Detail = $"No se encontró una categoría con ID {id}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var categoryDto = new EventCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                Icon = category.Icon,
                IsSystemDefault = category.IsSystemDefault,
                SortOrder = category.SortOrder
            };

            return Ok(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo categoría con ID: {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Obtiene las categorías del sistema (predeterminadas)
    /// </summary>
    /// <returns>Lista de categorías del sistema</returns>
    /// <response code="200">Categorías del sistema obtenidas exitosamente</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("system")]
    [ProducesResponseType(typeof(IEnumerable<EventCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<EventCategoryDto>>> GetSystemCategories()
    {
        try
        {
            _logger.LogInformation("Obteniendo categorías del sistema");

            var categories = await _eventCategoryRepository.GetSystemCategoriesAsync();

            // Mapear a DTOs
            var categoryDtos = categories.Select(c => new EventCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Color = c.Color,
                Icon = c.Icon,
                IsSystemDefault = c.IsSystemDefault,
                SortOrder = c.SortOrder
            });

            _logger.LogInformation("Se encontraron {CategoryCount} categorías del sistema", categoryDtos.Count());

            return Ok(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo categorías del sistema");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Obtiene las categorías personalizadas de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario (opcional, usa usuario demo si no se especifica)</param>
    /// <returns>Lista de categorías personalizadas</returns>
    /// <response code="200">Categorías personalizadas obtenidas exitosamente</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("custom")]
    [ProducesResponseType(typeof(IEnumerable<EventCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<EventCategoryDto>>> GetCustomCategories([FromQuery] Guid? userId = null)
    {
        try
        {
            var effectiveUserId = ResolveUserId(userId);

            _logger.LogInformation("Obteniendo categorías personalizadas para usuario: {UserId}", effectiveUserId);

            var categories = await _eventCategoryRepository.GetUserCustomCategoriesAsync(effectiveUserId);

            // Mapear a DTOs
            var categoryDtos = categories.Select(c => new EventCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Color = c.Color,
                Icon = c.Icon,
                IsSystemDefault = c.IsSystemDefault,
                SortOrder = c.SortOrder
            });

            _logger.LogInformation("Se encontraron {CategoryCount} categorías personalizadas", categoryDtos.Count());

            return Ok(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo categorías personalizadas del usuario");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Crea una nueva categoría personalizada para el usuario
    /// </summary>
    /// <param name="createDto">Datos de la nueva categoría</param>
    /// <param name="userId">ID del usuario (opcional, usa usuario autenticado si no se especifica)</param>
    /// <returns>Categoría creada</returns>
    /// <response code="201">Categoría creada exitosamente</response>
    /// <response code="400">Datos inválidos</response>
    /// <response code="409">Ya existe una categoría con ese nombre</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost]
    [ProducesResponseType(typeof(EventCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventCategoryDto>> CreateCategory(
        [FromBody] CreateEventCategoryDto createDto,
        [FromQuery] Guid? userId = null)
    {
        try
        {
            var effectiveUserId = ResolveUserId(userId);

            _logger.LogInformation("Creando categoría para usuario: {UserId}", effectiveUserId);

            // Validar que no exista una categoría con el mismo nombre
            var exists = await _eventCategoryRepository.ExistsCategoryWithNameAsync(
                createDto.Name, effectiveUserId);

            if (exists)
            {
                _logger.LogWarning("Ya existe una categoría con el nombre: {Name}", createDto.Name);
                return Conflict(new ProblemDetails
                {
                    Title = "Categoría duplicada",
                    Detail = $"Ya existe una categoría con el nombre '{createDto.Name}'",
                    Status = StatusCodes.Status409Conflict
                });
            }

            // Obtener el siguiente SortOrder
            var userCategories = await _eventCategoryRepository.GetAvailableCategoriesForUserAsync(effectiveUserId);
            var maxSortOrder = userCategories.Any() ? userCategories.Max(c => c.SortOrder) : 0;

            // Crear la nueva categoría
            var newCategory = new Aurora.Domain.Entities.EventCategory
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Description = createDto.Description,
                Color = createDto.Color,
                Icon = createDto.Icon,
                UserId = effectiveUserId,
                IsSystemDefault = false,
                SortOrder = maxSortOrder + 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _eventCategoryRepository.AddAsync(newCategory);
            await _eventCategoryRepository.SaveChangesAsync();

            var categoryDto = new EventCategoryDto
            {
                Id = newCategory.Id,
                Name = newCategory.Name,
                Description = newCategory.Description,
                Color = newCategory.Color,
                Icon = newCategory.Icon,
                IsSystemDefault = newCategory.IsSystemDefault,
                SortOrder = newCategory.SortOrder,
                UserId = newCategory.UserId,
                CreatedAt = newCategory.CreatedAt,
                UpdatedAt = newCategory.UpdatedAt
            };

            _logger.LogInformation("Categoría creada exitosamente: {CategoryId}", newCategory.Id);

            return CreatedAtAction(nameof(GetCategory), new { id = newCategory.Id }, categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando categoría");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Actualiza una categoría personalizada existente
    /// </summary>
    /// <param name="id">ID de la categoría</param>
    /// <param name="updateDto">Datos actualizados</param>
    /// <param name="userId">ID del usuario (opcional, usa usuario autenticado si no se especifica)</param>
    /// <returns>Categoría actualizada</returns>
    /// <response code="200">Categoría actualizada exitosamente</response>
    /// <response code="400">Datos inválidos</response>
    /// <response code="403">No se puede editar una categoría del sistema</response>
    /// <response code="404">Categoría no encontrada</response>
    /// <response code="409">Ya existe una categoría con ese nombre</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventCategoryDto>> UpdateCategory(
        Guid id,
        [FromBody] UpdateEventCategoryDto updateDto,
        [FromQuery] Guid? userId = null)
    {
        try
        {
            var effectiveUserId = ResolveUserId(userId);

            _logger.LogInformation("Actualizando categoría {CategoryId} para usuario: {UserId}", id, effectiveUserId);

            var category = await _eventCategoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                _logger.LogWarning("Categoría no encontrada: {CategoryId}", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Categoría no encontrada",
                    Detail = $"No se encontró una categoría con ID {id}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Verificar que no sea una categoría del sistema
            if (category.IsSystemDefault)
            {
                _logger.LogWarning("Intento de editar categoría del sistema: {CategoryId}", id);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Operación no permitida",
                    Detail = "No se pueden editar categorías del sistema",
                    Status = StatusCodes.Status403Forbidden
                });
            }

            // Verificar que la categoría pertenece al usuario
            if (category.UserId != effectiveUserId)
            {
                _logger.LogWarning("Usuario {UserId} intentó editar categoría {CategoryId} de otro usuario", effectiveUserId, id);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Operación no permitida",
                    Detail = "No tienes permiso para editar esta categoría",
                    Status = StatusCodes.Status403Forbidden
                });
            }

            // Validar que no exista otra categoría con el mismo nombre
            var exists = await _eventCategoryRepository.ExistsCategoryWithNameAsync(
                updateDto.Name, effectiveUserId, id);

            if (exists)
            {
                _logger.LogWarning("Ya existe otra categoría con el nombre: {Name}", updateDto.Name);
                return Conflict(new ProblemDetails
                {
                    Title = "Categoría duplicada",
                    Detail = $"Ya existe otra categoría con el nombre '{updateDto.Name}'",
                    Status = StatusCodes.Status409Conflict
                });
            }

            // Actualizar la categoría
            category.Name = updateDto.Name;
            category.Description = updateDto.Description;
            category.Color = updateDto.Color;
            category.Icon = updateDto.Icon;
            category.UpdatedAt = DateTime.UtcNow;

            await _eventCategoryRepository.UpdateAsync(category);
            await _eventCategoryRepository.SaveChangesAsync();

            var categoryDto = new EventCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                Icon = category.Icon,
                IsSystemDefault = category.IsSystemDefault,
                SortOrder = category.SortOrder,
                UserId = category.UserId,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            _logger.LogInformation("Categoría actualizada exitosamente: {CategoryId}", id);

            return Ok(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando categoría {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Elimina una categoría personalizada, opcionalmente reasignando sus eventos
    /// </summary>
    /// <param name="id">ID de la categoría</param>
    /// <param name="deleteDto">Configuración de eliminación (ID de categoría para reasignar eventos)</param>
    /// <param name="userId">ID del usuario (opcional, usa usuario autenticado si no se especifica)</param>
    /// <returns>Resultado de la eliminación</returns>
    /// <response code="204">Categoría eliminada exitosamente</response>
    /// <response code="400">Solicitud inválida (ej: tiene eventos y no se especificó reasignación)</response>
    /// <response code="403">No se puede eliminar una categoría del sistema</response>
    /// <response code="404">Categoría no encontrada</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCategory(
        Guid id,
        [FromBody] DeleteEventCategoryDto? deleteDto = null,
        [FromQuery] Guid? userId = null)
    {
        try
        {
            var effectiveUserId = ResolveUserId(userId);

            _logger.LogInformation("Eliminando categoría {CategoryId} para usuario: {UserId}", id, effectiveUserId);

            var category = await _eventCategoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                _logger.LogWarning("Categoría no encontrada: {CategoryId}", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Categoría no encontrada",
                    Detail = $"No se encontró una categoría con ID {id}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Verificar que no sea una categoría del sistema
            if (category.IsSystemDefault)
            {
                _logger.LogWarning("Intento de eliminar categoría del sistema: {CategoryId}", id);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Operación no permitida",
                    Detail = "No se pueden eliminar categorías del sistema",
                    Status = StatusCodes.Status403Forbidden
                });
            }

            // Verificar que la categoría pertenece al usuario
            if (category.UserId != effectiveUserId)
            {
                _logger.LogWarning("Usuario {UserId} intentó eliminar categoría {CategoryId} de otro usuario", effectiveUserId, id);
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Operación no permitida",
                    Detail = "No tienes permiso para eliminar esta categoría",
                    Status = StatusCodes.Status403Forbidden
                });
            }

            // Verificar si tiene eventos asociados
            var eventCount = await _eventCategoryRepository.GetEventCountForCategoryAsync(id);

            if (eventCount > 0)
            {
                if (deleteDto?.ReassignToCategoryId == null)
                {
                    _logger.LogWarning("Intento de eliminar categoría con eventos sin especificar reasignación: {CategoryId}", id);
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Categoría tiene eventos asociados",
                        Detail = $"Esta categoría tiene {eventCount} evento(s) asociado(s). Debes especificar una categoría para reasignarlos.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                // Verificar que la categoría de destino existe y es accesible
                var targetCategory = await _eventCategoryRepository.GetByIdAsync(deleteDto.ReassignToCategoryId.Value);
                if (targetCategory == null ||
                    (!targetCategory.IsSystemDefault && targetCategory.UserId != effectiveUserId))
                {
                    _logger.LogWarning("Categoría de destino inválida: {TargetCategoryId}", deleteDto.ReassignToCategoryId);
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Categoría de destino inválida",
                        Detail = "La categoría especificada para reasignar los eventos no existe o no tienes acceso a ella",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                // Reasignar eventos
                _logger.LogInformation("Reasignando {EventCount} eventos de categoría {FromId} a {ToId}",
                    eventCount, id, deleteDto.ReassignToCategoryId.Value);
                await _eventCategoryRepository.ReassignEventsToAnotherCategoryAsync(id, deleteDto.ReassignToCategoryId.Value);
            }

            // Eliminar la categoría
            await _eventCategoryRepository.DeleteAsync(id);
            await _eventCategoryRepository.SaveChangesAsync();

            _logger.LogInformation("Categoría eliminada exitosamente: {CategoryId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando categoría {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error procesando la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    private Guid ResolveUserId(Guid? requestedUserId)
    {
        if (requestedUserId.HasValue)
        {
            return requestedUserId.Value;
        }

        var authenticatedUserId = User.GetUserId();
        if (authenticatedUserId.HasValue)
        {
            return authenticatedUserId.Value;
        }

        return DomainConstants.DemoUser.Id;
    }
}