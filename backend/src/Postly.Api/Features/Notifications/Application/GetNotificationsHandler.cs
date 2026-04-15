using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Notifications.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Notifications.Application;

public class GetNotificationsHandler
{
    private readonly AppDbContext _context;
    private readonly ICurrentViewerAccessor _currentViewer;

    public GetNotificationsHandler(AppDbContext context, ICurrentViewerAccessor currentViewer)
    {
        _context = context;
        _currentViewer = currentViewer;
    }

    public async Task<IResult> HandleAsync()
    {
        var userId = _currentViewer.GetCurrentUserId();
        if (userId == null)
            return Results.Unauthorized();

        var notifications = await _context.Notifications
            .Include(n => n.ActorUser)
            .Where(n => n.RecipientUserId == userId.Value)
            .Take(50)
            .ToListAsync();

        // Order in memory since SQLite doesn't support DateTimeOffset ordering
        var orderedNotifications = notifications
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenByDescending(n => n.Id)
            .ToList();

        var summaries = orderedNotifications.Select(n => new NotificationSummary(
            n.Id,
            n.Kind,
            n.ActorUser.Username,
            n.ActorUser.DisplayName,
            n.CreatedAtUtc,
            n.ReadAtUtc.HasValue,
            GetDestinationKind(n),
            GetDestinationRoute(n),
            "available"
        )).ToList();

        return Results.Ok(new NotificationsResponse(summaries));
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

    private static string GetDestinationRoute(Persistence.Entities.Notification notification)
    {
        return notification.Kind switch
        {
            "follow" => $"/u/{notification.ActorUser.Username}",
            "like" => $"/posts/{notification.PostId}",
            "reply" => $"/posts/{notification.PostId}",
            _ => "/"
        };
    }
}