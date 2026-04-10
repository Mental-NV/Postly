using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Profiles.Application;

public class UnfollowUserHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;

    public UnfollowUserHandler(
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

        // 3. Find follow relationship
        var follow = await _dbContext.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUserId.Value && f.FollowedId == targetUser.Id);

        if (follow != null)
        {
            _dbContext.Follows.Remove(follow);
            await _dbContext.SaveChangesAsync();
        }

        // Idempotent - return success even if not following
        return Results.NoContent();
    }
}
