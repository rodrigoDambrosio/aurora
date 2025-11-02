using Aurora.Domain.Services;

namespace Aurora.Domain.Entities;

/// <summary>
/// Representa el estado de ánimo diario registrado por un usuario.
/// </summary>
public class DailyMoodEntry : BaseEntity
{
    /// <summary>
    /// Fecha del registro (sin componente de hora, en UTC).
    /// </summary>
    public DateTime EntryDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Calificación del estado de ánimo (1 = muy mal, 5 = excelente).
    /// </summary>
    public int MoodRating { get; set; }

    /// <summary>
    /// Nota opcional sobre cómo se sintió el usuario ese día.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Identificador del usuario propietario del registro.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Usuario propietario del registro.
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Obtiene la fecha normalizada en UTC (sin hora).
    /// </summary>
    public DateTime GetNormalizedDateUtc() => EntryDate.Date;

    /// <summary>
    /// Define el propietario del registro, respetando el modo demo.
    /// </summary>
    /// <param name="userId">Identificador de usuario.</param>
    public void SetOwner(Guid? userId = null)
    {
        UserId = DevelopmentUserService.GetCurrentUserId(userId);
    }

    /// <summary>
    /// Indica si el registro pertenece al usuario indicado.
    /// </summary>
    public bool BelongsToUser(Guid? userId)
    {
        var currentUser = DevelopmentUserService.GetCurrentUserId(userId);
        return UserId == currentUser;
    }
}
