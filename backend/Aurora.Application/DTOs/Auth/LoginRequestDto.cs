namespace Aurora.Application.DTOs.Auth;

/// <summary>
/// Datos necesarios para iniciar sesión con email y contraseña.
/// </summary>
public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
