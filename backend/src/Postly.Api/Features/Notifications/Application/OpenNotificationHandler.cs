using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Notifications.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Notifications.Application;

public class OpenNotificationHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;

    public OpenNotificationHandler(
        AppDbContext dbContext,
        ICurrentViewerAccessor currentViewer,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentViewer = currentViewer;
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync(long notificationId)
    {
        var userId = _currentViewer.GetCurrentUserId();
        if (userId == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        var notification = await _dbContext.Notifications
            .Include(n => n.Actor)
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId.Value);

        if (notification == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("Notification not found", _httpContext.TraceIdentifier));
        }

        // Mark as read if not already
        if (!notification.ReadAtUtc.HasValue)
        {
            notification.ReadAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        // Resolve destination
        var destination = await ResolveDestinationAsync(notification);

        var avatarUrl = notification.Actor.AvatarUpdatedAtUtc.HasValue
            ? $"/api/profiles/{notification.Actor.Username}/avatar?v={notification.Actor.AvatarUpdatedAtUtc.Value.ToUnixTimeSeconds()}"
            : null;

        var summary = new NotificationSummary(
            notification.Id,
            notification.Kind,
            notification.Actor.Username,
            notification.Actor.DisplayName,
            avatarUrl,
            notification.CreatedAtUtc,
            notification.ReadAtUtc.HasValue,
            destination.Kind,
            destination.Route,
            destination.State
        );

        return Results.Ok(new NotificationOpenResponse(summary, destination));
    }

    private async Task<NotificationDestination> ResolveDestinationAsync(Persistence.Entities.Notification notification)
    {
        return notification.Kind switch
        {
            "follow" => await ResolveProfileDestinationAsync(notification.ProfileUserId!.Value),
            "like" => await ResolvePostDestinationAsync(notification.PostId!.Value),
            "reply" => await ResolveConversationDestinationAsync(notification.PostId!.Value),
            _ => new NotificationDestination("notification-unavailable", "/notifications/unavailable", "unavailable")
        };
    }

    private async Task<NotificationDestination> ResolveProfileDestinationAsync(long profileUserId)
    {
        var user = await _dbContext.UserAccounts
            .Where(u => u.Id == profileUserId)
            .Select(u => new { u.Username })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return new NotificationDestination("notification-unavailable", "/notifications/unavailable", "unavailable");
        }

        return new NotificationDestination("profile", $"/u/{user.Username}", "available");
    }

    private async Task<NotificationDestination> ResolvePostDestinationAsync(long postId)
    {
        var post = await _dbContext.Posts
            .Where(p => p.Id == postId && p.DeletedAtUtc == null)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync();

        if (post == null)
        {
            return new NotificationDestination("notification-unavailable", "/notifications/unavailable", "unavailable");
        }

        return new NotificationDestination("post", $"/posts/{post.Id}", "available");
    }

    private async Task<NotificationDestination> ResolveConversationDestinationAsync(long postId)
    {
        var post = await _dbContext.Posts
            .Where(p => p.Id == postId)
            .Select(p => new { p.Id, p.DeletedAtUtc })
            .FirstOrDefaultAsync();

        if (post == null)
        {
            return new NotificationDestination("notification-unavailable", "/notifications/unavailable", "unavailable");
        }

        return new NotificationDestination("conversation", $"/posts/{post.Id}", "available");
    }
}
