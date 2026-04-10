using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Profiles.Contracts;
using Postly.Api.Persistence;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class ProfilesFlowTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProfilesFlowTests()
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
    public async Task GetProfile_ExistingUser_ReturnsProfile()
    {
        // Arrange
        await SignInAsBob();

        // Act
        var response = await _client.GetAsync("/api/profiles/alice");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(data);
        Assert.Equal("alice", data.Profile.Username);
        Assert.Equal("Alice Example", data.Profile.DisplayName);
    }

    [Fact]
    public async Task GetProfile_NonExistentUser_Returns404()
    {
        // Arrange
        await SignInAsBob();

        // Act
        var response = await _client.GetAsync("/api/profiles/nonexistent");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_OwnProfile_SetsSelfTrue()
    {
        // Arrange
        await SignInAsBob();

        // Act
        var response = await _client.GetAsync("/api/profiles/bob");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(data);
        Assert.True(data.Profile.IsSelf);
        Assert.False(data.Profile.IsFollowedByViewer);
    }

    [Fact]
    public async Task GetProfile_OtherProfile_SetsSelfFalse()
    {
        // Arrange
        await SignInAsBob();

        // Act
        var response = await _client.GetAsync("/api/profiles/alice");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(data);
        Assert.False(data.Profile.IsSelf);
    }

    [Fact]
    public async Task GetProfile_FollowerCounts_AreAccurate()
    {
        // Arrange - Bob follows alice
        await SignInAsBob();
        await _client.PostAsync("/api/profiles/alice/follow", null);

        // Charlie follows alice
        var charlieClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var charlieSigninRequest = new { username = "charlie", password = "TestPassword123" };
        await charlieClient.PostAsJsonAsync("/api/auth/signin", charlieSigninRequest);
        await charlieClient.PostAsync("/api/profiles/alice/follow", null);

        // Act - Bob checks alice's profile
        await SignInAsBob();
        var response = await _client.GetAsync("/api/profiles/alice");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(data);
        Assert.Equal(2, data.Profile.FollowerCount);
        Assert.Equal(0, data.Profile.FollowingCount);
    }

    [Fact]
    public async Task GetProfile_IsFollowedByViewer_ReflectsRelationship()
    {
        // Arrange
        await SignInAsBob();

        // Act - Before following
        var response1 = await _client.GetAsync("/api/profiles/alice");

        // Assert - Before following
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var data1 = await response1.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(data1);
        Assert.False(data1.Profile.IsFollowedByViewer);

        // Act - Follow alice
        await _client.PostAsync("/api/profiles/alice/follow", null);
        var response2 = await _client.GetAsync("/api/profiles/alice");

        // Assert - After following
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var data2 = await response2.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(data2);
        Assert.True(data2.Profile.IsFollowedByViewer);
    }

    [Fact]
    public async Task GetProfile_IncludesPosts_WithPagination()
    {
        // Arrange
        await SignInAsBob();

        // Act
        var response = await _client.GetAsync("/api/profiles/alice");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(data);
        Assert.NotEmpty(data.Posts);
        Assert.Equal("alice", data.Posts[0].AuthorUsername);
    }

    [Fact]
    public async Task GetProfile_Unauthorized_Returns401()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/profiles/alice");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
