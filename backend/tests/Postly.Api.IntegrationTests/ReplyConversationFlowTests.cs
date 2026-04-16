using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Persistence.Entities;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class ReplyConversationFlowTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReplyConversationFlowTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task SignInAsBob() =>
        await _client.PostAsJsonAsync("/api/auth/signin", new { username = "bob", password = "TestPassword123" });

    private async Task SignInAsAlice() =>
        await _client.PostAsJsonAsync("/api/auth/signin", new { username = "alice", password = "TestPassword123" });

    private async Task<long> GetAlicePostIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Posts
            .Where(p => p.Author.Username == "alice" && p.ReplyToPostId == null)
            .Select(p => p.Id)
            .FirstAsync();
    }

    [Fact]
    public async Task CreateReply_ValidBody_AppearsInConversation()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();

        var createResponse = await _client.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "Hello from Bob" });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var conversation = await (await _client.GetAsync($"/api/posts/{postId}")).Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.Contains(conversation!.Replies, r => r.Body == "Hello from Bob");
    }

    [Fact]
    public async Task EditReply_ByAuthor_UpdatesBody()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();

        var created = await (await _client.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "Original reply" }))
            .Content.ReadFromJsonAsync<PostResponse>();
        var replyId = created!.Post.Id;

        var editResponse = await _client.PatchAsJsonAsync($"/api/posts/{replyId}", new { body = "Edited reply" });
        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);

        var updated = await editResponse.Content.ReadFromJsonAsync<PostResponse>();
        Assert.Equal("Edited reply", updated!.Post.Body);
        Assert.True(updated.Post.IsEdited);
    }

    [Fact]
    public async Task DeleteReply_ByAuthor_SoftDeletesAndPreservesSlot()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();

        var created = await (await _client.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "Reply to delete" }))
            .Content.ReadFromJsonAsync<PostResponse>();
        var replyId = created!.Post.Id;

        var deleteResponse = await _client.DeleteAsync($"/api/posts/{replyId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify soft-delete in DB
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reply = await dbContext.Posts.FindAsync(replyId);
        Assert.NotNull(reply);
        Assert.NotNull(reply!.DeletedAtUtc);

        // Placeholder appears in conversation
        var conversation = await (await _client.GetAsync($"/api/posts/{postId}")).Content.ReadFromJsonAsync<ConversationResponse>();
        var placeholder = conversation!.Replies.FirstOrDefault(r => r.Id == replyId);
        Assert.NotNull(placeholder);
        Assert.Equal("deleted", placeholder!.State);
        Assert.Null(placeholder.Body);
        Assert.Null(placeholder.AuthorUsername);
    }

    [Fact]
    public async Task DeleteTopLevelPost_HardDeletes()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();

        // Alice deletes her own post
        await SignInAsAlice();
        var deleteResponse = await _client.DeleteAsync($"/api/posts/{postId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Post is gone from DB
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await dbContext.Posts.FindAsync(postId);
        Assert.Null(post);
    }

    [Fact]
    public async Task UnavailableParent_ConversationRouteStillAccessible()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();

        // Bob replies first
        await _client.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "Bob's reply" });

        // Alice deletes her post (hard delete)
        await SignInAsAlice();
        await _client.DeleteAsync($"/api/posts/{postId}");

        // Route still returns 404 (hard-deleted top-level post)
        var response = await _client.GetAsync($"/api/posts/{postId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateReply_UnavailableTarget_Returns404()
    {
        await SignInAsBob();

        var response = await _client.PostAsJsonAsync("/api/posts/999999/replies", new { body = "reply" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateReply_Anonymous_Returns401()
    {
        var freshClient = _factory.CreateClient();
        var postId = await GetAlicePostIdAsync();

        var response = await freshClient.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "reply" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConversation_Anonymous_Returns200WithReplies()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();
        await _client.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "Visible reply" });

        var freshClient = _factory.CreateClient();
        var response = await freshClient.GetAsync($"/api/posts/{postId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);
        Assert.Equal("available", conversation!.Target.State);
        Assert.Contains(conversation.Replies, r => r.Body == "Visible reply");
    }

    [Fact]
    public async Task NonAuthor_CannotEditReply_Returns403()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();

        var created = await (await _client.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "Bob's reply" }))
            .Content.ReadFromJsonAsync<PostResponse>();
        var replyId = created!.Post.Id;

        // Alice tries to edit Bob's reply
        await SignInAsAlice();
        var editResponse = await _client.PatchAsJsonAsync($"/api/posts/{replyId}", new { body = "Alice's edit" });

        Assert.Equal(HttpStatusCode.Forbidden, editResponse.StatusCode);
    }

    [Fact]
    public async Task NonAuthor_CannotDeleteReply_Returns403()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();

        var created = await (await _client.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "Bob's reply" }))
            .Content.ReadFromJsonAsync<PostResponse>();
        var replyId = created!.Post.Id;

        await SignInAsAlice();
        var deleteResponse = await _client.DeleteAsync($"/api/posts/{replyId}");

        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeletedReply_CanEditAndDeleteFlagsAreFalse()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();

        var created = await (await _client.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "Reply to delete" }))
            .Content.ReadFromJsonAsync<PostResponse>();
        var replyId = created!.Post.Id;

        await _client.DeleteAsync($"/api/posts/{replyId}");

        var conversation = await (await _client.GetAsync($"/api/posts/{postId}")).Content.ReadFromJsonAsync<ConversationResponse>();
        var placeholder = conversation!.Replies.First(r => r.Id == replyId);

        Assert.False(placeholder.CanEdit);
        Assert.False(placeholder.CanDelete);
    }
}
