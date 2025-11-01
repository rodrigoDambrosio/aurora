using Aurora.Domain.Enums;

namespace Aurora.Application.Services.Helpers;

public static class ReminderCalculator
{
    /// <summary>
    /// Calcula la fecha y hora en que debe dispararse un recordatorio
    /// </summary>
    public static DateTime CalculateTriggerDateTime(
        DateTime eventStartDate,
        ReminderType reminderType,
        int? customTimeHours = null,
        int? customTimeMinutes = null)
    {
        return reminderType switch
        {
            ReminderType.Minutes15 => eventStartDate.AddMinutes(-15),
            ReminderType.Minutes30 => eventStartDate.AddMinutes(-30),
            ReminderType.OneDayBefore => CalculateOneDayBeforeTrigger(eventStartDate, customTimeHours, customTimeMinutes),
            _ => throw new ArgumentException($"Tipo de recordatorio no válido: {reminderType}")
        };
    }

    private static DateTime CalculateOneDayBeforeTrigger(
        DateTime eventStartDate,
        int? customTimeHours,
        int? customTimeMinutes)
    {
        if (!customTimeHours.HasValue || !customTimeMinutes.HasValue)
        {
            throw new ArgumentException("Se requieren hora y minutos personalizados para recordatorios de un día antes");
        }

        var oneDayBefore = eventStartDate.Date.AddDays(-1);
        return oneDayBefore.AddHours(customTimeHours.Value).AddMinutes(customTimeMinutes.Value);
    }

    /// <summary>
    /// Verifica si un recordatorio debe dispararse ahora (con tolerancia de 2 minutos)
    /// </summary>
    public static bool ShouldTriggerNow(DateTime triggerDateTime, int toleranceMinutes = 2)
    {
        var now = DateTime.UtcNow;
        var toleranceSpan = TimeSpan.FromMinutes(toleranceMinutes);

        return triggerDateTime <= now.Add(toleranceSpan) && triggerDateTime >= now.Subtract(toleranceSpan);
    }
}
