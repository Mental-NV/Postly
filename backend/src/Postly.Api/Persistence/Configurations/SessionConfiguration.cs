using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TokenHash)
            .IsRequired();

        builder.HasIndex(s => s.TokenHash)
            .IsUnique();

        builder.HasIndex(s => new { s.UserAccountId, s.ExpiresAtUtc });

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.ExpiresAtUtc)
            .IsRequired();
    }
}
