using Aurora.Application.DTOs.User;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Interfaz para el servicio de gestión de usuarios
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Obtiene el perfil del usuario autenticado
    /// </summary>
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza el perfil del usuario autenticado
    /// </summary>
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las preferencias del usuario autenticado
    /// </summary>
    Task<UserPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza las preferencias del usuario autenticado
    /// </summary>
    Task<UserPreferencesDto> UpdatePreferencesAsync(Guid userId, UpdateUserPreferencesDto dto, CancellationToken cancellationToken = default);
}
