using Aurora.Domain.Constants;
using Aurora.Domain.Entities;

namespace Aurora.Domain.Services;

/// <summary>
/// Servicio de dominio para manejo de usuarios en modo desarrollo
/// </summary>
public static class DevelopmentUserService
{
    /// <summary>
    /// Obtiene el usuario actual, usando usuario demo si no hay autenticación
    /// </summary>
    /// <param name="userId">ID del usuario autenticado (puede ser null)</param>
    /// <returns>ID del usuario a usar</returns>
    public static Guid GetCurrentUserId(Guid? userId = null)
    {
#if DEBUG
        // En modo DEBUG, permite acceso sin autenticación usando usuario demo
        return userId ?? DomainConstants.DemoUser.Id;
#else
        // En producción, requiere autenticación
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado");
        }

        return userId.Value;
#endif
    }

    /// <summary>
    /// Crea la información básica del usuario demo
    /// </summary>
    /// <returns>Usuario demo configurado</returns>
    public static User CreateDemoUser()
    {
        return new User
        {
            Id = DomainConstants.DemoUser.Id,
            Email = DomainConstants.DemoUser.Email,
            Name = DomainConstants.DemoUser.Name,
            IsEmailVerified = true,
            PasswordHash = "DEMO_USER_NO_PASSWORD", // Hash ficticio
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>
    /// Verifica si un usuario tiene acceso a un recurso
    /// En modo desarrollo siempre retorna true para el usuario demo
    /// </summary>
    /// <param name="resourceOwnerId">ID del propietario del recurso</param>
    /// <param name="currentUserId">ID del usuario actual</param>
    /// <returns>True si tiene acceso</returns>
    public static bool HasAccessToResource(Guid resourceOwnerId, Guid? currentUserId = null)
    {
        var userId = GetCurrentUserId(currentUserId);

        // En modo desarrollo, el usuario demo tiene acceso a todo
        if (DomainConstants.Development.AllowAnonymousAccess &&
            userId == DomainConstants.DemoUser.Id)
        {
            return true;
        }

        return resourceOwnerId == userId;
    }
}