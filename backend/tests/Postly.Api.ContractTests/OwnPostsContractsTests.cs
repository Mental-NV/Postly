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
        Assert.True(post.Id > 0);
        Assert.Equal("alice", post.AuthorUsername);
        Assert.Equal("Alice Example", post.AuthorDisplayName);
        Assert.Equal("Test post content", post.Body);
        Assert.False(post.IsEdited);
        Assert.Null(post.EditedAtUtc);
        Assert.True(post.CreatedAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CreatePost_EmptyBody_Returns400BadRequest()
    {
        // Arrange
        await SignInAsAlice();
        var request = new { body = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreatePost_BodyExceeds280Chars_Returns400BadRequest()
    {
        // Arrange
        await SignInAsAlice();
        var request = new { body = new string('a', 281) };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreatePost_Unauthenticated_Returns401Unauthorized()
    {
        // Arrange: Use a fresh client without cookies and no redirect following
        var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var request = new { body = "Test post" };

        // Act
        var response = await freshClient.PostAsJsonAsync("/api/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
        var response = await _client.PatchAsJsonAsync($"/api/posts/{createdPost!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var updatedPost = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(updatedPost);
        Assert.Equal(createdPost.Id, updatedPost.Id);
        Assert.Equal("Updated content", updatedPost.Body);
        Assert.True(updatedPost.IsEdited);
        Assert.NotNull(updatedPost.EditedAtUtc);
        Assert.Equal(createdPost.CreatedAtUtc, updatedPost.CreatedAtUtc); // Original timestamp preserved
    }

    [Fact]
    public async Task UpdatePost_EmptyBody_Returns400BadRequest()
    {
        // Arrange
        await SignInAsAlice();
        var createRequest = new { body = "Original content" };
        var createResponse = await _client.PostAsJsonAsync("/api/posts", createRequest);
        var createdPost = await createResponse.Content.ReadFromJsonAsync<PostResponse>();

        var updateRequest = new { body = "" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/posts/{createdPost!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UpdatePost_BodyExceeds280Chars_Returns400BadRequest()
    {
        // Arrange
        await SignInAsAlice();
        var createRequest = new { body = "Original content" };
        var createResponse = await _client.PostAsJsonAsync("/api/posts", createRequest);
        var createdPost = await createResponse.Content.ReadFromJsonAsync<PostResponse>();

        var updateRequest = new { body = new string('a', 281) };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/posts/{createdPost!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UpdatePost_NonAuthor_Returns403Forbidden()
    {
        // Arrange: Alice creates a post
        await SignInAsAlice();
        var createRequest = new { body = "Alice's post" };
        var createResponse = await _client.PostAsJsonAsync("/api/posts", createRequest);
        var createdPost = await createResponse.Content.ReadFromJsonAsync<PostResponse>();

        // Act: Bob tries to update Alice's post
        await SignInAsBob();
        var updateRequest = new { body = "Bob's edit attempt" };
        var response = await _client.PatchAsJsonAsync($"/api/posts/{createdPost!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
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

    [Fact]
    public async Task UpdatePost_Unauthenticated_Returns401Unauthorized()
    {
        // Arrange: Use a fresh client without cookies and no redirect following
        var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var updateRequest = new { body = "Updated content" };

        // Act
        var response = await freshClient.PatchAsJsonAsync("/api/posts/1", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
        var response = await _client.DeleteAsync($"/api/posts/{createdPost!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeletePost_NonAuthor_Returns403Forbidden()
    {
        // Arrange: Alice creates a post
        await SignInAsAlice();
        var createRequest = new { body = "Alice's post" };
        var createResponse = await _client.PostAsJsonAsync("/api/posts", createRequest);
        var createdPost = await createResponse.Content.ReadFromJsonAsync<PostResponse>();

        // Act: Bob tries to delete Alice's post
        await SignInAsBob();
        var response = await _client.DeleteAsync($"/api/posts/{createdPost!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
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
        await _client.DeleteAsync($"/api/posts/{createdPost!.Id}");

        // Act: Second delete (stale)
        var response = await _client.DeleteAsync($"/api/posts/{createdPost.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DeletePost_Unauthenticated_Returns401Unauthorized()
    {
        // Arrange: Use a fresh client without cookies and no redirect following
        var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await freshClient.DeleteAsync("/api/posts/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    private record PostResponse(
        long Id,
        string AuthorUsername,
        string AuthorDisplayName,
        string Body,
        DateTimeOffset CreatedAtUtc,
        bool IsEdited,
        DateTimeOffset? EditedAtUtc
    );
}
