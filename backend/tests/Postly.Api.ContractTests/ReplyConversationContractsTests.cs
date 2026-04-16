using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Persistence.Entities;
using Xunit;

namespace Postly.Api.ContractTests;

public class ReplyConversationContractsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReplyConversationContractsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        ResetData();
    }

    private void ResetData()
    {
        using var scope = _factory.Services.CreateScope();
        DataSeed.ResetAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>()).GetAwaiter().GetResult();
    }

    private async Task SignInAsBob()
    {
        await _client.PostAsJsonAsync("/api/auth/signin", new { username = "bob", password = "TestPassword123" });
    }

    private async Task<long> GetAlicePostIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Posts
            .Where(p => p.Author.Username == "alice" && p.ReplyToPostId == null)
            .Select(p => p.Id)
            .FirstAsync();
    }

    // GET /api/posts/{postId} — anonymous success
    [Fact]
    public async Task GetConversation_Anonymous_Returns200()
    {
        var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var postId = await GetAlicePostIdAsync();

        var response = await freshClient.GetAsync($"/api/posts/{postId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);
        Assert.Equal("available", conversation!.Target.State);
    }

    // GET /api/posts/{postId}/replies — anonymous success
    [Fact]
    public async Task GetReplies_Anonymous_Returns200()
    {
        var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var postId = await GetAlicePostIdAsync();

        var response = await freshClient.GetAsync($"/api/posts/{postId}/replies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<ReplyPageResponse>();
        Assert.NotNull(page);
    }

    // POST /api/posts/{postId}/replies — anonymous 401
    [Fact]
    public async Task CreateReply_Anonymous_Returns401()
    {
        var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var postId = await GetAlicePostIdAsync();

        var response = await freshClient.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "reply" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // PATCH /api/posts/{postId} — anonymous 401
    [Fact]
    public async Task UpdatePost_Anonymous_Returns401()
    {
        var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var postId = await GetAlicePostIdAsync();

        var response = await freshClient.PatchAsJsonAsync($"/api/posts/{postId}", new { body = "edit" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // DELETE /api/posts/{postId} — anonymous 401
    [Fact]
    public async Task DeletePost_Anonymous_Returns401()
    {
        var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var postId = await GetAlicePostIdAsync();

        var response = await freshClient.DeleteAsync($"/api/posts/{postId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // GET /api/posts/{postId} — conversation response shape
    [Fact]
    public async Task GetConversation_Authenticated_ReturnsConversationShape()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();

        var response = await _client.GetAsync($"/api/posts/{postId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);
        Assert.NotNull(conversation!.Target);
        Assert.Equal("available", conversation.Target.State);
        Assert.NotNull(conversation.Target.Post);
        Assert.NotNull(conversation.Replies);
    }

    // POST /api/posts/{postId}/replies — authenticated creates reply
    [Fact]
    public async Task CreateReply_Authenticated_Returns201WithPostResponse()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();

        var response = await _client.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "My reply" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var postResponse = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(postResponse);
        Assert.True(postResponse!.Post.IsReply);
        Assert.Equal(postId, postResponse.Post.ReplyToPostId);
        Assert.Equal("available", postResponse.Post.State);
    }

    // POST /api/posts/{postId}/replies — unavailable target returns 404
    [Fact]
    public async Task CreateReply_UnavailableTarget_Returns404()
    {
        await SignInAsBob();

        var response = await _client.PostAsJsonAsync("/api/posts/999999/replies", new { body = "My reply" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // DELETE reply — soft-deletes, conversation still accessible
    [Fact]
    public async Task DeleteReply_ByAuthor_SoftDeletesAndConversationRemainsAccessible()
    {
        await SignInAsBob();
        var postId = await GetAlicePostIdAsync();

        // Create a reply
        var createResponse = await _client.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "Reply to delete" });
        var created = await createResponse.Content.ReadFromJsonAsync<PostResponse>();
        var replyId = created!.Post.Id;

        // Delete the reply
        var deleteResponse = await _client.DeleteAsync($"/api/posts/{replyId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Conversation still accessible
        var conversationResponse = await _client.GetAsync($"/api/posts/{postId}");
        Assert.Equal(HttpStatusCode.OK, conversationResponse.StatusCode);

        // Reply appears as deleted placeholder
        var conversation = await conversationResponse.Content.ReadFromJsonAsync<ConversationResponse>();
        var placeholder = conversation!.Replies.FirstOrDefault(r => r.Id == replyId);
        Assert.NotNull(placeholder);
        Assert.Equal("deleted", placeholder!.State);
        Assert.Null(placeholder.Body);
    }
}
