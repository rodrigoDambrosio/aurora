using Aurora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad EventCategory
/// </summary>
public class EventCategoryConfiguration : IEntityTypeConfiguration<EventCategory>
{
    public void Configure(EntityTypeBuilder<EventCategory> builder)
    {
        // Tabla
        builder.ToTable("EventCategories");

        // Clave primaria
        builder.HasKey(c => c.Id);

        // Propiedades
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.Color)
            .IsRequired()
            .HasMaxLength(7); // Para colores hex (#FFFFFF)

        builder.Property(c => c.Icon)
            .HasMaxLength(50);

        builder.Property(c => c.IsSystemDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.UserId)
            .IsRequired();

        // Índices
        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("IX_EventCategories_UserId");

        // Índice único parcial: solo aplica a categorías activas
        // Esto permite múltiples categorías inactivas con el mismo nombre (soft delete)
        builder.HasIndex(c => new { c.UserId, c.Name })
            .IsUnique()
            .HasDatabaseName("IX_EventCategories_UserId_Name_Active")
            .HasFilter("IsActive = 1");

        builder.HasIndex(c => c.IsSystemDefault)
            .HasDatabaseName("IX_EventCategories_IsDefault");

        // Relaciones
        builder.HasOne(c => c.User)
            .WithMany(u => u.EventCategories)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Events)
            .WithOne(e => e.EventCategory)
            .HasForeignKey(e => e.EventCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Propiedades de auditoría
        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("datetime('now')");

        builder.Property(c => c.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("datetime('now')");

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}