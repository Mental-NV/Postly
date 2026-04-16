using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Shared.Pagination;
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

        if (!OpaqueCursor.TryParse(cursor, out var parsedCursor))
        {
            return Results.Problem(ProblemDetailsFactory.CreateValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["cursor"] = ["Cursor must be a valid continuation token."]
                },
                _httpContext.TraceIdentifier));
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
            .Where(p => authorIds.Contains(p.AuthorId) && p.ReplyToPostId == null && p.DeletedAtUtc == null)
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
            userId.Value);

        return Results.Ok(new TimelineResponse(postSummaries, page.NextCursor));
    }
}
