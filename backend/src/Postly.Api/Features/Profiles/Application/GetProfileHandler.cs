using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Profiles.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Shared.Pagination;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Persistence.Entities;
using Postly.Api.Security;

namespace Postly.Api.Features.Profiles.Application;

public class GetProfileHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;
    private const int PageSize = 20;

    public GetProfileHandler(
        AppDbContext dbContext,
        ICurrentViewerAccessor currentViewer,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentViewer = currentViewer;
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync(string username, string? cursor)
    {
        var currentUserId = _currentViewer.GetCurrentUserId();

        var normalizedUsername = username.ToUpperInvariant();
        var targetUser = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername);

        if (targetUser == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("User not found", _httpContext.TraceIdentifier));
        }

        return await BuildProfileResponseAsync(targetUser, currentUserId, cursor);
    }

    public async Task<IResult> HandleSelfAsync(string? cursor)
    {
        // 1. Get current user
        var currentUserId = _currentViewer.GetCurrentUserId();
        if (currentUserId == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        // 2. Find target user by ID
        var targetUser = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.Id == currentUserId.Value);

        if (targetUser == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        // 3. Build and return profile response using shared logic
        return await BuildProfileResponseAsync(targetUser, currentUserId.Value, cursor);
    }

    private async Task<IResult> BuildProfileResponseAsync(UserAccount targetUser, long? currentViewerId, string? cursor)
    {
        var followerCount = await _dbContext.Follows
            .CountAsync(f => f.FollowedId == targetUser.Id);

        var followingCount = await _dbContext.Follows
            .CountAsync(f => f.FollowerId == targetUser.Id);

        var isSelf = currentViewerId != null && targetUser.Id == currentViewerId.Value;

        var isFollowedByViewer = currentViewerId != null && await _dbContext.Follows
            .AnyAsync(f => f.FollowerId == currentViewerId.Value && f.FollowedId == targetUser.Id);

        var profile = new UserProfile(
            targetUser.Username,
            targetUser.DisplayName,
            targetUser.Bio,
            ProfileIdentityProjection.CreateAvatarUrl(targetUser),
            ProfileIdentityProjection.HasCustomAvatar(targetUser),
            followerCount,
            followingCount,
            isSelf,
            isFollowedByViewer
        );

        if (!OpaqueCursor.TryParse(cursor, out var parsedCursor))
        {
            return Results.Problem(ProblemDetailsFactory.CreateValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["cursor"] = ["Cursor must be a valid continuation token."]
                },
                _httpContext.TraceIdentifier));
        }

        var allPosts = await _dbContext.Posts
            .Include(p => p.Author)
            .Where(p => p.AuthorId == targetUser.Id && p.ReplyToPostId == null && p.DeletedAtUtc == null)
            .ToListAsync();

        var page = OpaqueCursorPagination.Paginate(
            allPosts,
            parsedCursor,
            PageSize,
            post => post.CreatedAtUtc,
            post => post.Id);

        var postSummaries = await PostSummaryFactory.CreateManyAsync(
            _dbContext,
            page.Items,
            currentViewerId);

        return Results.Ok(new ProfileResponse(profile, postSummaries, page.NextCursor));
    }
}
