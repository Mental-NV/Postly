namespace Postly.Api.Features.Timeline.Contracts;

public record PostSummary(
    long Id,
    string? AuthorUsername,
    string? AuthorDisplayName,
    string? AuthorAvatarUrl,
    string? Body,
    DateTimeOffset CreatedAtUtc,
    bool IsEdited,
    int LikeCount,
    bool LikedByViewer,
    bool CanEdit,
    bool CanDelete,
    bool IsReply,
    long? ReplyToPostId,
    string State  // "available" | "deleted"
);

public record TimelineResponse(
    PostSummary[] Posts,
    string? NextCursor
);
