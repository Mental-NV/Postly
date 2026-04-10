using Microsoft.AspNetCore.Identity;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.Persistence;

public static class DataSeed
{
    private const string BobPassword = "TestPassword123";
    private const string AlicePassword = "TestPassword123";
    private const string CharliePassword = "TestPassword123";

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
            Username = "bob",
            NormalizedUsername = "BOB",
            DisplayName = "Bob Tester",
            Bio = "Primary seeded user for Postly e2e flows.",
            PasswordHash = string.Empty,
            CreatedAtUtc = now
        };
        bob.PasswordHash = passwordHasher.HashPassword(bob, BobPassword);

        var alice = new UserAccount
        {
            Username = "alice",
            NormalizedUsername = "ALICE",
            DisplayName = "Alice Example",
            Bio = "Seeded profile used for follow, like, and redirect scenarios.",
            PasswordHash = string.Empty,
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
            CreatedAtUtc = now
        };
        charlie.PasswordHash = passwordHasher.HashPassword(charlie, CharliePassword);

        context.UserAccounts.AddRange(bob, alice, charlie);
        await context.SaveChangesAsync();

        var alicePost = new Post
        {
            AuthorId = alice.Id,
            Body = "Seed post from Alice",
            CreatedAtUtc = now
        };

        var bobPost = new Post
        {
            AuthorId = bob.Id,
            Body = "Seed post from Bob",
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
