using Postly.Api.Features.Auth.Application;
using Postly.Api.Features.Auth.Contracts;

namespace Postly.Api.Features.Auth.Endpoints;

public static class SigninEndpoints
{
    public static void MapSigninEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/signin", async (
            SigninRequest request,
            SigninHandler handler) =>
        {
            return await handler.HandleAsync(request);
        })
        .AllowAnonymous()
        .WithName("Signin")
        .Produces<SessionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
