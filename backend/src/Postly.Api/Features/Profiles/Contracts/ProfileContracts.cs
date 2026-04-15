using Postly.Api.Features.Timeline.Contracts;

namespace Postly.Api.Features.Profiles.Contracts;

public record UpdateProfileRequest(
    string DisplayName,
    string? Bio
);

public record UserProfile(
    string Username,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    bool HasCustomAvatar,
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
