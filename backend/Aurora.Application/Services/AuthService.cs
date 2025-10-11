using Aurora.Application.DTOs.Auth;
using Aurora.Application.Interfaces;
using Aurora.Application.Options;
using Aurora.Domain.Entities;
using Aurora.Domain.Services;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aurora.Application.Services;

/// <summary>
/// Servicio centralizado para gestionar registro, login y logout de usuarios.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEventCategoryRepository _categoryRepository;
    private readonly IValidator<RegisterUserRequestDto> _registerValidator;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthService>? _logger;

    public AuthService(
        IUserRepository userRepository,
        IUserSessionRepository sessionRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEventCategoryRepository categoryRepository,
        IValidator<RegisterUserRequestDto> registerValidator,
        IValidator<LoginRequestDto> loginValidator,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthService>? logger = null)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _registerValidator = registerValidator ?? throw new ArgumentNullException(nameof(registerValidator));
        _loginValidator = loginValidator ?? throw new ArgumentNullException(nameof(loginValidator));
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedEmail = NormalizeEmail(request.Email);

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe una cuenta con ese email. Prueba iniciando sesión.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            IsEmailVerified = false,
            LastLoginAt = null
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        await EnsureDefaultCategoriesAsync(user);

        return await IssueSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedEmail = NormalizeEmail(request.Email);
        var existingUser = await _userRepository.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (existingUser is null)
        {
            throw new UnauthorizedAccessException("Credenciales inválidas. Verifica tus datos e inténtalo nuevamente.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, existingUser.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciales inválidas. Verifica tus datos e inténtalo nuevamente.");
        }

        existingUser.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(existingUser);
        await _userRepository.SaveChangesAsync();

        // Opcionalmente, invalidar sesiones caducadas
        await _sessionRepository.InvalidateSessionsForUserAsync(existingUser.Id, cancellationToken);

        return await IssueSessionAsync(existingUser, cancellationToken);
    }

    public async Task LogoutAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByTokenIdAsync(tokenId, cancellationToken);
        if (session is null)
        {
            _logger?.LogInformation("Se intentó cerrar sesión con token inexistente {TokenId}", tokenId);
            return;
        }

        if (!session.IsRevoked)
        {
            session.RevokedAtUtc = DateTime.UtcNow;
            session.RevokedReason = "logout";
            session.IsActive = false;

            await _sessionRepository.UpdateAsync(session);
            await _sessionRepository.SaveChangesAsync();
        }
    }

    private async Task<AuthResponseDto> IssueSessionAsync(User user, CancellationToken cancellationToken)
    {
        var expiresAt = DateTime.UtcNow.AddDays(_jwtOptions.SessionDurationDays);
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenId = Guid.NewGuid(),
            ExpiresAtUtc = expiresAt,
            IsActive = true
        };

        await _sessionRepository.AddAsync(session);
        await _sessionRepository.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user, session);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAt,
            User = new UserSummaryDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified
            }
        };
    }

    private async Task EnsureDefaultCategoriesAsync(User user)
    {
        var existingDefaults = await _categoryRepository.GetDefaultCategoriesAsync(user.Id);
        if (existingDefaults.Any())
        {
            return;
        }

        var defaultCategories = DefaultEventCategories.CreateSystemCategories(user.Id);

        foreach (var category in defaultCategories)
        {
            await _categoryRepository.AddAsync(category);
        }

        await _categoryRepository.SaveChangesAsync();
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
