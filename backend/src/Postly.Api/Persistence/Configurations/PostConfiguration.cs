using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Body)
            .IsRequired()
            .HasMaxLength(280);

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.Property(p => p.ReplyToPostId)
            .IsRequired(false);

        builder.Property(p => p.DeletedAtUtc)
            .IsRequired(false);

        builder.HasIndex(p => new { p.AuthorId, p.CreatedAtUtc, p.Id })
            .IsDescending(false, true, true);

        builder.HasIndex(p => new { p.CreatedAtUtc, p.Id })
            .IsDescending(true, true);

        // Reply pagination index
        builder.HasIndex(p => new { p.ReplyToPostId, p.CreatedAtUtc, p.Id })
            .IsDescending(false, true, true);

        builder.HasMany(p => p.Likes)
            .WithOne(l => l.Post)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
