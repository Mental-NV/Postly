using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Postly.Api.ContractTests;

public class OwnPostsContractsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OwnPostsContractsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SignInAsAlice()
    {
        var signinRequest = new { username = "alice", password = "TestPassword123" };
        await _client.PostAsJsonAsync("/api/auth/signin", signinRequest);
    }

    private async Task SignInAsBob()
    {
        var signinRequest = new { username = "bob", password = "TestPassword123" };
        await _client.PostAsJsonAsync("/api/auth/signin", signinRequest);
    }

    #region POST /api/posts

    [Fact]
    public async Task CreatePost_ValidBody_Returns201Created()
    {
        // Arrange
        await SignInAsAlice();
        var request = new { body = "Test post content" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var post = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(post);
        Assert.True(post.Post.Id > 0);
        Assert.Equal("alice", post.Post.AuthorUsername);
        Assert.Equal("Alice Example", post.Post.AuthorDisplayName);
        Assert.Equal("Test post content", post.Post.Body);
        Assert.False(post.Post.IsEdited);
        Assert.True(post.Post.CreatedAtUtc <= DateTimeOffset.UtcNow);
    }

    #endregion

    #region PATCH /api/posts/{postId}

    [Fact]
    public async Task UpdatePost_ValidBodyByAuthor_Returns200OK()
    {
        // Arrange
        await SignInAsAlice();
        var createRequest = new { body = "Original content" };
        var createResponse = await _client.PostAsJsonAsync("/api/posts", createRequest);
        var createdPost = await createResponse.Content.ReadFromJsonAsync<PostResponse>();

        var updateRequest = new { body = "Updated content" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/posts/{createdPost!.Post.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var updatedPost = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(updatedPost);
        Assert.Equal(createdPost.Post.Id, updatedPost.Post.Id);
        Assert.Equal("Updated content", updatedPost.Post.Body);
        Assert.True(updatedPost.Post.IsEdited);
        Assert.Equal(createdPost.Post.CreatedAtUtc, updatedPost.Post.CreatedAtUtc); // Original timestamp preserved
    }

    [Fact]
    public async Task UpdatePost_NonExistentPost_Returns404NotFound()
    {
        // Arrange
        await SignInAsAlice();
        var updateRequest = new { body = "Updated content" };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/posts/999999", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    #endregion

    #region DELETE /api/posts/{postId}

    [Fact]
    public async Task DeletePost_ByAuthor_Returns204NoContent()
    {
        // Arrange
        await SignInAsAlice();
        var createRequest = new { body = "Post to delete" };
        var createResponse = await _client.PostAsJsonAsync("/api/posts", createRequest);
        var createdPost = await createResponse.Content.ReadFromJsonAsync<PostResponse>();

        // Act
        var response = await _client.DeleteAsync($"/api/posts/{createdPost!.Post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeletePost_NonExistentPost_Returns404NotFound()
    {
        // Arrange
        await SignInAsAlice();

        // Act
        var response = await _client.DeleteAsync("/api/posts/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DeletePost_StaleDelete_Returns404NotFound()
    {
        // Arrange
        await SignInAsAlice();
        var createRequest = new { body = "Post to delete twice" };
        var createResponse = await _client.PostAsJsonAsync("/api/posts", createRequest);
        var createdPost = await createResponse.Content.ReadFromJsonAsync<PostResponse>();

        // First delete
        await _client.DeleteAsync($"/api/posts/{createdPost!.Post.Id}");

        // Act: Second delete (stale)
        var response = await _client.DeleteAsync($"/api/posts/{createdPost.Post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    #endregion

    private record PostSummaryDto(
        long Id,
        string? AuthorUsername,
        string? AuthorDisplayName,
        string? Body,
        DateTimeOffset CreatedAtUtc,
        bool IsEdited,
        bool CanEdit,
        bool CanDelete,
        string State
    );

    private record PostResponse(PostSummaryDto Post);
}
