using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Profiles.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
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

        // 3. Calculate profile context
        var followerCount = await _dbContext.Follows
            .CountAsync(f => f.FollowedId == targetUser.Id);

        var followingCount = await _dbContext.Follows
            .CountAsync(f => f.FollowerId == targetUser.Id);

        var isSelf = targetUser.Id == currentUserId.Value;

        var isFollowedByViewer = await _dbContext.Follows
            .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowedId == targetUser.Id);

        var profile = new UserProfile(
            targetUser.Username,
            targetUser.DisplayName,
            targetUser.Bio,
            followerCount,
            followingCount,
            isSelf,
            isFollowedByViewer
        );

        // 4. Parse cursor for posts
        DateTimeOffset cursorTime = DateTimeOffset.MaxValue;
        long cursorId = long.MaxValue;

        if (!string.IsNullOrEmpty(cursor))
        {
            if (!TryParseCursor(cursor, out cursorTime, out cursorId))
            {
                return Results.BadRequest(new { error = "Invalid cursor format" });
            }
        }

        // 5. Query posts by this user and load into memory
        var allPosts = await _dbContext.Posts
            .Include(p => p.Author)
            .Where(p => p.AuthorId == targetUser.Id)
            .ToListAsync();

        // Apply cursor filter and sort in memory
        var posts = allPosts
            .Where(p => cursorTime == DateTimeOffset.MaxValue ||
                       p.CreatedAtUtc < cursorTime ||
                       (p.CreatedAtUtc == cursorTime && p.Id < cursorId))
            .OrderByDescending(p => p.CreatedAtUtc)
            .ThenByDescending(p => p.Id)
            .Take(PageSize + 1)
            .ToList();

        // 6. Calculate viewer context for posts
        var postIds = posts.Take(PageSize).Select(p => p.Id).ToList();

        var likeCounts = await _dbContext.Likes
            .Where(l => postIds.Contains(l.PostId))
            .GroupBy(l => l.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);

        var likedByViewer = await _dbContext.Likes
            .Where(l => l.UserAccountId == currentUserId.Value && postIds.Contains(l.PostId))
            .Select(l => l.PostId)
            .ToHashSetAsync();

        // 7. Build response
        var postSummaries = posts.Take(PageSize).Select(p => new PostSummary(
            p.Id,
            p.Author.Username,
            p.Author.DisplayName,
            p.Body,
            p.CreatedAtUtc,
            p.EditedAtUtc != null,
            likeCounts.GetValueOrDefault(p.Id, 0),
            likedByViewer.Contains(p.Id),
            p.AuthorId == currentUserId.Value,
            p.AuthorId == currentUserId.Value
        )).ToArray();

        // 8. Generate next cursor if more posts exist
        string? nextCursor = null;
        if (posts.Count > PageSize)
        {
            var lastPost = posts[PageSize - 1];
            nextCursor = EncodeCursor(lastPost.CreatedAtUtc, lastPost.Id);
        }

        return Results.Ok(new ProfileResponse(profile, postSummaries, nextCursor));
    }

    private static bool TryParseCursor(string cursor, out DateTimeOffset time, out long id)
    {
        time = DateTimeOffset.MinValue;
        id = 0;

        try
        {
            var parts = cursor.Split('_');
            if (parts.Length != 2)
                return false;

            time = DateTimeOffset.Parse(parts[0]);
            id = long.Parse(parts[1]);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string EncodeCursor(DateTimeOffset time, long id)
    {
        return $"{time:O}_{id}";
    }
}
