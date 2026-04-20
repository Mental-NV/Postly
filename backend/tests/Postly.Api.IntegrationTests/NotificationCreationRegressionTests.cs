using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Notifications.Contracts;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Xunit;

namespace Postly.Api.IntegrationTests;

/// <summary>
/// Regression tests to ensure notifications are created for follow, like, and reply actions.
/// These tests verify the fix for the issue where notification creation code was commented out.
/// </summary>
public class NotificationCreationRegressionTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public NotificationCreationRegressionTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task FollowUser_CreatesNotificationForFollowedUser()
    {
        // Arrange - Sign in as Bob and Alice
        await SignInAsBob();
        await SignInAsAlice();

        // Act - Bob follows Alice
        await SignInAsBob();
        var followResponse = await _client.PostAsync("/api/profiles/alice/follow", null);
        followResponse.EnsureSuccessStatusCode();

        // Assert - Alice should have a follow notification
        await SignInAsAlice();
        var notificationsResponse = await _client.GetAsync("/api/notifications");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<NotificationsResponse>();

        Assert.NotNull(notifications);
        Assert.Single(notifications.Notifications);

        var notification = notifications.Notifications[0];
        Assert.Equal("follow", notification.Kind);
        Assert.Equal("bob", notification.ActorUsername);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task LikePost_CreatesNotificationForPostAuthor()
    {
        // Arrange - Get Alice's post
        var postId = await GetAlicePostIdAsync();

        // Act - Bob likes Alice's post
        await SignInAsBob();
        var likeResponse = await _client.PostAsync($"/api/posts/{postId}/like", null);
        likeResponse.EnsureSuccessStatusCode();

        // Assert - Alice should have a like notification
        await SignInAsAlice();
        var notificationsResponse = await _client.GetAsync("/api/notifications");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<NotificationsResponse>();

        Assert.NotNull(notifications);
        var likeNotification = notifications.Notifications.FirstOrDefault(n => n.Kind == "like");
        Assert.NotNull(likeNotification);
        Assert.Equal("bob", likeNotification.ActorUsername);
        Assert.Equal(postId, ExtractPostIdFromRoute(likeNotification.DestinationRoute));
        Assert.False(likeNotification.IsRead);
    }

    [Fact]
    public async Task LikePost_DoesNotCreateNotificationForOwnPost()
    {
        // Arrange - Get Alice's post
        var postId = await GetAlicePostIdAsync();

        // Act - Alice likes her own post
        await SignInAsAlice();
        var likeResponse = await _client.PostAsync($"/api/posts/{postId}/like", null);
        likeResponse.EnsureSuccessStatusCode();

        // Assert - Alice should have no like notifications (self-action suppression)
        var notificationsResponse = await _client.GetAsync("/api/notifications");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<NotificationsResponse>();

        Assert.NotNull(notifications);
        var likeNotifications = notifications.Notifications.Where(n => n.Kind == "like");
        Assert.Empty(likeNotifications);
    }

    [Fact]
    public async Task CreateReply_CreatesNotificationForPostAuthor()
    {
        // Arrange - Get Alice's post
        var postId = await GetAlicePostIdAsync();

        // Act - Bob creates a reply to Alice's post
        await SignInAsBob();
        var replyResponse = await _client.PostAsJsonAsync($"/api/posts/{postId}/replies",
            new { body = "This is Bob's reply" });
        replyResponse.EnsureSuccessStatusCode();

        // Assert - Alice should have a reply notification
        await SignInAsAlice();
        var notificationsResponse = await _client.GetAsync("/api/notifications");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<NotificationsResponse>();

        Assert.NotNull(notifications);
        var replyNotification = notifications.Notifications.FirstOrDefault(n => n.Kind == "reply");
        Assert.NotNull(replyNotification);
        Assert.Equal("bob", replyNotification.ActorUsername);
        Assert.Equal(postId, ExtractPostIdFromRoute(replyNotification.DestinationRoute));
        Assert.False(replyNotification.IsRead);
    }

    [Fact]
    public async Task CreateReply_DoesNotCreateNotificationForOwnPost()
    {
        // Arrange - Get Alice's post
        var postId = await GetAlicePostIdAsync();

        // Act - Alice replies to her own post
        await SignInAsAlice();
        var replyResponse = await _client.PostAsJsonAsync($"/api/posts/{postId}/replies",
            new { body = "Self reply" });
        replyResponse.EnsureSuccessStatusCode();

        // Assert - Alice should have no reply notifications (self-action suppression)
        var notificationsResponse = await _client.GetAsync("/api/notifications");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<NotificationsResponse>();

        Assert.NotNull(notifications);
        var replyNotifications = notifications.Notifications.Where(n => n.Kind == "reply");
        Assert.Empty(replyNotifications);
    }

    [Fact]
    public async Task MultipleActions_CreateMultipleNotifications()
    {
        // Arrange - Get Alice's post
        var postId = await GetAlicePostIdAsync();

        // Act - Bob follows Alice, likes her post, and replies
        await SignInAsBob();
        await _client.PostAsync("/api/profiles/alice/follow", null);
        await _client.PostAsync($"/api/posts/{postId}/like", null);
        await _client.PostAsJsonAsync($"/api/posts/{postId}/replies", new { body = "Great post!" });

        // Assert - Alice should have 3 notifications
        await SignInAsAlice();
        var notificationsResponse = await _client.GetAsync("/api/notifications");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<NotificationsResponse>();

        Assert.NotNull(notifications);
        Assert.Equal(3, notifications.Notifications.Count);

        // Verify all notification kinds are present
        var kinds = notifications.Notifications.Select(n => n.Kind).ToHashSet();
        Assert.Contains("follow", kinds);
        Assert.Contains("like", kinds);
        Assert.Contains("reply", kinds);

        // All should be from Bob
        Assert.All(notifications.Notifications, n => Assert.Equal("bob", n.ActorUsername));
    }

    [Fact]
    public async Task NotificationCreation_PersistsInDatabase()
    {
        // Act - Bob follows Alice
        await SignInAsBob();
        await _client.PostAsync("/api/profiles/alice/follow", null);

        // Assert - Verify notification exists in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var notification = await dbContext.Notifications
            .Include(n => n.ActorUser)
            .Include(n => n.RecipientUser)
            .FirstOrDefaultAsync(n => n.Kind == "follow" && n.ActorUsername == "bob");

        Assert.NotNull(notification);
        Assert.NotNull(notification!.ActorUser);
        Assert.NotNull(notification.RecipientUser);
        Assert.Equal("bob", notification.ActorUser!.Username);
        Assert.Equal("alice", notification.RecipientUser!.Username);
        Assert.Equal("follow", notification.Kind);
        Assert.Null(notification.ReadAtUtc);
        Assert.True(notification.CreatedAtUtc <= DateTimeOffset.UtcNow);
    }

    // Helper methods
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

    private static long ExtractPostIdFromRoute(string route)
    {
        // Extract post ID from route like "/posts/123"
        var parts = route.Split('/');
        return long.Parse(parts[^1]);
    }
}
