using Postly.Api.Features.Posts.Application;

namespace Postly.Api.Features.Posts.Endpoints;

public static class PostInteractionEndpoints
{
    public static void MapPostInteractionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/posts");

        group.MapPost("{postId:long}/like", async (long postId, LikePostHandler handler) =>
            await handler.HandleAsync(postId));

        group.MapDelete("{postId:long}/like", async (long postId, UnlikePostHandler handler) =>
            await handler.HandleAsync(postId));
    }
}
