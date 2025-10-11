using Aurora.Application.Interfaces;
using BCrypt.Net;

namespace Aurora.Infrastructure.Services;

/// <summary>
/// Implementación de hashing de contraseñas utilizando BCrypt.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string HashPassword(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
        {
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(plainTextPassword));
        }

        return BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: WorkFactor);
    }

    public bool VerifyPassword(string plainTextPassword, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
    }
}
