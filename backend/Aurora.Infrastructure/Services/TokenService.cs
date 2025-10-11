using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aurora.Application.Interfaces;
using Aurora.Application.Options;
using Aurora.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aurora.Infrastructure.Services;

/// <summary>
/// Implementación de generación de tokens JWT.
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.SecretKey) || _options.SecretKey.Length < 32)
        {
            throw new ArgumentException("La clave secreta para JWT debe tener al menos 32 caracteres.", nameof(options));
        }

        var keyBytes = Encoding.UTF8.GetBytes(_options.SecretKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }

    public string GenerateAccessToken(User user, UserSession session)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, session.TokenId.ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.Name, user.Name)
        };

        if (user.IsEmailVerified)
        {
            claims.Add(new Claim("email_verified", "true"));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = now.AddMinutes(_options.AccessTokenMinutes),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = _signingCredentials,
            NotBefore = now
        };

        var securityToken = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(securityToken);
    }
}
