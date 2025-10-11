using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Aurora.Application.DTOs.Auth;
using Aurora.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aurora.Api.Controllers;

/// <summary>
/// Endpoints de autenticación para registro, login y logout.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterUserRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            return Created(string.Empty, result);
        }
        catch (ValidationException validationException)
        {
            var errors = validationException.Errors
                .Select(error => error.ErrorMessage)
                .ToArray();

            return BadRequest(new ProblemDetails
            {
                Title = "Datos inválidos",
                Detail = string.Join(" ", errors)
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Registro no permitido",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Inicia sesión con email y contraseña válidos.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ValidationException validationException)
        {
            var errors = validationException.Errors
                .Select(error => error.ErrorMessage)
                .ToArray();

            return BadRequest(new ProblemDetails
            {
                Title = "Datos inválidos",
                Detail = string.Join(" ", errors)
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciales inválidas",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Cierra la sesión actual invalidando el token en el servidor.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var tokenIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrWhiteSpace(tokenIdValue) || !Guid.TryParse(tokenIdValue, out var tokenId))
        {
            _logger.LogWarning("No se encontró JTI en el token. No se pudo cerrar la sesión correctamente.");
            return NoContent();
        }

        await _authService.LogoutAsync(tokenId, cancellationToken);
        return NoContent();
    }
}
