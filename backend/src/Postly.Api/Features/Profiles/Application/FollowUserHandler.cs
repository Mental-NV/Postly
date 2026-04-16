using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Persistence;
using Postly.Api.Persistence.Entities;
using Postly.Api.Security;

namespace Postly.Api.Features.Profiles.Application;

public class FollowUserHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;

    public FollowUserHandler(
        AppDbContext dbContext,
        ICurrentViewerAccessor currentViewer,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentViewer = currentViewer;
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync(string username)
    {
        // 1. Get current user
        var currentUserId = _currentViewer.GetCurrentUserId();
        if (currentUserId == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        // 2. Find target user by username (case-insensitive)
        var normalizedUsername = username.ToUpperInvariant();
        var targetUser = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername);

        if (targetUser == null)
        {
            return Results.NotFound(new { error = "User not found" });
        }

        // 3. Check self-follow
        if (targetUser.Id == currentUserId.Value)
        {
            return Results.BadRequest(new { error = "You cannot follow yourself" });
        }

        // 4. Check if follow already exists
        var existingFollow = await _dbContext.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUserId.Value && f.FollowedId == targetUser.Id);

        if (existingFollow != null)
        {
            // Idempotent - already following
            return Results.NoContent();
        }

        // 5. Create follow relationship
        var follow = new Follow
        {
            FollowerId = currentUserId.Value,
            FollowedId = targetUser.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Follows.Add(follow);

        // Create notification
        var currentUser = await _dbContext.UserAccounts.FindAsync(currentUserId.Value);
        _dbContext.Notifications.Add(new Notification
        {
            RecipientUserId = targetUser.Id,
            ActorUserId = currentUserId.Value,
            ActorUsername = currentUser!.Username,
            ActorDisplayName = currentUser.DisplayName,
            Kind = "follow",
            ProfileUserId = targetUser.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        return Results.NoContent();
    }
}
