namespace Aurora.Application.DTOs.Auth;

/// <summary>
/// Información básica del usuario autenticado que se expone al frontend.
/// </summary>
public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
}
