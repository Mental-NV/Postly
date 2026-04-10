using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.Persistence.Configurations;

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("Follows");

        builder.HasKey(f => new { f.FollowerId, f.FollowedId });

        builder.HasIndex(f => f.FollowedId);

        builder.HasIndex(f => f.FollowerId);

        builder.Property(f => f.CreatedAtUtc)
            .IsRequired();
    }
}
