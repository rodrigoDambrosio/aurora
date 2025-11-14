using Aurora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Infrastructure.Data.Configurations;

public class EventReminderConfiguration : IEntityTypeConfiguration<EventReminder>
{
    public void Configure(EntityTypeBuilder<EventReminder> builder)
    {
        builder.ToTable("EventReminders");

        builder.HasKey(er => er.Id);

        builder.Property(er => er.Id)
            .ValueGeneratedOnAdd();

        builder.Property(er => er.ReminderType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(er => er.CustomTimeHours)
            .IsRequired(false);

        builder.Property(er => er.CustomTimeMinutes)
            .IsRequired(false);

        builder.Property(er => er.TriggerDateTime)
            .IsRequired();

        builder.Property(er => er.IsSent)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(er => er.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(er => er.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(er => er.Event)
            .WithMany(e => e.Reminders)
            .HasForeignKey(er => er.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(er => er.EventId);
        builder.HasIndex(er => er.TriggerDateTime);
        builder.HasIndex(er => er.IsSent);
        builder.HasIndex(er => new { er.TriggerDateTime, er.IsSent })
            .HasDatabaseName("IX_EventReminders_TriggerDateTime_IsSent");
    }
}
