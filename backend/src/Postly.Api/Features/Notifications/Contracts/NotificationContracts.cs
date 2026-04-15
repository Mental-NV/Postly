namespace Postly.Api.Features.Notifications.Contracts;

public record NotificationsResponse(
    List<NotificationSummary> Notifications
);

public record NotificationSummary(
    long Id,
    string Kind,
    string ActorUsername,
    string ActorDisplayName,
    DateTimeOffset CreatedAtUtc,
    bool IsRead,
    string DestinationKind,
    string DestinationRoute,
    string DestinationState
);

public record NotificationOpenResponse(
    NotificationSummary Notification,
    NotificationDestination Destination
);

public record NotificationDestination(
    string State,
    string Route
);