using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Notifications.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Notifications.Application;

public class OpenNotificationHandler
{
    private readonly AppDbContext _context;
    private readonly ICurrentViewerAccessor _currentViewer;

    public OpenNotificationHandler(AppDbContext context, ICurrentViewerAccessor currentViewer)
    {
        _context = context;
        _currentViewer = currentViewer;
    }

    public async Task<IResult> HandleAsync(long notificationId)
    {
        var userId = _currentViewer.GetCurrentUserId();
        if (userId == null)
            return Results.Unauthorized();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId.Value);

        if (notification == null)
            return Results.NotFound();

        // Mark as read
        if (!notification.ReadAtUtc.HasValue)
        {
            notification.ReadAtUtc = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }

        var destinationState = await CheckDestinationAvailability(notification);
        var destinationRoute = GetDestinationRoute(notification, destinationState);

        var summary = new NotificationSummary(
            notification.Id,
            notification.Kind,
            notification.ActorUsername,
            notification.ActorDisplayName,
            notification.CreatedAtUtc,
            true,
            GetDestinationKind(notification),
            destinationRoute,
            destinationState
        );

        var destination = new NotificationDestination(destinationState, destinationRoute);

        return Results.Ok(new NotificationOpenResponse(summary, destination));
    }

    private async Task<string> CheckDestinationAvailability(Persistence.Entities.Notification notification)
    {
        return notification.Kind switch
        {
            "follow" => await _context.UserAccounts.AnyAsync(u => u.Id == notification.ActorUserId) ? "available" : "unavailable",
            "like" or "reply" => await _context.Posts.AnyAsync(p => p.Id == notification.PostId && p.DeletedAtUtc == null) ? "available" : "unavailable",
            _ => "unavailable"
        };
    }

    private static string GetDestinationRoute(Persistence.Entities.Notification notification, string state)
    {
        if (state == "unavailable")
            return "/notifications/unavailable";

        return notification.Kind switch
        {
            "follow" => $"/u/{notification.ActorUsername}",
            "like" => $"/posts/{notification.PostId}",
            "reply" => $"/posts/{notification.PostId}",
            _ => "/"
        };
    }

    private static string GetDestinationKind(Persistence.Entities.Notification notification)
    {
        return notification.Kind switch
        {
            "follow" => "profile",
            "like" => "post",
            "reply" => "post",
            _ => "unknown"
        };
    }
}