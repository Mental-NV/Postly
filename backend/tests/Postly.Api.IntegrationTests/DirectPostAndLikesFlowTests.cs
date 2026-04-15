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

public class DirectPostAndLikesFlowTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DirectPostAndLikesFlowTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task SignInAsBob()
    {
        await _client.PostAsJsonAsync("/api/auth/signin", new { username = "bob", password = "TestPassword123" });
    }

    private async Task<long> GetSeededPostIdAsync(string authorUsername)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Posts
            .Where(p => p.Author.Username == authorUsername && p.ReplyToPostId == null)
            .Select(p => p.Id)
            .FirstAsync();
    }

    private async Task DeletePostAsync(long postId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await dbContext.Posts.FindAsync(postId);
        Assert.NotNull(post);
        dbContext.Posts.Remove(post!);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task LikeAndUnlike_AreIdempotent()
    {
        await SignInAsBob();
        var alicePostId = await GetSeededPostIdAsync("alice");

        var likeState1 = await (await _client.PostAsync($"/api/posts/{alicePostId}/like", null)).Content.ReadFromJsonAsync<PostInteractionState>();
        var likeState2 = await (await _client.PostAsync($"/api/posts/{alicePostId}/like", null)).Content.ReadFromJsonAsync<PostInteractionState>();
        var unlikeState1 = await (await _client.DeleteAsync($"/api/posts/{alicePostId}/like")).Content.ReadFromJsonAsync<PostInteractionState>();
        var unlikeState2 = await (await _client.DeleteAsync($"/api/posts/{alicePostId}/like")).Content.ReadFromJsonAsync<PostInteractionState>();

        Assert.Equal(1, likeState1!.LikeCount); Assert.True(likeState1.LikedByViewer);
        Assert.Equal(1, likeState2!.LikeCount); Assert.True(likeState2.LikedByViewer);
        Assert.Equal(0, unlikeState1!.LikeCount); Assert.False(unlikeState1.LikedByViewer);
        Assert.Equal(0, unlikeState2!.LikeCount); Assert.False(unlikeState2.LikedByViewer);
    }

    [Fact]
    public async Task LikeState_IsVisibleAcrossTimelineProfileAndDirectPost()
    {
        await SignInAsBob();
        var alicePostId = await GetSeededPostIdAsync("alice");
        await _client.PostAsync("/api/profiles/alice/follow", null);
        await _client.PostAsync($"/api/posts/{alicePostId}/like", null);

        var timeline = await (await _client.GetAsync("/api/timeline")).Content.ReadFromJsonAsync<TimelineResponse>();
        var profile = await (await _client.GetAsync("/api/profiles/alice")).Content.ReadFromJsonAsync<ProfileResponse>();
        var conversation = await (await _client.GetAsync($"/api/posts/{alicePostId}")).Content.ReadFromJsonAsync<ConversationResponse>();

        var timelinePost = Assert.Single(timeline!.Posts, p => p.Id == alicePostId);
        var profilePost = Assert.Single(profile!.Posts, p => p.Id == alicePostId);
        var directPost = conversation!.Target.Post;

        Assert.True(timelinePost.LikedByViewer); Assert.Equal(1, timelinePost.LikeCount);
        Assert.True(profilePost.LikedByViewer); Assert.Equal(1, profilePost.LikeCount);
        Assert.NotNull(directPost);
        Assert.True(directPost!.LikedByViewer); Assert.Equal(1, directPost.LikeCount);
    }

    [Fact]
    public async Task DirectPost_DeletedAfterAuthentication_ReturnsUnavailableState()
    {
        await SignInAsBob();
        var alicePostId = await GetSeededPostIdAsync("alice");
        await DeletePostAsync(alicePostId);

        var response = await _client.GetAsync($"/api/posts/{alicePostId}");

        // Hard-deleted top-level post → 404
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OwnershipFlags_AreConsistentAcrossTimelineProfileAndDirectPost()
    {
        await SignInAsBob();
        var alicePostId = await GetSeededPostIdAsync("alice");
        var bobPostId = await GetSeededPostIdAsync("bob");
        await _client.PostAsync("/api/profiles/alice/follow", null);

        var timeline = await (await _client.GetAsync("/api/timeline")).Content.ReadFromJsonAsync<TimelineResponse>();
        var bobProfile = await (await _client.GetAsync("/api/profiles/bob")).Content.ReadFromJsonAsync<ProfileResponse>();
        var aliceProfile = await (await _client.GetAsync("/api/profiles/alice")).Content.ReadFromJsonAsync<ProfileResponse>();
        var bobConversation = await (await _client.GetAsync($"/api/posts/{bobPostId}")).Content.ReadFromJsonAsync<ConversationResponse>();
        var aliceConversation = await (await _client.GetAsync($"/api/posts/{alicePostId}")).Content.ReadFromJsonAsync<ConversationResponse>();

        var timelineBobPost = Assert.Single(timeline!.Posts, p => p.Id == bobPostId);
        var timelineAlicePost = Assert.Single(timeline.Posts, p => p.Id == alicePostId);
        var bobProfilePost = Assert.Single(bobProfile!.Posts, p => p.Id == bobPostId);
        var aliceProfilePost = Assert.Single(aliceProfile!.Posts, p => p.Id == alicePostId);
        var bobDirectPost = bobConversation!.Target.Post;
        var aliceDirectPost = aliceConversation!.Target.Post;

        Assert.True(timelineBobPost.CanEdit); Assert.True(timelineBobPost.CanDelete);
        Assert.True(bobProfilePost.CanEdit); Assert.True(bobProfilePost.CanDelete);
        Assert.NotNull(bobDirectPost);
        Assert.True(bobDirectPost!.CanEdit); Assert.True(bobDirectPost.CanDelete);

        Assert.False(timelineAlicePost.CanEdit); Assert.False(timelineAlicePost.CanDelete);
        Assert.False(aliceProfilePost.CanEdit); Assert.False(aliceProfilePost.CanDelete);
        Assert.NotNull(aliceDirectPost);
        Assert.False(aliceDirectPost!.CanEdit); Assert.False(aliceDirectPost.CanDelete);
    }
}
