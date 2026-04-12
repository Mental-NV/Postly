using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Persistence;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class AuthSigninAndSessionFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthSigninAndSessionFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Signin_CreatesNewSessionInDatabase()
    {
        var client = _factory.CreateClient();
        var request = new
        {
            username = "alice",
            password = "TestPassword123"
        };

        var response = await client.PostAsJsonAsync("/api/auth/signin", request);
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == "ALICE");

        Assert.NotNull(user);

        var sessions = await dbContext.Sessions
            .Where(s => s.UserAccountId == user.Id && s.RevokedAtUtc == null)
            .ToListAsync();

        var session = sessions.OrderByDescending(s => s.CreatedAtUtc).FirstOrDefault();

        Assert.NotNull(session);
        Assert.True(session.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Signin_WithExistingSession_RevokesOldSession()
    {
        var client = _factory.CreateClient();
        var request = new
        {
            username = "bob",
            password = "TestPassword123"
        };

        // First signin
        var response1 = await client.PostAsJsonAsync("/api/auth/signin", request);
        response1.EnsureSuccessStatusCode();

        using var scope1 = _factory.Services.CreateScope();
        var dbContext1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await dbContext1.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == "BOB");
        var firstSessionId = (await dbContext1.Sessions
            .Where(s => s.UserAccountId == user!.Id && s.RevokedAtUtc == null)
            .FirstOrDefaultAsync())!.Id;

        // Second signin (new client to simulate new browser)
        var client2 = _factory.CreateClient();
        var response2 = await client2.PostAsJsonAsync("/api/auth/signin", request);
        response2.EnsureSuccessStatusCode();

        using var scope2 = _factory.Services.CreateScope();
        var dbContext2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

        var firstSession = await dbContext2.Sessions.FindAsync(firstSessionId);
        Assert.NotNull(firstSession);
        Assert.NotNull(firstSession.RevokedAtUtc);

        var activeSessions = await dbContext2.Sessions
            .Where(s => s.UserAccountId == user!.Id && s.RevokedAtUtc == null)
            .ToListAsync();
        Assert.Single(activeSessions);
    }

    [Fact]
    public async Task Signout_RevokesSessionInDatabase()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        // First signin
        var signinRequest = new
        {
            username = "charlie",
            password = "TestPassword123"
        };
        await client.PostAsJsonAsync("/api/auth/signin", signinRequest);

        using var scope1 = _factory.Services.CreateScope();
        var dbContext1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await dbContext1.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == "CHARLIE");

        Assert.NotNull(user);

        var session = await dbContext1.Sessions
            .Where(s => s.UserAccountId == user.Id && s.RevokedAtUtc == null)
            .FirstOrDefaultAsync();

        Assert.NotNull(session);
        var sessionId = session.Id;

        // Then signout
        await client.PostAsync("/api/auth/signout", null);

        using var scope2 = _factory.Services.CreateScope();
        var dbContext2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var revokedSession = await dbContext2.Sessions.FindAsync(sessionId);

        Assert.NotNull(revokedSession);
        Assert.NotNull(revokedSession.RevokedAtUtc);
    }

    [Fact]
    public async Task GetSession_ReturnsCurrentUserWhenAuthenticated()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        // First signin
        var signinRequest = new
        {
            username = "alice",
            password = "TestPassword123"
        };
        await client.PostAsJsonAsync("/api/auth/signin", signinRequest);

        // Then get session
        var response = await client.GetAsync("/api/auth/session");
        response.EnsureSuccessStatusCode();

        var sessionData = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(sessionData);
        Assert.Equal("alice", sessionData.Username);
        Assert.Equal("Alice Example", sessionData.DisplayName);
    }

    [Fact]
    public async Task GetSession_Returns401WhenNotAuthenticated()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/auth/session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record SessionResponse(long UserId, string Username, string DisplayName);
}
