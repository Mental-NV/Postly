namespace Postly.Api.Features.Notifications.Contracts;

public record NotificationSummary(
    long Id,
    string Kind,  // "follow" | "like" | "reply"
    string ActorUsername,
    string ActorDisplayName,
    string? ActorAvatarUrl,
    DateTimeOffset CreatedAtUtc,
    bool IsRead,
    string DestinationKind,  // "profile" | "post" | "conversation"
    string DestinationRoute,
    string DestinationState  // "available" | "unavailable"
);

public record NotificationsResponse(
    NotificationSummary[] Notifications
);

public record NotificationDestination(
    string Kind,  // "profile" | "post" | "conversation" | "notification-unavailable"
    string Route,
    string State  // "available" | "unavailable"
);

public record NotificationOpenResponse(
    NotificationSummary Notification,
    NotificationDestination Destination
);
