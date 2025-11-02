using Aurora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad User
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Tabla
        builder.ToTable("Users");

        // Clave primaria
        builder.HasKey(u => u.Id);

        // Propiedades
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Índices
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        // Relaciones
        builder.HasMany(u => u.Events)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.EventCategories)
            .WithOne(ec => ec.User)
            .HasForeignKey(ec => ec.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.DailyMoodEntries)
            .WithOne(entry => entry.User)
            .HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Preferences)
            .WithOne(p => p.User)
            .HasForeignKey<UserPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Propiedades de auditoría
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("datetime('now')");

        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("datetime('now')");

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}