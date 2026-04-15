using Postly.Api.Persistence.Entities;

namespace Postly.Api.Features.Profiles.Application;

public static class ProfileIdentityProjection
{
    public const string AvatarContentType = "image/jpeg";

    public static bool HasCustomAvatar(UserAccount user)
    {
        return user.AvatarBytes is { Length: > 0 }
            && string.Equals(
                user.AvatarContentType,
                AvatarContentType,
                StringComparison.OrdinalIgnoreCase);
    }

    public static string? CreateAvatarUrl(UserAccount user)
    {
        if (!HasCustomAvatar(user) || user.AvatarUpdatedAtUtc == null)
        {
            return null;
        }

        return $"/api/profiles/{user.Username}/avatar?v={user.AvatarUpdatedAtUtc.Value.ToUnixTimeMilliseconds()}";
    }
}
