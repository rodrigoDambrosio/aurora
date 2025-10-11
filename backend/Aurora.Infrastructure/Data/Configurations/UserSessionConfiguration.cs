using Aurora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad UserSession.
/// </summary>
public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.TokenId)
            .IsRequired();

        builder.Property(session => session.ExpiresAtUtc)
            .IsRequired();

        builder.Property(session => session.RevokedReason)
            .HasMaxLength(256);

        builder.HasIndex(session => session.TokenId)
            .IsUnique();

        builder.HasOne(session => session.User)
            .WithMany(user => user.Sessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
