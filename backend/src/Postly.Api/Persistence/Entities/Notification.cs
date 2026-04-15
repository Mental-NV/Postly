namespace Postly.Api.Persistence.Entities;

public class Notification
{
    public long Id { get; set; }
    public long RecipientUserId { get; set; }
    public long ActorUserId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public long? ProfileUserId { get; set; }
    public long? PostId { get; set; }
    public long? ReplyPostId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }

    public UserAccount RecipientUser { get; set; } = null!;
    public UserAccount ActorUser { get; set; } = null!;
}