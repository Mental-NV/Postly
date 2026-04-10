using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Profiles.Contracts;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class FollowFlowTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FollowFlowTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    #region Helper Methods

    private async Task SignInAsAlice()
    {
        var request = new { username = "alice", password = "TestPassword123" };
        await _client.PostAsJsonAsync("/api/auth/signin", request);
    }

    private async Task SignInAsBob()
    {
        var request = new { username = "bob", password = "TestPassword123" };
        await _client.PostAsJsonAsync("/api/auth/signin", request);
    }

    private async Task SignInAsCharlie()
    {
        var request = new { username = "charlie", password = "TestPassword123" };
        await _client.PostAsJsonAsync("/api/auth/signin", request);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task FollowUser_ValidTarget_CreatesFollowRelationship()
    {
        // Arrange
        await SignInAsBob();

        // Act
        var response = await _client.PostAsync("/api/profiles/alice/follow", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var bob = await dbContext.UserAccounts.FirstAsync(u => u.Username == "bob");
        var alice = await dbContext.UserAccounts.FirstAsync(u => u.Username == "alice");

        var follow = await dbContext.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == bob.Id && f.FollowedId == alice.Id);

        Assert.NotNull(follow);
    }

    [Fact]
    public async Task FollowUser_SelfFollow_Returns400()
    {
        // Arrange
        await SignInAsBob();

        // Act
        var response = await _client.PostAsync("/api/profiles/bob/follow", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("cannot follow yourself", content.ToLower());
    }

    [Fact]
    public async Task FollowUser_AlreadyFollowing_IsIdempotent()
    {
        // Arrange
        await SignInAsBob();

        // Act - First follow
        var response1 = await _client.PostAsync("/api/profiles/alice/follow", null);

        // Assert - First follow
        Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);

        // Act - Second follow
        var response2 = await _client.PostAsync("/api/profiles/alice/follow", null);

        // Assert - Second follow
        Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);

        // Verify database - Only 1 relationship exists
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var bob = await dbContext.UserAccounts.FirstAsync(u => u.Username == "bob");
        var alice = await dbContext.UserAccounts.FirstAsync(u => u.Username == "alice");

        var followCount = await dbContext.Follows
            .CountAsync(f => f.FollowerId == bob.Id && f.FollowedId == alice.Id);

        Assert.Equal(1, followCount);
    }

    [Fact]
    public async Task FollowUser_NonExistentUser_Returns404()
    {
        // Arrange
        await SignInAsBob();

        // Act
        var response = await _client.PostAsync("/api/profiles/nonexistent/follow", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnfollowUser_ExistingFollow_RemovesRelationship()
    {
        // Arrange
        await SignInAsBob();
        await _client.PostAsync("/api/profiles/alice/follow", null);

        // Act
        var response = await _client.DeleteAsync("/api/profiles/alice/follow");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var bob = await dbContext.UserAccounts.FirstAsync(u => u.Username == "bob");
        var alice = await dbContext.UserAccounts.FirstAsync(u => u.Username == "alice");

        var follow = await dbContext.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == bob.Id && f.FollowedId == alice.Id);

        Assert.Null(follow);
    }

    [Fact]
    public async Task UnfollowUser_NotFollowing_IsIdempotent()
    {
        // Arrange
        await SignInAsBob();

        // Act
        var response = await _client.DeleteAsync("/api/profiles/alice/follow");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var bob = await dbContext.UserAccounts.FirstAsync(u => u.Username == "bob");
        var alice = await dbContext.UserAccounts.FirstAsync(u => u.Username == "alice");

        var follow = await dbContext.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == bob.Id && f.FollowedId == alice.Id);

        Assert.Null(follow);
    }

    [Fact]
    public async Task UnfollowUser_NonExistentUser_Returns404()
    {
        // Arrange
        await SignInAsBob();

        // Act
        var response = await _client.DeleteAsync("/api/profiles/nonexistent/follow");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FollowUnfollow_UpdatesTimeline()
    {
        // Arrange
        await SignInAsBob();

        // Act - Get timeline before following
        var response1 = await _client.GetAsync("/api/timeline");
        var data1 = await response1.Content.ReadFromJsonAsync<TimelineResponse>();

        // Assert - Only bob's post
        Assert.NotNull(data1);
        Assert.Single(data1.Posts);
        Assert.Equal("bob", data1.Posts[0].AuthorUsername);

        // Act - Follow alice
        await _client.PostAsync("/api/profiles/alice/follow", null);
        var response2 = await _client.GetAsync("/api/timeline");
        var data2 = await response2.Content.ReadFromJsonAsync<TimelineResponse>();

        // Assert - Bob's + Alice's posts
        Assert.NotNull(data2);
        Assert.Equal(2, data2.Posts.Length);
        var usernames = data2.Posts.Select(p => p.AuthorUsername).ToHashSet();
        Assert.Contains("bob", usernames);
        Assert.Contains("alice", usernames);

        // Act - Unfollow alice
        await _client.DeleteAsync("/api/profiles/alice/follow");
        var response3 = await _client.GetAsync("/api/timeline");
        var data3 = await response3.Content.ReadFromJsonAsync<TimelineResponse>();

        // Assert - Only bob's post again
        Assert.NotNull(data3);
        Assert.Single(data3.Posts);
        Assert.Equal("bob", data3.Posts[0].AuthorUsername);
    }

    [Fact]
    public async Task FollowUnfollow_UpdatesCounts()
    {
        // Arrange
        await SignInAsBob();

        // Act - Get alice's profile before following
        var response1 = await _client.GetAsync("/api/profiles/alice");
        var data1 = await response1.Content.ReadFromJsonAsync<ProfileResponse>();

        // Assert - Follower count is 0
        Assert.NotNull(data1);
        Assert.Equal(0, data1.Profile.FollowerCount);

        // Act - Follow alice
        await _client.PostAsync("/api/profiles/alice/follow", null);
        var response2 = await _client.GetAsync("/api/profiles/alice");
        var data2 = await response2.Content.ReadFromJsonAsync<ProfileResponse>();

        // Assert - Follower count is 1
        Assert.NotNull(data2);
        Assert.Equal(1, data2.Profile.FollowerCount);

        // Act - Get bob's profile
        var response3 = await _client.GetAsync("/api/profiles/bob");
        var data3 = await response3.Content.ReadFromJsonAsync<ProfileResponse>();

        // Assert - Following count is 1
        Assert.NotNull(data3);
        Assert.Equal(1, data3.Profile.FollowingCount);

        // Act - Unfollow alice
        await _client.DeleteAsync("/api/profiles/alice/follow");
        var response4 = await _client.GetAsync("/api/profiles/alice");
        var data4 = await response4.Content.ReadFromJsonAsync<ProfileResponse>();

        // Assert - Follower count is 0 again
        Assert.NotNull(data4);
        Assert.Equal(0, data4.Profile.FollowerCount);
    }

    [Fact]
    public async Task FollowUser_Unauthorized_Returns401()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.PostAsync("/api/profiles/alice/follow", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
