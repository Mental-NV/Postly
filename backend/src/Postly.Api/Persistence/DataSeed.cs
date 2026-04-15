using Microsoft.AspNetCore.Identity;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.Persistence;

public static class DataSeed
{
    private const string BobPassword = "TestPassword123";
    private const string AlicePassword = "TestPassword123";
    private const string CharliePassword = "TestPassword123";
    public const string BobUsername = "bob";
    public const string BobDisplayName = "Bob Tester";
    public const string BobBio = "Primary seeded user for Postly e2e flows.";
    public const string BobPostBody = "Seed post from Bob";
    public const string AliceUsername = "alice";
    public const string AliceDisplayName = "Alice Example";
    public const string AliceBio = "Seeded profile used for follow, like, and redirect scenarios.";
    public const string AlicePostBody = "Seed post from Alice";

    // Conversation with replies spanning multiple pages
    public const string ConversationPostBody = "Seed conversation post for reply flows";

    public static async Task SeedAsync(AppDbContext context)
    {
        if (context.UserAccounts.Any())
        {
            return;
        }

        var passwordHasher = new PasswordHasher<UserAccount>();
        var now = DateTimeOffset.UtcNow;

        var bob = new UserAccount
        {
            Username = BobUsername,
            NormalizedUsername = BobUsername.ToUpperInvariant(),
            DisplayName = BobDisplayName,
            Bio = BobBio,
            PasswordHash = string.Empty,
            AvatarContentType = null,
            AvatarBytes = null,
            AvatarUpdatedAtUtc = null,
            CreatedAtUtc = now
        };
        bob.PasswordHash = passwordHasher.HashPassword(bob, BobPassword);

        var alice = new UserAccount
        {
            Username = AliceUsername,
            NormalizedUsername = AliceUsername.ToUpperInvariant(),
            DisplayName = AliceDisplayName,
            Bio = AliceBio,
            PasswordHash = string.Empty,
            AvatarContentType = null,
            AvatarBytes = null,
            AvatarUpdatedAtUtc = null,
            CreatedAtUtc = now
        };
        alice.PasswordHash = passwordHasher.HashPassword(alice, AlicePassword);

        var charlie = new UserAccount
        {
            Username = "charlie",
            NormalizedUsername = "CHARLIE",
            DisplayName = "Charlie Test",
            Bio = "Additional test user.",
            PasswordHash = string.Empty,
            AvatarContentType = null,
            AvatarBytes = null,
            AvatarUpdatedAtUtc = null,
            CreatedAtUtc = now
        };
        charlie.PasswordHash = passwordHasher.HashPassword(charlie, CharliePassword);

        context.UserAccounts.AddRange(bob, alice, charlie);
        await context.SaveChangesAsync();

        // Top-level posts
        var alicePost = new Post
        {
            AuthorId = alice.Id,
            Body = AlicePostBody,
            CreatedAtUtc = now
        };

        var bobPost = new Post
        {
            AuthorId = bob.Id,
            Body = BobPostBody,
            CreatedAtUtc = now.AddMinutes(-5)
        };

        // Conversation post (Alice's) with multiple replies for UF-04/05/06/07
        var conversationPost = new Post
        {
            AuthorId = bob.Id,
            Body = ConversationPostBody,
            CreatedAtUtc = now.AddMinutes(-10)
        };

        context.Posts.AddRange(alicePost, bobPost, conversationPost);
        await context.SaveChangesAsync();

        // Seed replies on conversationPost:
        // - One Bob-authored reply (for UF-05: edit/delete own reply)
        // - One Alice-authored reply (for UF-07: non-author has no controls)
        // - Enough replies to span more than one page (21+ for page size 20)
        var replies = new List<Post>();

        var bobReply = new Post
        {
            AuthorId = bob.Id,
            Body = "Bob's seeded reply on the conversation post",
            CreatedAtUtc = now.AddMinutes(-6),
            ReplyToPostId = conversationPost.Id
        };
        replies.Add(bobReply);

        var aliceReply = new Post
        {
            AuthorId = alice.Id,
            Body = "Alice's seeded reply on the conversation post",
            CreatedAtUtc = now.AddMinutes(-6).AddSeconds(30),
            ReplyToPostId = conversationPost.Id
        };
        replies.Add(aliceReply);

        // Add 20 older Charlie replies to push past page boundary
        for (var i = 1; i <= 20; i++)
        {
            replies.Add(new Post
            {
                AuthorId = charlie.Id,
                Body = $"Charlie's seeded reply #{i}",
                CreatedAtUtc = now.AddMinutes(-7).AddSeconds(i),
                ReplyToPostId = conversationPost.Id
            });
        }

        context.Posts.AddRange(replies);
        await context.SaveChangesAsync();
    }

    public static async Task ResetAsync(AppDbContext context)
    {
        context.Likes.RemoveRange(context.Likes);
        context.Follows.RemoveRange(context.Follows);
        context.Posts.RemoveRange(context.Posts);
        context.Sessions.RemoveRange(context.Sessions);
        context.UserAccounts.RemoveRange(context.UserAccounts);
        await context.SaveChangesAsync();

        await SeedAsync(context);
    }
}
