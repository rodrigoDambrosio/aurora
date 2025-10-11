namespace Aurora.Application.DTOs.Auth;

/// <summary>
/// Datos requeridos para registrar a un nuevo usuario.
/// </summary>
public class RegisterUserRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
