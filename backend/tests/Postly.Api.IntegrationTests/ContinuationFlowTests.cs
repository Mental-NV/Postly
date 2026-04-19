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

namespace Postly.Api.IntegrationTests;

public class ContinuationFlowTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ContinuationFlowTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task TimelineContinuation_AppendsOlderPosts_AndRepeatingCursorIsSafe()
    {
        await SignInAsBobAsync();

        var firstPage = await _client.GetFromJsonAsync<TimelineResponse>("/api/timeline");
        Assert.NotNull(firstPage);
        Assert.Equal(20, firstPage!.Posts.Length);
        Assert.NotNull(firstPage.NextCursor);

        var secondPage = await _client.GetFromJsonAsync<TimelineResponse>(
            $"/api/timeline?cursor={Uri.EscapeDataString(firstPage.NextCursor!)}");
        var repeatedSecondPage = await _client.GetFromJsonAsync<TimelineResponse>(
            $"/api/timeline?cursor={Uri.EscapeDataString(firstPage.NextCursor!)}");

        Assert.NotNull(secondPage);
        Assert.NotNull(repeatedSecondPage);
        Assert.NotEmpty(secondPage!.Posts);
        Assert.Equal(
            secondPage.Posts.Select(post => post.Id),
            repeatedSecondPage!.Posts.Select(post => post.Id));
        Assert.DoesNotContain(
            secondPage.Posts.Select(post => post.Id),
            id => firstPage.Posts.Any(post => post.Id == id));
    }

    [Fact]
    public async Task ProfileContinuation_AppendsOlderPosts_AndEventuallyExhausts()
    {
        var firstPage = await _client.GetFromJsonAsync<ProfileResponse>("/api/profiles/alice");
        Assert.NotNull(firstPage);
        Assert.Equal(20, firstPage!.Posts.Length);
        Assert.NotNull(firstPage.NextCursor);

        var secondPage = await _client.GetFromJsonAsync<ProfileResponse>(
            $"/api/profiles/alice?cursor={Uri.EscapeDataString(firstPage.NextCursor!)}");

        Assert.NotNull(secondPage);
        Assert.NotEmpty(secondPage!.Posts);
        Assert.Null(secondPage.NextCursor);
        Assert.DoesNotContain(
            secondPage.Posts.Select(post => post.Id),
            id => firstPage.Posts.Any(post => post.Id == id));
    }

    [Fact]
    public async Task ConversationContinuation_AppendsOlderReplies_AndEventuallyExhausts()
    {
        var conversationPostId = await GetConversationPostIdAsync();

        var firstPage = await _client.GetFromJsonAsync<ConversationResponse>($"/api/posts/{conversationPostId}");
        Assert.NotNull(firstPage);
        Assert.Equal(20, firstPage!.Replies.Length);
        Assert.NotNull(firstPage.NextCursor);

        var secondPage = await _client.GetFromJsonAsync<ConversationResponse>(
            $"/api/posts/{conversationPostId}?cursor={Uri.EscapeDataString(firstPage.NextCursor!)}");

        Assert.NotNull(secondPage);
        Assert.NotEmpty(secondPage!.Replies);
        Assert.Null(secondPage.NextCursor);
        Assert.DoesNotContain(
            secondPage.Replies.Select(reply => reply.Id),
            id => firstPage.Replies.Any(reply => reply.Id == id));
    }

    [Fact]
    public async Task InvalidContinuationCursor_ReturnsProblemDetails()
    {
        await SignInAsBobAsync();

        var response = await _client.GetAsync("/api/timeline?cursor=invalid-cursor");

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
}
