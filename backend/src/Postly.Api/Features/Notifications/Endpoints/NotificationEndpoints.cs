using Postly.Api.Features.Notifications.Application;

namespace Postly.Api.Features.Notifications.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").RequireAuthorization();

        group.MapGet("/", async (GetNotificationsHandler handler) =>
            await handler.HandleAsync());
        group.MapPost("/{notificationId:long}/open", async (long notificationId, OpenNotificationHandler handler) =>
            await handler.HandleAsync(notificationId));
    }
}