using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Timeline.Application;

public class GetTimelineHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;
    private const int PageSize = 20;

    public GetTimelineHandler(
        AppDbContext dbContext,
        ICurrentViewerAccessor currentViewer,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentViewer = currentViewer;
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync(string? cursor)
    {
        // 1. Get current user
        var userId = _currentViewer.GetCurrentUserId();
        if (userId == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        // 2. Parse cursor
        DateTimeOffset cursorTime = DateTimeOffset.MaxValue;
        long cursorId = long.MaxValue;

        if (!string.IsNullOrEmpty(cursor))
        {
            if (!TryParseCursor(cursor, out cursorTime, out cursorId))
            {
                return Results.BadRequest(new { error = "Invalid cursor format" });
            }
        }

        // 3. Get followed user IDs (materialize to avoid complex subquery)
        var followedUserIds = await _dbContext.Follows
            .Where(f => f.FollowerId == userId.Value)
            .Select(f => f.FollowedId)
            .ToListAsync();

        // Add current user to the list to simplify query
        var authorIds = new List<long>(followedUserIds) { userId.Value };

        // 4. Query posts (own + followed users) and load into memory
        var allPosts = await _dbContext.Posts
            .Include(p => p.Author)
            .Where(p => authorIds.Contains(p.AuthorId))
            .ToListAsync();

        // 5. Apply cursor filter and sort in memory
        var posts = allPosts
            .Where(p => cursorTime == DateTimeOffset.MaxValue ||
                       p.CreatedAtUtc < cursorTime ||
                       (p.CreatedAtUtc == cursorTime && p.Id < cursorId))
            .OrderByDescending(p => p.CreatedAtUtc)
            .ThenByDescending(p => p.Id)
            .Take(PageSize + 1)
            .ToList();

        // 6. Calculate viewer context for each post
        var visiblePosts = posts.Take(PageSize).ToArray();
        var postSummaries = await PostSummaryFactory.CreateManyAsync(_dbContext, visiblePosts, userId.Value);

        // 7. Generate next cursor if more posts exist
        string? nextCursor = null;
        if (posts.Count > PageSize)
        {
            var lastPost = posts[PageSize - 1];
            nextCursor = EncodeCursor(lastPost.CreatedAtUtc, lastPost.Id);
        }

        return Results.Ok(new TimelineResponse(postSummaries, nextCursor));
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
