using Postly.Api.Features.Auth.Application;
using Postly.Api.Features.Auth.Contracts;

namespace Postly.Api.Features.Auth.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/session", async (GetSessionHandler handler) =>
        {
            return await handler.HandleAsync();
        })
        .WithName("GetSession")
        .Produces<SessionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapPost("/api/auth/signout", async (SignoutHandler handler) =>
        {
            return await handler.HandleAsync();
        })
        .WithName("Signout")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
