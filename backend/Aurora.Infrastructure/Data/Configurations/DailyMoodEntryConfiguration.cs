using Aurora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para los registros de estado de ánimo diario.
/// </summary>
public class DailyMoodEntryConfiguration : IEntityTypeConfiguration<DailyMoodEntry>
{
    public void Configure(EntityTypeBuilder<DailyMoodEntry> builder)
    {
        builder.ToTable("DailyMoodEntries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.EntryDate)
            .HasColumnType("TEXT")
            .HasConversion(
                date => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
                date => DateTime.SpecifyKind(date, DateTimeKind.Utc))
            .IsRequired();

        builder.Property(entry => entry.MoodRating)
            .IsRequired();

        builder.Property(entry => entry.Notes)
            .HasMaxLength(500);

        builder.Property(entry => entry.UserId)
            .IsRequired();

        builder.Property(entry => entry.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        builder.Property(entry => entry.UpdatedAt)
            .HasDefaultValueSql("datetime('now')");

        builder.Property(entry => entry.IsActive)
            .HasDefaultValue(true)
            .ValueGeneratedNever();

        builder.HasIndex(entry => new { entry.UserId, entry.EntryDate })
            .IsUnique()
            .HasDatabaseName("IX_DailyMoodEntries_UserId_EntryDate");

        builder.HasOne(entry => entry.User)
            .WithMany(user => user.DailyMoodEntries)
            .HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
