using Postly.Api.Features.Timeline.Application;

namespace Postly.Api.Features.Timeline.Endpoints;

public static class TimelineEndpoints
{
    public static void MapTimelineEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/timeline")
            .RequireAuthorization();

        group.MapGet("", async (string? cursor, GetTimelineHandler handler) =>
            await handler.HandleAsync(cursor));
    }
}
