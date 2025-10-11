using System.ComponentModel.DataAnnotations.Schema;

namespace Aurora.Domain.Entities;

/// <summary>
/// Representa una sesión de autenticación del usuario respaldada por un token JWT.
/// Permite invalidar sesiones (logout) y controlar expiraciones en el servidor.
/// </summary>
public class UserSession : BaseEntity
{
    /// <summary>
    /// Identificador del usuario al que pertenece la sesión.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Identificador único del token JWT emitido (JTI).
    /// Se guarda también como clave alternativa para búsquedas eficientes.
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// Fecha y hora de expiración del token asociado.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Fecha y hora en la que la sesión fue revocada manualmente (logout).
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// Motivo opcional de la revocación.
    /// </summary>
    public string? RevokedReason { get; set; }

    /// <summary>
    /// Navegación hacia el usuario dueño de la sesión.
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Indica si la sesión está actualmente activa.
    /// </summary>
    [NotMapped]
    public bool IsRevoked => RevokedAtUtc.HasValue || !IsActive || DateTime.UtcNow >= ExpiresAtUtc;
}
