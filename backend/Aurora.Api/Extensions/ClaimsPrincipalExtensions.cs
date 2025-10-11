using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Aurora.Api.Extensions;

/// <summary>
/// Métodos de extensión para ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Obtiene el identificador del usuario a partir del token JWT.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (Guid.TryParse(userIdValue, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Obtiene el identificador único del token (JTI).
    /// </summary>
    public static Guid? GetTokenId(this ClaimsPrincipal principal)
    {
        var tokenIdValue = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
        return Guid.TryParse(tokenIdValue, out var tokenId) ? tokenId : null;
    }
}
