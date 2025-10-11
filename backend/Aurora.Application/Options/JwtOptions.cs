namespace Aurora.Application.Options;

/// <summary>
/// Configuración para la generación de tokens JWT.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public int SessionDurationDays { get; set; } = 7;
}
