using Postly.Api.Features.Profiles.Application;
using Postly.Api.Features.Profiles.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Postly.Api.Features.Profiles.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profiles");

        group.MapGet("me", async (string? cursor, GetProfileHandler handler) =>
            await handler.HandleSelfAsync(cursor))
            .RequireAuthorization();

        group.MapPatch("me", async (UpdateProfileRequest request, UpdateProfileHandler handler) =>
            await handler.HandleAsync(request))
            .RequireAuthorization();

        group.MapPut("me/avatar", async (IFormFile? avatar, ReplaceAvatarHandler handler) =>
            await handler.HandleAsync(avatar))
            .DisableAntiforgery()
            .RequireAuthorization();

        group.MapGet("{username}", async (string username, string? cursor, GetProfileHandler handler) =>
            await handler.HandleAsync(username, cursor));

        group.MapGet("{username}/avatar", async (
            string username,
            AppDbContext dbContext,
            IHttpContextAccessor httpContextAccessor) =>
        {
            var normalizedUsername = username.ToUpperInvariant();
            var user = await dbContext.UserAccounts
                .FirstOrDefaultAsync(account => account.NormalizedUsername == normalizedUsername);

            if (user == null || !ProfileIdentityProjection.HasCustomAvatar(user))
            {
                var httpContext = httpContextAccessor.HttpContext
                    ?? throw new InvalidOperationException("HttpContext is not available");

                return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("Avatar not found", httpContext.TraceIdentifier));
            }

            return Results.File(
                fileContents: user.AvatarBytes!,
                contentType: ProfileIdentityProjection.AvatarContentType,
                fileDownloadName: $"{user.Username}-avatar.jpg");
        });

        group.MapPost("{username}/follow", async (string username, FollowUserHandler handler) =>
            await handler.HandleAsync(username))
            .RequireAuthorization();

        group.MapDelete("{username}/follow", async (string username, UnfollowUserHandler handler) =>
            await handler.HandleAsync(username))
            .RequireAuthorization();
    }
}
