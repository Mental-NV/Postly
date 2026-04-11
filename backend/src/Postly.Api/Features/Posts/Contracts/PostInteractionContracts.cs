namespace Postly.Api.Features.Posts.Contracts;

public record PostInteractionState(
    long PostId,
    int LikeCount,
    bool LikedByViewer
);
