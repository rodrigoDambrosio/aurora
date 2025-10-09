using Aurora.Domain.Constants;
using Aurora.Domain.Entities;
using Aurora.Domain.Enums;
using Aurora.Domain.Services;
using Aurora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aurora.Infrastructure.Data;

/// <summary>
/// Servicio para inicializar datos de desarrollo en la base de datos
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Inicializa la base de datos con datos de desarrollo
    /// </summary>
    /// <param name="context">Contexto de base de datos</param>
    public static async Task InitializeAsync(AuroraDbContext context)
    {
        // Crear la base de datos si no existe
        await context.Database.EnsureCreatedAsync();

        // Si ya hay usuarios, no hacer nada
        if (await context.Users.AnyAsync())
        {
            return;
        }

        // Crear usuario de desarrollo
        var developmentUser = DevelopmentUserService.CreateDemoUser();
        await context.Users.AddAsync(developmentUser);

        // Crear preferencias por defecto para el usuario
        var userPreferences = new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = developmentUser.Id,
            Theme = "light",
            Language = "es-ES",
            DefaultReminderMinutes = 15,
            FirstDayOfWeek = 1, // Lunes
            TimeFormat = "24h",
            DateFormat = "dd/MM/yyyy"
        };
        await context.UserPreferences.AddAsync(userPreferences);

        // Crear categorías por defecto
        var defaultCategories = DefaultEventCategories.CreateSystemCategories(developmentUser.Id);
        await context.EventCategories.AddRangeAsync(defaultCategories);

        // Guardar cambios
        await context.SaveChangesAsync();

        // Crear eventos de ejemplo
        await CreateSampleEventsAsync(context, developmentUser.Id, defaultCategories);
    }

    /// <summary>
    /// Crea eventos de ejemplo para demostrar la funcionalidad
    /// </summary>
    private static async Task CreateSampleEventsAsync(AuroraDbContext context, Guid userId, IEnumerable<EventCategory> categories)
    {
        var categoriesList = categories.ToList();
        var workCategory = categoriesList.First(c => c.Name == "Trabajo");
        var personalCategory = categoriesList.First(c => c.Name == "Personal");
        var healthCategory = categoriesList.First(c => c.Name == "Salud");

        var now = DateTime.Now;
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek + 1); // Lunes de esta semana

        var sampleEvents = new List<Event>
        {
            // Eventos de esta semana
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Reunión de equipo",
                Description = "Reunión semanal del equipo de desarrollo",
                StartDate = startOfWeek.AddHours(9), // Lunes 9:00
                EndDate = startOfWeek.AddHours(10), // Lunes 10:00
                IsAllDay = false,
                UserId = userId,
                EventCategoryId = workCategory.Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Almuerzo con amigos",
                Description = "Almuerzo en el restaurante italiano",
                StartDate = startOfWeek.AddDays(1).AddHours(13), // Martes 13:00
                EndDate = startOfWeek.AddDays(1).AddHours(15), // Martes 15:00
                IsAllDay = false,
                UserId = userId,
                EventCategoryId = personalCategory.Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Consulta médica",
                Description = "Control médico anual",
                StartDate = startOfWeek.AddDays(2).AddHours(16), // Miércoles 16:00
                EndDate = startOfWeek.AddDays(2).AddHours(17), // Miércoles 17:00
                IsAllDay = false,
                UserId = userId,
                EventCategoryId = healthCategory.Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Presentación proyecto",
                Description = "Presentación del proyecto Aurora al cliente",
                StartDate = startOfWeek.AddDays(3).AddHours(10), // Jueves 10:00
                EndDate = startOfWeek.AddDays(3).AddHours(12), // Jueves 12:00
                IsAllDay = false,
                UserId = userId,
                EventCategoryId = workCategory.Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Día libre",
                Description = "Día de descanso y relajación",
                StartDate = startOfWeek.AddDays(4), // Viernes todo el día
                EndDate = startOfWeek.AddDays(4),
                IsAllDay = true,
                UserId = userId,
                EventCategoryId = personalCategory.Id
            },
            // Eventos de la semana siguiente
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Gimnasio",
                Description = "Rutina de ejercicios matutina",
                StartDate = startOfWeek.AddDays(7).AddHours(7), // Lunes siguiente 7:00
                EndDate = startOfWeek.AddDays(7).AddHours(8), // Lunes siguiente 8:00
                IsAllDay = false,
                UserId = userId,
                EventCategoryId = healthCategory.Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Reunión con cliente",
                Description = "Revisión de avances del proyecto",
                StartDate = startOfWeek.AddDays(8).AddHours(15), // Martes siguiente 15:00
                EndDate = startOfWeek.AddDays(8).AddHours(16), // Martes siguiente 16:00
                IsAllDay = false,
                UserId = userId,
                EventCategoryId = workCategory.Id
            }
        };

        await context.Events.AddRangeAsync(sampleEvents);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Reinicia la base de datos con datos frescos de desarrollo
    /// </summary>
    /// <param name="context">Contexto de base de datos</param>
    public static async Task ResetDatabaseAsync(AuroraDbContext context)
    {
        await context.Database.EnsureDeletedAsync();
        await InitializeAsync(context);
    }
}
