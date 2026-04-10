using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Persistence;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class OwnPostsFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OwnPostsFlowTests(WebApplicationFactory<Program> factory)
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

    #region Create Post Flow

    [Fact]
    public async Task CreatePost_ValidBody_CreatesPostInDatabase()
    {
        // Arrange
        await SignInAsAlice();
        var request = new { body = "Integration test post" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await dbContext.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Body == "Integration test post");

        Assert.NotNull(post);
        Assert.Equal("alice", post.Author.Username);
        Assert.Null(post.EditedAtUtc);
        Assert.True(post.CreatedAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CreatePost_281Characters_ReturnsValidationError()
    {
        // Arrange
        await SignInAsAlice();
        var request = new { body = new string('a', 281) };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var postCount = await dbContext.Posts.CountAsync(p => p.Body.Length == 281);

        Assert.Equal(0, postCount); // No post created
    }

    #endregion

    #region Edit Post Flow

    [Fact]
    public async Task UpdatePost_ByAuthor_UpdatesPostInDatabase()
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

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await dbContext.Posts.FindAsync(createdPost.Id);

        Assert.NotNull(post);
        Assert.Equal("Updated content", post.Body);
        Assert.NotNull(post.EditedAtUtc);
        Assert.True(post.EditedAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task UpdatePost_PreservesCreatedAtUtc()
    {
        // Arrange
        await SignInAsAlice();
        var createRequest = new { body = "Original content" };
        var createResponse = await _client.PostAsJsonAsync("/api/posts", createRequest);
        var createdPost = await createResponse.Content.ReadFromJsonAsync<PostResponse>();

        var originalCreatedAt = createdPost!.CreatedAtUtc;

        // Small delay to ensure timestamps would differ
        await Task.Delay(100);

        var updateRequest = new { body = "Updated content" };

        // Act
        await _client.PatchAsJsonAsync($"/api/posts/{createdPost.Id}", updateRequest);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await dbContext.Posts.FindAsync(createdPost.Id);

        Assert.NotNull(post);
        Assert.Equal(originalCreatedAt, post.CreatedAtUtc); // Timestamp unchanged
    }

    [Fact]
    public async Task UpdatePost_NonAuthor_Returns403AndDoesNotUpdate()
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

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await dbContext.Posts.FindAsync(createdPost.Id);

        Assert.NotNull(post);
        Assert.Equal("Alice's post", post.Body); // Content unchanged
        Assert.Null(post.EditedAtUtc); // Not marked as edited
    }

    #endregion

    #region Delete Post Flow

    [Fact]
    public async Task DeletePost_ByAuthor_RemovesPostFromDatabase()
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

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await dbContext.Posts.FindAsync(createdPost.Id);

        Assert.Null(post); // Post deleted
    }

    [Fact]
    public async Task DeletePost_NonAuthor_Returns403AndDoesNotDelete()
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

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await dbContext.Posts.FindAsync(createdPost.Id);

        Assert.NotNull(post); // Post still exists
        Assert.Equal("Alice's post", post.Body);
    }

    [Fact]
    public async Task DeletePost_StaleDelete_Returns404()
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
    }

    #endregion

    #region Ownership Enforcement

    [Fact]
    public async Task OwnershipEnforcement_UserCannotEditOthersPost()
    {
        // Arrange: Alice creates a post
        await SignInAsAlice();
        var createRequest = new { body = "Alice's original post" };
        var createResponse = await _client.PostAsJsonAsync("/api/posts", createRequest);
        var createdPost = await createResponse.Content.ReadFromJsonAsync<PostResponse>();

        // Act: Bob attempts to edit
        await SignInAsBob();
        var updateRequest = new { body = "Bob's unauthorized edit" };
        var updateResponse = await _client.PatchAsJsonAsync($"/api/posts/{createdPost!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);

        // Verify database unchanged
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await dbContext.Posts.FindAsync(createdPost.Id);

        Assert.NotNull(post);
        Assert.Equal("Alice's original post", post.Body);
        Assert.Null(post.EditedAtUtc);
    }

    [Fact]
    public async Task OwnershipEnforcement_UserCannotDeleteOthersPost()
    {
        // Arrange: Alice creates a post
        await SignInAsAlice();
        var createRequest = new { body = "Alice's post to protect" };
        var createResponse = await _client.PostAsJsonAsync("/api/posts", createRequest);
        var createdPost = await createResponse.Content.ReadFromJsonAsync<PostResponse>();

        // Act: Bob attempts to delete
        await SignInAsBob();
        var deleteResponse = await _client.DeleteAsync($"/api/posts/{createdPost!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);

        // Verify post still exists
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await dbContext.Posts.FindAsync(createdPost.Id);

        Assert.NotNull(post);
        Assert.Equal("Alice's post to protect", post.Body);
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
