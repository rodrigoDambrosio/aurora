namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para actualizar el estado de ánimo de un evento
/// </summary>
public class UpdateEventMoodDto
{
    /// <summary>
    /// Calificación del estado de ánimo (1-5)
    /// 1 = Muy mal, 2 = Mal, 3 = Normal, 4 = Bien, 5 = Muy bien
    /// </summary>
    public int? MoodRating { get; set; }

    /// <summary>
    /// Notas adicionales sobre cómo se sintió el usuario durante el evento
    /// </summary>
    public string? MoodNotes { get; set; }
}
