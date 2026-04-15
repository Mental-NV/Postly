using Postly.Api.Features.Posts.Application;

namespace Postly.Api.Features.Posts.Endpoints;

public static class PostQueryEndpoints
{
    public static void MapPostQueryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/posts");

        group.MapGet("{postId:long}", async (long postId, string? cursor, GetPostHandler handler) =>
            await handler.HandleAsync(postId, cursor))
            .AllowAnonymous();

        group.MapGet("{postId:long}/replies", async (long postId, string? cursor, GetRepliesHandler handler) =>
            await handler.HandleAsync(postId, cursor))
            .AllowAnonymous();
    }
}
