using Aurora.Domain.Entities;

namespace Aurora.Application.Interfaces;

/// <summary>
/// Servicio responsable de generar y validar tokens JWT.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Genera un token JWT para el usuario y la sesión indicada.
    /// </summary>
    /// <param name="user">Usuario autenticado.</param>
    /// <param name="session">Sesión persistida en base de datos.</param>
    /// <returns>Token JWT serializado.</returns>
    string GenerateAccessToken(User user, UserSession session);
}
