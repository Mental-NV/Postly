namespace Postly.Api.Features.Timeline.Contracts;

public record PostSummary(
    long Id,
    string AuthorUsername,
    string AuthorDisplayName,
    string Body,
    DateTimeOffset CreatedAtUtc,
    bool IsEdited,
    int LikeCount,
    bool LikedByViewer,
    bool CanEdit,
    bool CanDelete
);

public record TimelineResponse(
    PostSummary[] Posts,
    string? NextCursor
);
