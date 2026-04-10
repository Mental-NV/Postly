using Postly.Api.Features.Timeline.Contracts;

namespace Postly.Api.Features.Profiles.Contracts;

public record UserProfile(
    string Username,
    string DisplayName,
    string? Bio,
    int FollowerCount,
    int FollowingCount,
    bool IsSelf,
    bool IsFollowedByViewer
);

public record ProfileResponse(
    UserProfile Profile,
    PostSummary[] Posts,
    string? NextCursor
);
