using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Posts.Contracts;

namespace Postly.Api.Features.Posts.Endpoints;

public static class PostMutationEndpoints
{
    public static void MapPostMutationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/posts")
            .RequireAuthorization();

        group.MapPost("", async (CreatePostRequest request, CreatePostHandler handler) =>
            await handler.HandleAsync(request));

        group.MapPatch("{postId:long}", async (long postId, UpdatePostRequest request, UpdatePostHandler handler) =>
            await handler.HandleAsync(postId, request));

        group.MapDelete("{postId:long}", async (long postId, DeletePostHandler handler) =>
            await handler.HandleAsync(postId));
    }
}
