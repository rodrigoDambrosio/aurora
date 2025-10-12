namespace Aurora.Application.DTOs.User;

/// <summary>
/// DTO para actualizar el perfil del usuario
/// </summary>
public class UpdateUserProfileDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Timezone { get; set; }
}
