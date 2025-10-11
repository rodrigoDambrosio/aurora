using Aurora.Domain.Entities;
using Aurora.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad Event
/// </summary>
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // Tabla
        builder.ToTable("Events");

        // Clave primaria
        builder.HasKey(e => e.Id);

        // Propiedades
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.StartDate)
            .IsRequired()
            .HasColumnType("datetime");

        builder.Property(e => e.EndDate)
            .HasColumnType("datetime");

        builder.Property(e => e.IsAllDay)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.EventCategoryId)
            .IsRequired();

        builder.Property(e => e.Priority)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(EventPriority.Medium);

        // Índices
        builder.HasIndex(e => e.StartDate)
            .HasDatabaseName("IX_Events_StartDate");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_Events_UserId");

        builder.HasIndex(e => e.EventCategoryId)
            .HasDatabaseName("IX_Events_CategoryId");

        builder.HasIndex(e => new { e.UserId, e.StartDate })
            .HasDatabaseName("IX_Events_UserId_StartDate");

        // Relaciones
        builder.HasOne(e => e.User)
            .WithMany(u => u.Events)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EventCategory)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.EventCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Propiedades de auditoría
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("datetime('now')");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("datetime('now')");

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}