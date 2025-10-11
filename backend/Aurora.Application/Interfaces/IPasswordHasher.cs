namespace Aurora.Application.Interfaces;

/// <summary>
/// Servicio para gestionar el hash seguro de contraseñas.
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string plainTextPassword);
    bool VerifyPassword(string plainTextPassword, string passwordHash);
}
