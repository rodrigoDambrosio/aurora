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
            // En desarrollo, usar usuario demo si no se especifica
            var effectiveUserId = userId ?? DomainConstants.DemoUser.Id;

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
            // En desarrollo, usar usuario demo si no se especifica
            var effectiveUserId = userId ?? DomainConstants.DemoUser.Id;

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
}