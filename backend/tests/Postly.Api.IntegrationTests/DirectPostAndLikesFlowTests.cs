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

    private async Task SignInAsBob()
    {
        var signinRequest = new { username = "bob", password = "TestPassword123" };
        await _client.PostAsJsonAsync("/api/auth/signin", signinRequest);
    }

    private async Task<long> GetSeededPostIdAsync(string authorUsername)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Posts
            .Where(post => post.Author.Username == authorUsername)
            .Select(post => post.Id)
            .SingleAsync();
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

        var likeResponse1 = await _client.PostAsync($"/api/posts/{alicePostId}/like", null);
        var likeState1 = await likeResponse1.Content.ReadFromJsonAsync<PostInteractionState>();
        var likeResponse2 = await _client.PostAsync($"/api/posts/{alicePostId}/like", null);
        var likeState2 = await likeResponse2.Content.ReadFromJsonAsync<PostInteractionState>();
        var unlikeResponse1 = await _client.DeleteAsync($"/api/posts/{alicePostId}/like");
        var unlikeState1 = await unlikeResponse1.Content.ReadFromJsonAsync<PostInteractionState>();
        var unlikeResponse2 = await _client.DeleteAsync($"/api/posts/{alicePostId}/like");
        var unlikeState2 = await unlikeResponse2.Content.ReadFromJsonAsync<PostInteractionState>();

        Assert.Equal(HttpStatusCode.OK, likeResponse1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, likeResponse2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unlikeResponse1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unlikeResponse2.StatusCode);

        Assert.NotNull(likeState1);
        Assert.NotNull(likeState2);
        Assert.NotNull(unlikeState1);
        Assert.NotNull(unlikeState2);

        Assert.Equal(1, likeState1!.LikeCount);
        Assert.True(likeState1.LikedByViewer);
        Assert.Equal(1, likeState2!.LikeCount);
        Assert.True(likeState2.LikedByViewer);
        Assert.Equal(0, unlikeState1!.LikeCount);
        Assert.False(unlikeState1.LikedByViewer);
        Assert.Equal(0, unlikeState2!.LikeCount);
        Assert.False(unlikeState2.LikedByViewer);
    }

    [Fact]
    public async Task LikeState_IsVisibleAcrossTimelineProfileAndDirectPost()
    {
        await SignInAsBob();
        var alicePostId = await GetSeededPostIdAsync("alice");
        await _client.PostAsync("/api/profiles/alice/follow", null);
        await _client.PostAsync($"/api/posts/{alicePostId}/like", null);

        var timelineResponse = await _client.GetAsync("/api/timeline");
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<TimelineResponse>();
        var profileResponse = await _client.GetAsync("/api/profiles/alice");
        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        var directPostResponse = await _client.GetAsync($"/api/posts/{alicePostId}");
        var directPost = await directPostResponse.Content.ReadFromJsonAsync<PostSummary>();

        Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, directPostResponse.StatusCode);

        var timelinePost = Assert.Single(timeline!.Posts.Where(post => post.Id == alicePostId));
        var profilePost = Assert.Single(profile!.Posts.Where(post => post.Id == alicePostId));

        Assert.True(timelinePost.LikedByViewer);
        Assert.Equal(1, timelinePost.LikeCount);
        Assert.True(profilePost.LikedByViewer);
        Assert.Equal(1, profilePost.LikeCount);
        Assert.True(directPost!.LikedByViewer);
        Assert.Equal(1, directPost.LikeCount);
    }

    [Fact]
    public async Task DirectPost_DeletedAfterAuthentication_ReturnsUnavailableState()
    {
        await SignInAsBob();
        var alicePostId = await GetSeededPostIdAsync("alice");
        await DeletePostAsync(alicePostId);

        var response = await _client.GetAsync($"/api/posts/{alicePostId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OwnershipFlags_AreConsistentAcrossTimelineProfileAndDirectPost()
    {
        await SignInAsBob();
        var alicePostId = await GetSeededPostIdAsync("alice");
        var bobPostId = await GetSeededPostIdAsync("bob");
        await _client.PostAsync("/api/profiles/alice/follow", null);

        var timelineResponse = await _client.GetAsync("/api/timeline");
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<TimelineResponse>();
        var bobProfileResponse = await _client.GetAsync("/api/profiles/bob");
        var bobProfile = await bobProfileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        var aliceProfileResponse = await _client.GetAsync("/api/profiles/alice");
        var aliceProfile = await aliceProfileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        var bobDirectResponse = await _client.GetAsync($"/api/posts/{bobPostId}");
        var bobDirectPost = await bobDirectResponse.Content.ReadFromJsonAsync<PostSummary>();
        var aliceDirectResponse = await _client.GetAsync($"/api/posts/{alicePostId}");
        var aliceDirectPost = await aliceDirectResponse.Content.ReadFromJsonAsync<PostSummary>();

        Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bobProfileResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, aliceProfileResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bobDirectResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, aliceDirectResponse.StatusCode);

        var timelineBobPost = Assert.Single(timeline!.Posts.Where(post => post.Id == bobPostId));
        var timelineAlicePost = Assert.Single(timeline.Posts.Where(post => post.Id == alicePostId));
        var bobProfilePost = Assert.Single(bobProfile!.Posts.Where(post => post.Id == bobPostId));
        var aliceProfilePost = Assert.Single(aliceProfile!.Posts.Where(post => post.Id == alicePostId));

        Assert.True(timelineBobPost.CanEdit);
        Assert.True(timelineBobPost.CanDelete);
        Assert.True(bobProfilePost.CanEdit);
        Assert.True(bobProfilePost.CanDelete);
        Assert.True(bobDirectPost!.CanEdit);
        Assert.True(bobDirectPost.CanDelete);

        Assert.False(timelineAlicePost.CanEdit);
        Assert.False(timelineAlicePost.CanDelete);
        Assert.False(aliceProfilePost.CanEdit);
        Assert.False(aliceProfilePost.CanDelete);
        Assert.False(aliceDirectPost!.CanEdit);
        Assert.False(aliceDirectPost.CanDelete);
    }
}
