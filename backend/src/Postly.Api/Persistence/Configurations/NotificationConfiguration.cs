using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Kind)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(n => n.CreatedAtUtc)
            .IsRequired();

        builder.Property(n => n.ReadAtUtc)
            .IsRequired(false);

        builder.Property(n => n.ProfileUserId)
            .IsRequired(false);

        builder.Property(n => n.PostId)
            .IsRequired(false);

        builder.Property(n => n.ReplyPostId)
            .IsRequired(false);

        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAtUtc, n.Id })
            .IsDescending(false, true, true);

        builder.HasIndex(n => new { n.RecipientUserId, n.ReadAtUtc });

        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Actor)
            .WithMany()
            .HasForeignKey(n => n.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
