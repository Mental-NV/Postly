namespace Postly.Api.Persistence.Entities;

public class Session
{
    public Guid Id { get; set; }
    public long UserAccountId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public DateTimeOffset? LastSeenAtUtc { get; set; }

    public UserAccount UserAccount { get; set; } = null!;
}
