using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Persistence;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class AuthSignupFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthSignupFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Signup_CreatesUserAccountInDatabase()
    {
        var client = _factory.CreateClient();
        var uniqueUsername = $"u{Guid.NewGuid():N}".Substring(0, 15); // Max 20 chars
        var request = new
        {
            username = uniqueUsername,
            displayName = "Test User 1",
            bio = "Test bio",
            password = "TestPassword123"
        };

        var response = await client.PostAsJsonAsync("/api/auth/signup", request);
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == uniqueUsername.ToUpperInvariant());

        Assert.NotNull(user);
        Assert.Equal(uniqueUsername, user.Username);
        Assert.Equal("Test User 1", user.DisplayName);
        Assert.Equal("Test bio", user.Bio);
    }

    [Fact]
    public async Task Signup_CreatesSessionInDatabase()
    {
        var client = _factory.CreateClient();
        var uniqueUsername = $"u{Guid.NewGuid():N}".Substring(0, 15);
        var request = new
        {
            username = uniqueUsername,
            displayName = "Test User 2",
            password = "TestPassword123"
        };

        await client.PostAsJsonAsync("/api/auth/signup", request);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == uniqueUsername.ToUpperInvariant());

        Assert.NotNull(user);

        var session = await dbContext.Sessions
            .FirstOrDefaultAsync(s => s.UserAccountId == user.Id);

        Assert.NotNull(session);
        Assert.Null(session.RevokedAtUtc);
        Assert.True(session.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Signup_WithDuplicateUsername_Returns409WithoutCreatingAccount()
    {
        var client = _factory.CreateClient();
        var uniqueUsername = $"u{Guid.NewGuid():N}".Substring(0, 15);

        // First signup
        var request1 = new
        {
            username = uniqueUsername,
            displayName = "First",
            password = "TestPassword123"
        };
        await client.PostAsJsonAsync("/api/auth/signup", request1);

        // Second signup with same username
        var request2 = new
        {
            username = uniqueUsername,
            displayName = "Second",
            password = "TestPassword456"
        };
        var response = await client.PostAsJsonAsync("/api/auth/signup", request2);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var users = await dbContext.UserAccounts
            .Where(u => u.NormalizedUsername == uniqueUsername.ToUpperInvariant())
            .ToListAsync();

        Assert.Single(users);
    }

    [Fact]
    public async Task Signup_HashesPassword()
    {
        var client = _factory.CreateClient();
        var uniqueUsername = $"u{Guid.NewGuid():N}".Substring(0, 15);
        var request = new
        {
            username = uniqueUsername,
            displayName = "Secure User",
            password = "MySecretPassword123"
        };

        await client.PostAsJsonAsync("/api/auth/signup", request);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == uniqueUsername.ToUpperInvariant());

        Assert.NotNull(user);
        Assert.NotEqual("MySecretPassword123", user.PasswordHash);
        Assert.NotEmpty(user.PasswordHash);
    }

    [Fact]
    public async Task Signup_NormalizesUsername()
    {
        var client = _factory.CreateClient();
        var request = new
        {
            username = "MixedCase",
            displayName = "Test",
            password = "TestPassword123"
        };

        await client.PostAsJsonAsync("/api/auth/signup", request);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == "MIXEDCASE");

        Assert.NotNull(user);
        Assert.Equal("MixedCase", user.Username);
        Assert.Equal("MIXEDCASE", user.NormalizedUsername);
    }
}
