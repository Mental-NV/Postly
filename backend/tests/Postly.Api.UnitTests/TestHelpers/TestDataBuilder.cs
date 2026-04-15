using Microsoft.AspNetCore.Identity;
using Moq;
using Postly.Api.Persistence.Entities;
using Postly.Api.Security;

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

    public static UserAccount CreateUser(long id, string username)
    {
        return CreateUserAccount(id, username, username);
    }

    public static Notification CreateNotification(
        long id,
        long recipientUserId,
        long actorUserId,
        string kind,
        long? profileUserId = null,
        long? postId = null,
        long? replyPostId = null)
    {
        return new Notification
        {
            Id = id,
            RecipientUserId = recipientUserId,
            ActorUserId = actorUserId,
            Kind = kind,
            ProfileUserId = profileUserId,
            PostId = postId,
            ReplyPostId = replyPostId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public static Mock<ICurrentViewerAccessor> CreateMockCurrentViewer(long userId)
    {
        var mock = new Mock<ICurrentViewerAccessor>();
        mock.Setup(x => x.GetCurrentUserId()).Returns(userId);
        return mock;
    }
}
