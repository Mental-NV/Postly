using System.Net;
using System.Net.Http.Json;
using Postly.Api.Features.Auth.Contracts;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class SessionEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SessionEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSession_AuthenticatedUser_ReturnsSessionInfo()
    {
        // Arrange - Sign up user
        var signupRequest = new SignupRequest("testuser", "Test User", null, "password123");
        await _client.PostAsJsonAsync("/api/auth/signup", signupRequest);

        // Act
        var response = await _client.GetAsync("/api/auth/session");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(session);
        Assert.Equal("testuser", session.Username);
        Assert.Equal("Test User", session.DisplayName);
    }

    [Fact]
    public async Task GetSession_Unauthenticated_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/session");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}