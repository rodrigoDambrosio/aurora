using Aurora.Domain.Constants;
using Aurora.Domain.Entities;
using Aurora.Domain.Services;
using Aurora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Aurora.Domain.Enums;

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

        // Asegurar que la columna Priority exista incluso si la base ya se creó antes del cambio
        await EnsurePriorityColumnAsync(context);

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
                EventCategoryId = workCategory.Id,
                Priority = EventPriority.High
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
                EventCategoryId = personalCategory.Id,
                Priority = EventPriority.Medium
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
                EventCategoryId = healthCategory.Id,
                Priority = EventPriority.High
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
                EventCategoryId = workCategory.Id,
                Priority = EventPriority.Critical
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
                EventCategoryId = personalCategory.Id,
                Priority = EventPriority.Low
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
                EventCategoryId = healthCategory.Id,
                Priority = EventPriority.Medium
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
                EventCategoryId = workCategory.Id,
                Priority = EventPriority.High
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

    /// <summary>
    /// Garantiza que la columna Priority exista en la tabla Events incluso para bases creadas antes del cambio.
    /// </summary>
    private static async Task EnsurePriorityColumnAsync(AuroraDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var shouldCloseConnection = false;

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
            shouldCloseConnection = true;
        }

        try
        {
            using var pragmaCommand = connection.CreateCommand();
            pragmaCommand.CommandText = "PRAGMA table_info('Events');";

            var hasPriorityColumn = false;

            await using (var reader = await pragmaCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var columnName = reader["name"]?.ToString();
                    if (string.Equals(columnName, "Priority", StringComparison.OrdinalIgnoreCase))
                    {
                        hasPriorityColumn = true;
                        break;
                    }
                }
            }

            if (!hasPriorityColumn)
            {
                using var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Events ADD COLUMN Priority INTEGER NOT NULL DEFAULT 2;";
                await alterCommand.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }
}
