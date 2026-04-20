using Microsoft.AspNetCore.Identity;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.UnitTests.TestHelpers;

public static class TestDataBuilder
{
    public static (UserAccount Bob, UserAccount Alice, UserAccount Charlie) CreateRound2Users()
    {
        var bob = CreateUserAccount(
            id: 1,
            username: "bob",
            displayName: "Bob Tester",
            bio: "Primary seeded user for Postly e2e flows.",
            password: "TestPassword123");
        var alice = CreateUserAccount(
            id: 2,
            username: "alice",
            displayName: "Alice Example",
            bio: "Conversation and profile fixture user.",
            password: "TestPassword123");
        var charlie = CreateUserAccount(
            id: 3,
            username: "charlie",
            displayName: "Charlie Example",
            bio: "Additional seeded social graph user.",
            password: "TestPassword123");

        return (bob, alice, charlie);
    }

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
        UserAccount? author = null,
        DateTimeOffset? createdAtUtc = null)
    {
        return new Post
        {
            Id = id ?? 1,
            AuthorId = authorId ?? 1,
            Body = body ?? "Test post body",
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow,
            Author = author ?? CreateUserAccount(id: authorId ?? 1)
        };
    }

    public static Post CreateReply(
        long? id = null,
        long? authorId = null,
        long? replyToPostId = null,
        string? body = null,
        UserAccount? author = null,
        DateTimeOffset? createdAtUtc = null)
    {
        return new Post
        {
            Id = id ?? 1,
            AuthorId = authorId ?? 1,
            Author = author ?? CreateUserAccount(id: authorId ?? 1),
            ReplyToPostId = replyToPostId,
            Body = body ?? "Test reply body",
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow
        };
    }

    public static (Post ParentPost, List<Post> Replies) CreateAvailableConversationThread(
        UserAccount parentAuthor,
        int replyCount = 22,
        long parentPostId = 100,
        long firstReplyId = 200,
        string parentBody = "Conversation target")
    {
        var parentPost = CreatePost(
            id: parentPostId,
            authorId: parentAuthor.Id,
            author: parentAuthor,
            body: parentBody);

        var replies = Enumerable.Range(1, replyCount)
            .Select(index => CreateReply(
                id: firstReplyId + index,
                authorId: parentAuthor.Id,
                author: parentAuthor,
                replyToPostId: parentPost.Id,
                body: $"Reply #{index}",
                createdAtUtc: DateTimeOffset.UtcNow.AddMinutes(-index)))
            .ToList();

        return (parentPost, replies);
    }

    public static (Post ParentPost, List<Post> Replies) CreateUnavailableParentConversationThread(
        UserAccount deletedParentAuthor,
        UserAccount replyAuthor,
        int visibleReplyCount = 1,
        long parentPostId = 1000,
        long firstReplyId = 1100)
    {
        var parentPost = CreatePost(
            id: parentPostId,
            authorId: deletedParentAuthor.Id,
            author: deletedParentAuthor,
            body: "Unavailable conversation target");
        parentPost.DeletedAtUtc = DateTimeOffset.UtcNow;

        var replies = Enumerable.Range(1, visibleReplyCount)
            .Select(index => CreateReply(
                id: firstReplyId + index,
                authorId: replyAuthor.Id,
                author: replyAuthor,
                replyToPostId: parentPost.Id,
                body: $"Visible reply #{index}",
                createdAtUtc: DateTimeOffset.UtcNow.AddMinutes(-index)))
            .ToList();

        return (parentPost, replies);
    }

    public static List<Post> CreateContinuationPosts(
        UserAccount author,
        int count,
        long firstPostId = 1,
        string bodyPrefix = "Continuation post")
    {
        return Enumerable.Range(0, count)
            .Select(index => CreatePost(
                id: firstPostId + index,
                authorId: author.Id,
                author: author,
                body: $"{bodyPrefix} #{index + 1}",
                createdAtUtc: DateTimeOffset.UtcNow.AddMinutes(-index)))
            .ToList();
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

    public static Notification CreateProfileNotification(
        UserAccount recipient,
        UserAccount actor,
        bool isRead = false)
    {
        return CreateNotification(
            recipientUserId: recipient.Id,
            actorUserId: actor.Id,
            actorUsername: actor.Username,
            actorDisplayName: actor.DisplayName,
            kind: "follow",
            profileUserId: actor.Id,
            readAtUtc: isRead ? DateTimeOffset.UtcNow : null);
    }

    public static Notification CreatePostNotification(
        UserAccount recipient,
        UserAccount actor,
        long postId,
        string kind = "like",
        long? replyPostId = null,
        bool isRead = false)
    {
        return CreateNotification(
            recipientUserId: recipient.Id,
            actorUserId: actor.Id,
            actorUsername: actor.Username,
            actorDisplayName: actor.DisplayName,
            kind: kind,
            postId: postId,
            replyPostId: replyPostId,
            readAtUtc: isRead ? DateTimeOffset.UtcNow : null);
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
