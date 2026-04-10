using Postly.Api.Features.Posts.Application;

namespace Postly.Api.Features.Posts.Endpoints;

public static class PostQueryEndpoints
{
    public static void MapPostQueryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/posts")
            .RequireAuthorization();

        group.MapGet("{postId:long}", async (long postId, GetPostHandler handler) =>
            await handler.HandleAsync(postId));
    }
}
