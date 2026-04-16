using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Notifications.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Persistence.Entities;
using Xunit;

namespace Postly.Api.ContractTests;

public class NotificationsContractsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationsContractsTests(TestWebApplicationFactory factory)
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

    private async Task<long> CreateNotificationForBob(string kind = "follow")
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bob = await dbContext.UserAccounts.FirstAsync(u => u.Username == "bob");
        var alice = await dbContext.UserAccounts.FirstAsync(u => u.Username == "alice");
        
        var notification = new Notification
        {
            RecipientUserId = bob.Id,
            ActorUserId = alice.Id,
            ActorUsername = alice.Username,
            ActorDisplayName = alice.DisplayName,
            Kind = kind,
            ProfileUserId = kind == "follow" ? bob.Id : null,
            PostId = kind != "follow" ? await dbContext.Posts.Where(p => p.AuthorId == bob.Id).Select(p => p.Id).FirstAsync() : null,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();
        return notification.Id;
    }

    [Fact]
    public async Task GetNotifications_Authenticated_Returns200WithNotificationsResponse()
    {
        await SignInAsBob();
        await CreateNotificationForBob();

        var response = await _client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var result = await response.Content.ReadFromJsonAsync<NotificationsResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result!.Notifications);
    }

    [Fact]
    public async Task GetNotifications_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNotifications_EmptyList_Returns200WithEmptyArray()
    {
        await SignInAsBob();

        // Clear Bob's seeded notifications
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bob = await dbContext.UserAccounts.FirstAsync(u => u.Username == "bob");
            var bobNotifications = await dbContext.Notifications.Where(n => n.RecipientUserId == bob.Id).ToListAsync();
            dbContext.Notifications.RemoveRange(bobNotifications);
            await dbContext.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<NotificationsResponse>();
        Assert.NotNull(result);
        Assert.Empty(result!.Notifications);
    }

    [Fact]
    public async Task GetNotifications_OrderedByCreatedAtDesc_ReturnsNewestFirst()
    {
        await SignInAsBob();
        var id1 = await CreateNotificationForBob();
        await Task.Delay(10);
        var id2 = await CreateNotificationForBob();

        var response = await _client.GetAsync("/api/notifications");

        var result = await response.Content.ReadFromJsonAsync<NotificationsResponse>();
        Assert.NotNull(result);
        Assert.True(result!.Notifications.Count >= 2);
        Assert.Equal(id2, result.Notifications[0].Id);
        Assert.Equal(id1, result.Notifications[1].Id);
    }

    [Fact]
    public async Task OpenNotification_ValidId_Returns200WithNotificationOpenResponse()
    {
        await SignInAsBob();
        var notificationId = await CreateNotificationForBob();

        var response = await _client.PostAsync($"/api/notifications/{notificationId}/open", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var result = await response.Content.ReadFromJsonAsync<NotificationOpenResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result!.Notification);
        Assert.NotNull(result.Destination);
        Assert.True(result.Notification.IsRead);
    }

    [Fact]
    public async Task OpenNotification_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/notifications/1/open", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OpenNotification_NotFound_Returns404()
    {
        await SignInAsBob();

        var response = await _client.PostAsync("/api/notifications/999999/open", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenNotification_NotOwnedByUser_Returns404()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var alice = await dbContext.UserAccounts.FirstAsync(u => u.Username == "alice");
        var charlie = await dbContext.UserAccounts.FirstAsync(u => u.Username == "charlie");
        
        var notification = new Notification
        {
            RecipientUserId = charlie.Id,
            ActorUserId = alice.Id,
            ActorUsername = alice.Username,
            ActorDisplayName = alice.DisplayName,
            Kind = "follow",
            ProfileUserId = charlie.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        await SignInAsBob();
        var response = await _client.PostAsync($"/api/notifications/{notification.Id}/open", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenNotification_AvailableDestination_ReturnsCorrectRoute()
    {
        await SignInAsBob();
        var notificationId = await CreateNotificationForBob("follow");

        var response = await _client.PostAsync($"/api/notifications/{notificationId}/open", null);

        var result = await response.Content.ReadFromJsonAsync<NotificationOpenResponse>();
        Assert.NotNull(result);
        Assert.Equal("available", result!.Destination.State);
        Assert.Equal("/u/alice", result.Destination.Route);
    }

    [Fact]
    public async Task OpenNotification_UnavailableDestination_ReturnsUnavailableRoute()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bob = await dbContext.UserAccounts.FirstAsync(u => u.Username == "bob");
        var alice = await dbContext.UserAccounts.FirstAsync(u => u.Username == "alice");
        
        // Create a post and then delete it
        var post = new Post
        {
            AuthorId = bob.Id,
            Body = "Post to be deleted",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            DeletedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        // Create a like notification for the deleted post
        var notification = new Notification
        {
            RecipientUserId = bob.Id,
            ActorUserId = alice.Id,
            ActorUsername = alice.Username,
            ActorDisplayName = alice.DisplayName,
            Kind = "like",
            PostId = post.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        await SignInAsBob();
        var response = await _client.PostAsync($"/api/notifications/{notification.Id}/open", null);

        var result = await response.Content.ReadFromJsonAsync<NotificationOpenResponse>();
        Assert.NotNull(result);
        Assert.Equal("unavailable", result!.Destination.State);
        Assert.Equal("/notifications/unavailable", result.Destination.Route);
    }

    [Fact]
    public async Task OpenNotification_FollowWithDeletedUser_ReturnsUnavailableDestination()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bob = await dbContext.UserAccounts.FirstAsync(u => u.Username == "bob");
        var alice = await dbContext.UserAccounts.FirstAsync(u => u.Username == "alice");
        
        // Create a follow notification
        var notification = new Notification
        {
            RecipientUserId = bob.Id,
            ActorUserId = alice.Id,
            ActorUsername = alice.Username,
            ActorDisplayName = alice.DisplayName,
            Kind = "follow",
            ProfileUserId = bob.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        // Delete alice - ActorUserId will be set to null automatically
        dbContext.UserAccounts.Remove(alice);
        await dbContext.SaveChangesAsync();

        await SignInAsBob();
        var response = await _client.PostAsync($"/api/notifications/{notification.Id}/open", null);

        var result = await response.Content.ReadFromJsonAsync<NotificationOpenResponse>();
        Assert.NotNull(result);
        Assert.Equal("unavailable", result!.Destination.State);
        Assert.Equal("/notifications/unavailable", result.Destination.Route);
        // Verify denormalized fields are preserved
        Assert.Equal("alice", result.Notification.ActorUsername);
        Assert.Equal("Alice Example", result.Notification.ActorDisplayName);
    }
}
