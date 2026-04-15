using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Xunit;

namespace Postly.Api.ContractTests;

public class DirectPostAndLikesContractsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DirectPostAndLikesContractsTests(TestWebApplicationFactory factory)
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

    private async Task<long> GetAliceSeedPostIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Posts
            .Where(p => p.Author.Username == "alice" && p.ReplyToPostId == null)
            .Select(p => p.Id)
            .FirstAsync();
    }

    [Fact]
    public async Task GetDirectPost_AuthenticatedVisiblePost_Returns200WithConversationResponse()
    {
        await SignInAsBob();
        var postId = await GetAliceSeedPostIdAsync();

        var response = await _client.GetAsync($"/api/posts/{postId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var conversation = await response.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);
        Assert.Equal("available", conversation!.Target.State);
        Assert.NotNull(conversation.Target.Post);
        Assert.Equal(postId, conversation.Target.Post!.Id);
        Assert.Equal("alice", conversation.Target.Post.AuthorUsername);
        Assert.False(conversation.Target.Post.CanEdit);
        Assert.False(conversation.Target.Post.CanDelete);
    }

    [Fact]
    public async Task GetDirectPost_MissingPost_Returns404Problem()
    {
        await SignInAsBob();

        var response = await _client.GetAsync("/api/posts/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetDirectPost_Unauthenticated_Returns200WithReadOnlyConversation()
    {
        var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var postId = await GetAliceSeedPostIdAsync();

        var response = await freshClient.GetAsync($"/api/posts/{postId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);
        Assert.Equal("available", conversation!.Target.State);
        Assert.False(conversation.Target.Post!.CanEdit);
        Assert.False(conversation.Target.Post.CanDelete);
    }

    [Fact]
    public async Task LikePost_ExistingPost_Returns200WithInteractionState()
    {
        await SignInAsBob();
        var postId = await GetAliceSeedPostIdAsync();

        var response = await _client.PostAsync($"/api/posts/{postId}/like", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var interactionState = await response.Content.ReadFromJsonAsync<PostInteractionState>();
        Assert.NotNull(interactionState);
        Assert.True(interactionState!.LikedByViewer);
        Assert.Equal(1, interactionState.LikeCount);
    }

    [Fact]
    public async Task LikePost_MissingPost_Returns404Problem()
    {
        await SignInAsBob();

        var response = await _client.PostAsync("/api/posts/999999/like", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnlikePost_ExistingPost_Returns200WithInteractionState()
    {
        await SignInAsBob();
        var postId = await GetAliceSeedPostIdAsync();
        await _client.PostAsync($"/api/posts/{postId}/like", null);

        var response = await _client.DeleteAsync($"/api/posts/{postId}/like");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var interactionState = await response.Content.ReadFromJsonAsync<PostInteractionState>();
        Assert.NotNull(interactionState);
        Assert.False(interactionState!.LikedByViewer);
        Assert.Equal(0, interactionState.LikeCount);
    }
}
