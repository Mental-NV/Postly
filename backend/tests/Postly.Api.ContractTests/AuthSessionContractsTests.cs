using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Postly.Api.ContractTests;

public class AuthSessionContractsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthSessionContractsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Signin_WithValidCredentials_Returns200WithSessionResponse()
    {
        // Use seeded user: alice / TestPassword123
        var request = new
        {
            username = "alice",
            password = "TestPassword123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signin", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", contentType);

        var sessionResponse = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(sessionResponse);
        Assert.Equal("alice", sessionResponse.Username);
        Assert.Equal("Alice Example", sessionResponse.DisplayName);
        Assert.True(sessionResponse.UserId > 0);

        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Signin_WithUnknownUsername_Returns401WithGenericError()
    {
        var request = new
        {
            username = "unknownuser",
            password = "SomePassword123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signin", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/problem+json", contentType);
    }

    [Fact]
    public async Task Signin_WithIncorrectPassword_Returns401WithGenericError()
    {
        var request = new
        {
            username = "alice",
            password = "WrongPassword123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signin", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/problem+json", contentType);
    }

    [Fact]
    public async Task Signin_WithMissingFields_Returns400()
    {
        var request = new
        {
            username = "",
            password = ""
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signin", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/problem+json", contentType);
    }

    [Fact]
    public async Task Signout_WhenAuthenticated_Returns204()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        // First signin
        var signinRequest = new
        {
            username = "alice",
            password = "TestPassword123"
        };
        var signinResponse = await client.PostAsJsonAsync("/api/auth/signin", signinRequest);
        signinResponse.EnsureSuccessStatusCode();

        // Then signout
        var signoutResponse = await client.PostAsync("/api/auth/signout", null);

        Assert.Equal(HttpStatusCode.NoContent, signoutResponse.StatusCode);
    }

    [Fact]
    public async Task Signout_WhenNotAuthenticated_Returns401()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/auth/signout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSession_WhenAuthenticated_Returns200WithSessionResponse()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        // First signin
        var signinRequest = new
        {
            username = "alice",
            password = "TestPassword123"
        };
        var signinResponse = await client.PostAsJsonAsync("/api/auth/signin", signinRequest);
        signinResponse.EnsureSuccessStatusCode();

        // Then get session
        var sessionResponse = await client.GetAsync("/api/auth/session");

        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);

        var sessionData = await sessionResponse.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(sessionData);
        Assert.Equal("alice", sessionData.Username);
        Assert.Equal("Alice Example", sessionData.DisplayName);
    }

    [Fact]
    public async Task GetSession_WhenNotAuthenticated_Returns401()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record SessionResponse(long UserId, string Username, string DisplayName);
}
