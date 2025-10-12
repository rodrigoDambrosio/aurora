namespace Aurora.Application.DTOs.User;

/// <summary>
/// DTO para el perfil público del usuario
/// </summary>
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Timezone { get; set; }
}
