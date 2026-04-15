using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class TimelineFlowTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TimelineFlowTests()
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
    public async Task GetTimeline_NoPostsNoFollows_ReturnsEmptyArray()
    {
        // Arrange
        await SignInAsCharlie();

        // Act
        var response = await _client.GetAsync("/api/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<TimelineResponse>();
        Assert.NotNull(data);
        Assert.Empty(data.Posts);
        Assert.Null(data.NextCursor);
    }

    [Fact]
    public async Task GetTimeline_OwnPostsOnly_ReturnsOwnPosts()
    {
        // Arrange
        await SignInAsBob();

        // Act
        var response = await _client.GetAsync("/api/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<TimelineResponse>();
        Assert.NotNull(data);
        Assert.Equal(2, data.Posts.Length);
        Assert.All(data.Posts, p => Assert.Equal("bob", p.AuthorUsername));
        Assert.All(data.Posts, p => Assert.True(p.CanEdit));
        Assert.All(data.Posts, p => Assert.True(p.CanDelete));
    }

    [Fact]
    public async Task GetTimeline_FollowedUserPosts_ReturnsFollowedPosts()
    {
        // Arrange
        await SignInAsBob();
        await _client.PostAsync("/api/profiles/alice/follow", null);

        // Act
        var response = await _client.GetAsync("/api/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<TimelineResponse>();
        Assert.NotNull(data);
        Assert.True(data.Posts.Length >= 2);

        // Verify both bob's and alice's posts are present
        var usernames = data.Posts.Select(p => p.AuthorUsername).ToHashSet();
        Assert.Contains("bob", usernames);
        Assert.Contains("alice", usernames);
    }

    [Fact]
    public async Task GetTimeline_MixedPosts_ReturnsNewestFirst()
    {
        // Arrange
        await SignInAsBob();

        // Create new post
        var newPostRequest = new { body = "Bob's new post" };
        await _client.PostAsJsonAsync("/api/posts", newPostRequest);

        // Follow alice
        await _client.PostAsync("/api/profiles/alice/follow", null);

        // Act
        var response = await _client.GetAsync("/api/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<TimelineResponse>();
        Assert.NotNull(data);
        Assert.True(data.Posts.Length >= 3);

        // Verify newest first ordering
        Assert.Equal("Bob's new post", data.Posts[0].Body);

        // Verify posts are ordered by CreatedAtUtc descending
        for (int i = 0; i < data.Posts.Length - 1; i++)
        {
            Assert.True(data.Posts[i].CreatedAtUtc >= data.Posts[i + 1].CreatedAtUtc);
        }
    }

    [Fact]
    public async Task GetTimeline_Pagination_ReturnsCursorAndNextPage()
    {
        // Arrange
        await SignInAsBob();

        // Create 25 posts
        for (int i = 0; i < 25; i++)
        {
            var request = new { body = $"Test post {i}" };
            await _client.PostAsJsonAsync("/api/posts", request);
        }

        // Act - First page
        var response1 = await _client.GetAsync("/api/timeline");

        // Assert - First page
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var data1 = await response1.Content.ReadFromJsonAsync<TimelineResponse>();
        Assert.NotNull(data1);
        Assert.Equal(20, data1.Posts.Length);
        Assert.NotNull(data1.NextCursor);

        // Act - Second page
        var response2 = await _client.GetAsync($"/api/timeline?cursor={Uri.EscapeDataString(data1.NextCursor)}");

        // Assert - Second page
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var data2 = await response2.Content.ReadFromJsonAsync<TimelineResponse>();
        Assert.NotNull(data2);
        Assert.Equal(7, data2.Posts.Length); // 25 new + 2 seeded = 27 total, so 7 on page 2
        Assert.Null(data2.NextCursor);
    }

    [Fact]
    public async Task GetTimeline_ViewerContext_SetsCanEditCanDelete()
    {
        // Arrange
        await SignInAsBob();
        await _client.PostAsync("/api/profiles/alice/follow", null);

        // Act
        var response = await _client.GetAsync("/api/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<TimelineResponse>();
        Assert.NotNull(data);

        var bobPost = data.Posts.First(p => p.AuthorUsername == "bob");
        Assert.True(bobPost.CanEdit);
        Assert.True(bobPost.CanDelete);

        var alicePost = data.Posts.First(p => p.AuthorUsername == "alice");
        Assert.False(alicePost.CanEdit);
        Assert.False(alicePost.CanDelete);
    }

    #endregion
}
