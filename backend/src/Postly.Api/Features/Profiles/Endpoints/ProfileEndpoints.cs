using Postly.Api.Features.Profiles.Application;

namespace Postly.Api.Features.Profiles.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profiles")
            .RequireAuthorization();

        // Register /me before /{username} so it always wins route matching
        group.MapGet("me", async (string? cursor, GetProfileHandler handler) =>
            await handler.HandleSelfAsync(cursor));

        group.MapGet("{username}", async (string username, string? cursor, GetProfileHandler handler) =>
            await handler.HandleAsync(username, cursor));

        group.MapPost("{username}/follow", async (string username, FollowUserHandler handler) =>
            await handler.HandleAsync(username));

        group.MapDelete("{username}/follow", async (string username, UnfollowUserHandler handler) =>
            await handler.HandleAsync(username));
    }
}
