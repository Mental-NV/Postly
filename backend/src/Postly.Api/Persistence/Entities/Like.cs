namespace Postly.Api.Persistence.Entities;

public class Like
{
    public long UserAccountId { get; set; }
    public long PostId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public UserAccount UserAccount { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
