using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        
        builder.Property(n => n.Kind)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.ActorUser)
            .WithMany()
            .HasForeignKey(n => n.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAtUtc, n.Id })
            .HasDatabaseName("IX_Notifications_RecipientUserId_CreatedAtUtc_Id");

        builder.HasIndex(n => new { n.RecipientUserId, n.ReadAtUtc })
            .HasDatabaseName("IX_Notifications_RecipientUserId_ReadAtUtc");
    }
}