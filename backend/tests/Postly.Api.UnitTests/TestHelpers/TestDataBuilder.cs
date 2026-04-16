using Microsoft.AspNetCore.Identity;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.UnitTests.TestHelpers;

public static class TestDataBuilder
{
    public static UserAccount CreateUserAccount(
        long? id = null,
        string? username = null,
        string? displayName = null,
        string? bio = null,
        string? password = null)
    {
        var user = new UserAccount
        {
            Id = id ?? 1,
            Username = username ?? "testuser",
            NormalizedUsername = (username ?? "testuser").ToUpperInvariant(),
            DisplayName = displayName ?? "Test User",
            Bio = bio,
            PasswordHash = string.Empty,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (password != null)
        {
            var passwordHasher = new PasswordHasher<UserAccount>();
            user.PasswordHash = passwordHasher.HashPassword(user, password);
        }

        return user;
    }

    public static Post CreatePost(
        long? id = null,
        long? authorId = null,
        string? body = null,
        UserAccount? author = null)
    {
        return new Post
        {
            Id = id ?? 1,
            AuthorId = authorId ?? 1,
            Body = body ?? "Test post body",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Author = author ?? CreateUserAccount(id: authorId ?? 1)
        };
    }

    public static Follow CreateFollow(
        long? followerId = null,
        long? followedId = null,
        UserAccount? follower = null,
        UserAccount? followed = null)
    {
        return new Follow
        {
            FollowerId = followerId ?? 1,
            FollowedId = followedId ?? 2,
            Follower = follower ?? CreateUserAccount(id: followerId ?? 1),
            Followed = followed ?? CreateUserAccount(id: followedId ?? 2),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public static Session CreateSession(
        Guid? id = null,
        long? userAccountId = null,
        string? tokenHash = null,
        DateTimeOffset? revokedAtUtc = null)
    {
        return new Session
        {
            Id = id ?? Guid.NewGuid(),
            UserAccountId = userAccountId ?? 1,
            TokenHash = tokenHash ?? Guid.NewGuid().ToString(),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            RevokedAtUtc = revokedAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public static Notification CreateNotification(
        long? id = null,
        long? recipientUserId = null,
        long? actorUserId = null,
        string? actorUsername = null,
        string? actorDisplayName = null,
        string? kind = null,
        long? profileUserId = null,
        long? postId = null,
        long? replyPostId = null,
        DateTimeOffset? readAtUtc = null)
    {
        return new Notification
        {
            Id = id ?? 1,
            RecipientUserId = recipientUserId ?? 1,
            ActorUserId = actorUserId ?? 2,
            ActorUsername = actorUsername ?? "actor",
            ActorDisplayName = actorDisplayName ?? "Actor User",
            Kind = kind ?? "follow",
            ProfileUserId = profileUserId,
            PostId = postId,
            ReplyPostId = replyPostId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ReadAtUtc = readAtUtc
        };
    }
}
