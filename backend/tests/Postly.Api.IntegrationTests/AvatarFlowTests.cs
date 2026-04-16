using System.Net;
using System.Net.Http.Json;
using Postly.Api.Features.Auth.Contracts;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class AvatarFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AvatarFlowTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ReplaceAvatar_Unauthenticated_Returns401()
    {
        // Arrange
        var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==");
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(imageBytes), "avatar", "test.png");

        // Act
        var response = await _client.PutAsync("/api/profiles/me/avatar", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAvatar_NonExistentUser_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/profiles/nonexistent/avatar");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}