using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.Persistence.Configurations;

public class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.ToTable("Likes");

        builder.HasKey(l => new { l.UserAccountId, l.PostId });

        builder.HasIndex(l => l.PostId);

        builder.Property(l => l.CreatedAtUtc)
            .IsRequired();
    }
}
