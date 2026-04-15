namespace Postly.Api.Persistence.Entities;

public class UserAccount
{
    public long Id { get; set; }
    public required string Username { get; set; }
    public required string NormalizedUsername { get; set; }
    public required string DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarContentType { get; set; }
    public byte[]? AvatarBytes { get; set; }
    public DateTimeOffset? AvatarUpdatedAtUtc { get; set; }
    public required string PasswordHash { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<Post> Posts { get; set; } = [];
    public ICollection<Follow> Following { get; set; } = [];
    public ICollection<Follow> Followers { get; set; } = [];
    public ICollection<Like> Likes { get; set; } = [];
}
