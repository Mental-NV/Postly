using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Notifications.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Notifications.Application;

public class GetNotificationsHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;

    public GetNotificationsHandler(
        AppDbContext dbContext,
        ICurrentViewerAccessor currentViewer,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentViewer = currentViewer;
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync()
    {
        var userId = _currentViewer.GetCurrentUserId();
        if (userId == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        var notifications = await _dbContext.Notifications
            .Include(n => n.Actor)
            .Where(n => n.RecipientUserId == userId.Value)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenByDescending(n => n.Id)
            .ToListAsync();

        var summaries = new List<NotificationSummary>();
        foreach (var notification in notifications)
        {
            var (destinationKind, destinationRoute, destinationState) = await ResolveDestinationAsync(notification);
            
            var avatarUrl = notification.Actor.AvatarUpdatedAtUtc.HasValue
                ? $"/api/profiles/{notification.Actor.Username}/avatar?v={notification.Actor.AvatarUpdatedAtUtc.Value.ToUnixTimeSeconds()}"
                : null;

            summaries.Add(new NotificationSummary(
                notification.Id,
                notification.Kind,
                notification.Actor.Username,
                notification.Actor.DisplayName,
                avatarUrl,
                notification.CreatedAtUtc,
                notification.ReadAtUtc.HasValue,
                destinationKind,
                destinationRoute,
                destinationState
            ));
        }

        return Results.Ok(new NotificationsResponse(summaries.ToArray()));
    }

    private async Task<(string Kind, string Route, string State)> ResolveDestinationAsync(Persistence.Entities.Notification notification)
    {
        return notification.Kind switch
        {
            "follow" => await ResolveProfileDestinationAsync(notification.ProfileUserId!.Value),
            "like" => await ResolvePostDestinationAsync(notification.PostId!.Value),
            "reply" => await ResolveConversationDestinationAsync(notification.PostId!.Value),
            _ => ("notification-unavailable", "/notifications/unavailable", "unavailable")
        };
    }

    private async Task<(string Kind, string Route, string State)> ResolveProfileDestinationAsync(long profileUserId)
    {
        var user = await _dbContext.UserAccounts
            .Where(u => u.Id == profileUserId)
            .Select(u => new { u.Username })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return ("notification-unavailable", "/notifications/unavailable", "unavailable");
        }

        return ("profile", $"/u/{user.Username}", "available");
    }

    private async Task<(string Kind, string Route, string State)> ResolvePostDestinationAsync(long postId)
    {
        var post = await _dbContext.Posts
            .Where(p => p.Id == postId && p.DeletedAtUtc == null)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync();

        if (post == null)
        {
            return ("notification-unavailable", "/notifications/unavailable", "unavailable");
        }

        return ("post", $"/posts/{post.Id}", "available");
    }

    private async Task<(string Kind, string Route, string State)> ResolveConversationDestinationAsync(long postId)
    {
        var post = await _dbContext.Posts
            .Where(p => p.Id == postId)
            .Select(p => new { p.Id, p.DeletedAtUtc })
            .FirstOrDefaultAsync();

        if (post == null)
        {
            return ("notification-unavailable", "/notifications/unavailable", "unavailable");
        }

        return ("conversation", $"/posts/{post.Id}", "available");
    }
}
