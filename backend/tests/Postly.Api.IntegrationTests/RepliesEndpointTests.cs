using System.Net;
using System.Net.Http.Json;
using Postly.Api.Features.Auth.Contracts;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Timeline.Contracts;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class RepliesEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RepliesEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetReplies_ExistingPostWithReplies_ReturnsReplies()
    {
        // Arrange - Sign up and create post
        var signupRequest = new SignupRequest("testuser", "Test User", null, "password123");
        await _client.PostAsJsonAsync("/api/auth/signup", signupRequest);

        var postRequest = new CreatePostRequest("Original post");
        var postResponse = await _client.PostAsJsonAsync("/api/posts", postRequest);
        var createdPost = await postResponse.Content.ReadFromJsonAsync<PostResponse>();

        // Create a reply
        var replyRequest = new CreateReplyRequest("This is a reply");
        await _client.PostAsJsonAsync($"/api/posts/{createdPost!.Post.Id}/replies", replyRequest);

        // Act - Get replies
        var response = await _client.GetAsync($"/api/posts/{createdPost.Post.Id}/replies");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var replies = await response.Content.ReadFromJsonAsync<ReplyPageResponse>();
        Assert.NotNull(replies);
        Assert.Single(replies.Replies);
        Assert.Equal("This is a reply", replies.Replies[0].Body);
    }

    [Fact]
    public async Task GetReplies_NonExistentPost_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/posts/999/replies");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}