using Aurora.Domain.Entities;
using Aurora.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Aurora.Infrastructure.Data;

/// <summary>
/// Contexto de base de datos para Aurora
/// </summary>
public class AuroraDbContext : DbContext
{
    public AuroraDbContext(DbContextOptions<AuroraDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Conjunto de usuarios
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Conjunto de eventos
    /// </summary>
    public DbSet<Event> Events { get; set; }

    /// <summary>
    /// Conjunto de categorías de eventos
    /// </summary>
    public DbSet<EventCategory> EventCategories { get; set; }

    /// <summary>
    /// Conjunto de preferencias de usuario
    /// </summary>
    public DbSet<UserPreferences> UserPreferences { get; set; }

    /// <summary>
    /// Conjunto de sesiones activas de usuarios
    /// </summary>
    public DbSet<UserSession> UserSessions { get; set; }

    /// <summary>
    /// Conjunto de recordatorios de eventos
    /// </summary>
    public DbSet<EventReminder> EventReminders { get; set; }
    /// <summary>
    /// Conjunto de registros diarios de estado de ánimo
    /// </summary>
    public DbSet<DailyMoodEntry> DailyMoodEntries { get; set; }

    /// <summary>
    /// Feedback sobre recomendaciones personalizadas
    /// </summary>
    public DbSet<RecommendationFeedback> RecommendationFeedback { get; set; }

    /// <summary>
    /// Conjunto de sugerencias de reorganización
    /// </summary>
    public DbSet<ScheduleSuggestion> ScheduleSuggestions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configuraciones específicas de entidades
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new EventCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new UserPreferencesConfiguration());
        modelBuilder.ApplyConfiguration(new UserSessionConfiguration());
        modelBuilder.ApplyConfiguration(new EventReminderConfiguration());
        modelBuilder.ApplyConfiguration(new DailyMoodEntryConfiguration());
        modelBuilder.ApplyConfiguration(new RecommendationFeedbackConfiguration());

        // Configurar filtros globales para soft delete
        modelBuilder.Entity<User>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<Event>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<EventCategory>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<UserPreferences>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<UserSession>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<DailyMoodEntry>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<RecommendationFeedback>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ScheduleSuggestion>().HasQueryFilter(e => e.IsActive);
    }

    /// <summary>
    /// Sobrescribe SaveChanges para manejar automáticamente las propiedades de auditoría
    /// </summary>
    public override int SaveChanges()
    {
        UpdateAuditProperties();
        return base.SaveChanges();
    }

    /// <summary>
    /// Sobrescribe SaveChangesAsync para manejar automáticamente las propiedades de auditoría
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditProperties();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Actualiza automáticamente las propiedades de auditoría
    /// </summary>
    private void UpdateAuditProperties()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            var now = DateTime.UtcNow;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.IsActive = true;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    // No modificar CreatedAt en updates
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    break;

                case EntityState.Deleted:
                    // Implementar soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsActive = false;
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}