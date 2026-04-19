using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Profiles.Contracts;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Xunit;

namespace Postly.Api.ContractTests;

public class ContinuationContractsTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ContinuationContractsTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        ResetData();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetTimeline_ReturnsContinuationShape()
    {
        await SignInAsBobAsync();

        var response = await _client.GetAsync("/api/timeline");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var timeline = await response.Content.ReadFromJsonAsync<TimelineResponse>();
        Assert.NotNull(timeline);
        Assert.NotEmpty(timeline!.Posts);
        Assert.NotNull(timeline.NextCursor);
    }

    [Fact]
    public async Task GetProfile_ReturnsContinuationShape()
    {
        var response = await _client.GetAsync("/api/profiles/alice");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("alice", profile!.Profile.Username);
        Assert.NotNull(profile.NextCursor);
    }

    [Fact]
    public async Task GetConversation_ReturnsContinuationShape()
    {
        var conversationPostId = await GetConversationPostIdAsync();

        var response = await _client.GetAsync($"/api/posts/{conversationPostId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);
        Assert.Equal("available", conversation!.Target.State);
        Assert.NotNull(conversation.NextCursor);
    }

    [Theory]
    [InlineData("/api/timeline?cursor=invalid-cursor", true)]
    [InlineData("/api/profiles/alice?cursor=invalid-cursor", false)]
    public async Task InvalidContinuationCursor_ReturnsProblemDetails(string path, bool requiresAuth)
    {
        if (requiresAuth)
        {
            await SignInAsBobAsync();
        }

        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetConversation_WithInvalidCursor_ReturnsProblemDetails()
    {
        var conversationPostId = await GetConversationPostIdAsync();

        var response = await _client.GetAsync($"/api/posts/{conversationPostId}?cursor=invalid-cursor");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private async Task SignInAsBobAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/signin", new
        {
            username = "bob",
            password = "TestPassword123"
        });
    }

    private async Task<long> GetConversationPostIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Posts
            .Where(post => post.Body == DataSeed.ConversationPostBody)
            .Select(post => post.Id)
            .SingleAsync();
    }

    private void ResetData()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DataSeed.ResetAsync(dbContext).GetAwaiter().GetResult();
    }
}
