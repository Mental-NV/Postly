using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Auth.Contracts;
using Postly.Api.Features.Notifications.Contracts;
using Postly.Api.Persistence;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class NotificationsFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public NotificationsFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetNotifications_WhenAuthenticated_ReturnsNotifications()
    {
        // Arrange - Create and sign in a user
        var signupRequest = new SignupRequest("testuser", "Test User", null, "password123");
        var signupResponse = await _client.PostAsJsonAsync("/api/auth/signup", signupRequest);
        signupResponse.EnsureSuccessStatusCode();

        // Act - Get notifications
        var response = await _client.GetAsync("/api/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var notifications = await response.Content.ReadFromJsonAsync<NotificationsResponse>();
        Assert.NotNull(notifications);
        Assert.Empty(notifications.Notifications); // Should be empty for new user
    }

    [Fact]
    public async Task GetNotifications_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}