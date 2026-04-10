namespace Postly.Api.Features.Posts.Contracts;

public record CreatePostRequest(string Body);

public record UpdatePostRequest(string Body);

public record PostResponse(
    long Id,
    string AuthorUsername,
    string AuthorDisplayName,
    string Body,
    DateTimeOffset CreatedAtUtc,
    bool IsEdited,
    DateTimeOffset? EditedAtUtc
);
