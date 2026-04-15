namespace Postly.Api.Persistence.Entities;

public class Post
{
    public long Id { get; set; }
    public long AuthorId { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? EditedAtUtc { get; set; }
    public long? ReplyToPostId { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    public UserAccount Author { get; set; } = null!;
    public ICollection<Like> Likes { get; set; } = [];
}
