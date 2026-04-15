using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.Persistence.Configurations;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(u => u.NormalizedUsername)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(u => u.NormalizedUsername)
            .IsUnique();

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Bio)
            .HasMaxLength(160);

        builder.Property(u => u.AvatarContentType)
            .HasMaxLength(32);

        builder.Property(u => u.AvatarBytes);

        builder.Property(u => u.AvatarUpdatedAtUtc);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        builder.HasMany(u => u.Sessions)
            .WithOne(s => s.UserAccount)
            .HasForeignKey(s => s.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Posts)
            .WithOne(p => p.Author)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Following)
            .WithOne(f => f.Follower)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Followers)
            .WithOne(f => f.Followed)
            .HasForeignKey(f => f.FollowedId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Likes)
            .WithOne(l => l.UserAccount)
            .HasForeignKey(l => l.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
