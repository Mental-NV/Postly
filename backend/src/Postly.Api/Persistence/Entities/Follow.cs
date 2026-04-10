namespace Postly.Api.Persistence.Entities;

public class Follow
{
    public long FollowerId { get; set; }
    public long FollowedId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public UserAccount Follower { get; set; } = null!;
    public UserAccount Followed { get; set; } = null!;
}
