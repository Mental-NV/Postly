using Postly.Api.Features.Auth.Application;
using Postly.Api.Features.Auth.Contracts;

namespace Postly.Api.Features.Auth.Endpoints;

public static class SignupEndpoints
{
    public static void MapSignupEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/signup", async (
            SignupRequest request,
            SignupHandler handler) =>
        {
            return await handler.HandleAsync(request);
        })
        .WithName("Signup")
        .Produces<SessionResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
