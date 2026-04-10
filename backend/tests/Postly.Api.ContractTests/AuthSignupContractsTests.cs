using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Postly.Api.ContractTests;

public class AuthSignupContractsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthSignupContractsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Signup_WithValidData_Returns201WithSessionResponse()
    {
        var uniqueUsername = $"u{Guid.NewGuid():N}".Substring(0, 15);
        var request = new
        {
            username = uniqueUsername,
            displayName = "New User",
            bio = "Test bio",
            password = "TestPassword123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", contentType);

        var sessionResponse = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(sessionResponse);
        Assert.Equal(uniqueUsername, sessionResponse.Username);
        Assert.Equal("New User", sessionResponse.DisplayName);
        Assert.True(sessionResponse.UserId > 0);

        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Signup_WithMissingFields_Returns400WithValidationErrors()
    {
        var request = new
        {
            username = "",
            displayName = "",
            password = ""
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/problem+json", contentType);
    }

    [Fact]
    public async Task Signup_WithInvalidUsername_Returns400()
    {
        var request = new
        {
            username = "ab",  // Too short
            displayName = "Test User",
            password = "TestPassword123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Signup_WithDuplicateUsername_Returns409()
    {
        var uniqueUsername = $"u{Guid.NewGuid():N}".Substring(0, 15);

        // First signup
        var request = new
        {
            username = uniqueUsername,
            displayName = "First User",
            password = "TestPassword123"
        };

        await _client.PostAsJsonAsync("/api/auth/signup", request);

        // Second signup with same username (different case)
        var duplicateRequest = new
        {
            username = uniqueUsername.ToUpper(),
            displayName = "Second User",
            password = "TestPassword456"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", duplicateRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private record SessionResponse(long UserId, string Username, string DisplayName);
}
