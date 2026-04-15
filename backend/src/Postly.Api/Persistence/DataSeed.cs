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
    public const string AliceBio =
        "Seeded profile used for follow, like, and redirect scenarios.";
    public const string AlicePostBody = "Seed post from Alice";

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
            // Seed users start without a custom avatar so fallback and
            // replacement flows are deterministic in US1.
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

        context.Posts.AddRange(alicePost, bobPost);
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
