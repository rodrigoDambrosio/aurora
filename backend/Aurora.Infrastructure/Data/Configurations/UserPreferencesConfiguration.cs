using Aurora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad UserPreferences
/// </summary>
public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        // Tabla
        builder.ToTable("UserPreferences");

        // Clave primaria
        builder.HasKey(p => p.Id);

        // Propiedades
        builder.Property(p => p.Theme)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("light");

        builder.Property(p => p.Language)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("es");

        builder.Property(p => p.DefaultReminderMinutes)
            .IsRequired()
            .HasDefaultValue(15);

        builder.Property(p => p.FirstDayOfWeek)
            .IsRequired()
            .HasDefaultValue(1); // Lunes

        builder.Property(p => p.TimeFormat)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("24h");

        builder.Property(p => p.DateFormat)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("DD/MM/YYYY");

        builder.Property(p => p.UserId)
            .IsRequired();

        // Índices
        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserPreferences_UserId");

        // Relaciones
        builder.HasOne(p => p.User)
            .WithOne(u => u.Preferences)
            .HasForeignKey<UserPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Propiedades de auditoría
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("datetime('now')");

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("datetime('now')");

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}