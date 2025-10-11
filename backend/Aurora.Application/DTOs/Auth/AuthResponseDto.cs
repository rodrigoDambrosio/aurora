namespace Aurora.Application.DTOs.Auth;

/// <summary>
/// Respuesta común para las operaciones de autenticación.
/// Incluye el token JWT de acceso y la metadata del usuario autenticado.
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// Token JWT firmado que permite acceder a la API.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora (UTC) de expiración del token.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Información resumida del usuario autenticado.
    /// </summary>
    public UserSummaryDto User { get; set; } = new();
}
