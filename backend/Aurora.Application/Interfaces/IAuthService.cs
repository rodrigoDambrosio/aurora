using Aurora.Application.DTOs.Auth;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Contrato para la lógica de autenticación de usuarios.
/// </summary>
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task LogoutAsync(Guid tokenId, CancellationToken cancellationToken = default);
}
