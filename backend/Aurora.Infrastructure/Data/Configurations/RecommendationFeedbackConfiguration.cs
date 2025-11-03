using System;
using Aurora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad RecommendationFeedback.
/// </summary>
public class RecommendationFeedbackConfiguration : IEntityTypeConfiguration<RecommendationFeedback>
{
    public void Configure(EntityTypeBuilder<RecommendationFeedback> builder)
    {
        builder.ToTable("RecommendationFeedback");

        builder.HasKey(feedback => feedback.Id);

        builder.Property(feedback => feedback.RecommendationId)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(feedback => feedback.Accepted)
            .IsRequired();

        builder.Property(feedback => feedback.Notes)
            .HasMaxLength(500);

        builder.Property(feedback => feedback.MoodAfter)
            .HasConversion<int?>();

        builder.Property(feedback => feedback.SubmittedAtUtc)
            .HasColumnType("TEXT")
            .HasConversion(
                date => DateTime.SpecifyKind(date, DateTimeKind.Utc),
                date => DateTime.SpecifyKind(date, DateTimeKind.Utc))
            .IsRequired();

        builder.Property(feedback => feedback.UserId)
            .IsRequired();

        builder.Property(feedback => feedback.CreatedAt)
            .HasDefaultValueSql("datetime('now')");

        builder.Property(feedback => feedback.UpdatedAt)
            .HasDefaultValueSql("datetime('now')");

        builder.Property(feedback => feedback.IsActive)
            .HasDefaultValue(true)
            .ValueGeneratedNever();

        builder.HasIndex(feedback => new { feedback.UserId, feedback.RecommendationId })
            .IsUnique()
            .HasDatabaseName("IX_RecommendationFeedback_UserId_RecommendationId");

        builder.HasOne(feedback => feedback.User)
            .WithMany(user => user.RecommendationFeedback)
            .HasForeignKey(feedback => feedback.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
