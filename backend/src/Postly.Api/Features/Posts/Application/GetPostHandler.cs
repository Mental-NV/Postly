using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Posts.Application;

public class GetPostHandler
{
    private const int PageSize = 20;

    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;

    public GetPostHandler(
        AppDbContext dbContext,
        ICurrentViewerAccessor currentViewer,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentViewer = currentViewer;
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync(long postId, string? cursor)
    {
        var userId = _currentViewer.GetCurrentUserId();

        var targetPost = await _dbContext.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (targetPost == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("Post not found", _httpContext.TraceIdentifier));
        }

        PostSummary? targetSummary = null;
        string targetState;

        if (targetPost.DeletedAtUtc != null)
        {
            targetState = "unavailable";
        }
        else
        {
            var likeCount = await _dbContext.Likes.CountAsync(l => l.PostId == postId);
            var likedByViewer = userId != null && await _dbContext.Likes
                .AnyAsync(l => l.PostId == postId && l.UserAccountId == userId.Value);
            targetSummary = PostSummaryFactory.Create(targetPost, userId, likeCount, likedByViewer);
            targetState = "available";
        }

        // Load all replies into memory, then sort (SQLite DateTimeOffset ORDER BY not supported)
        var allReplies = await _dbContext.Posts
            .Include(p => p.Author)
            .Where(p => p.ReplyToPostId == postId)
            .ToListAsync();

        // Parse cursor
        DateTimeOffset cursorTime = DateTimeOffset.MaxValue;
        long cursorId = long.MaxValue;
        if (!string.IsNullOrEmpty(cursor))
        {
            var parts = cursor.Split('_');
            if (parts.Length == 2 && DateTimeOffset.TryParse(parts[0], out var ct) && long.TryParse(parts[1], out var ci))
            {
                cursorTime = ct;
                cursorId = ci;
            }
        }

        var replies = allReplies
            .Where(p => cursorTime == DateTimeOffset.MaxValue ||
                        p.CreatedAtUtc < cursorTime ||
                        (p.CreatedAtUtc == cursorTime && p.Id < cursorId))
            .OrderByDescending(p => p.CreatedAtUtc)
            .ThenByDescending(p => p.Id)
            .Take(PageSize + 1)
            .ToList();

        string? nextCursor = null;
        if (replies.Count > PageSize)
        {
            replies = replies.Take(PageSize).ToList();
            var last = replies.Last();
            nextCursor = $"{last.CreatedAtUtc:O}_{last.Id}";
        }

        var replySummaries = await PostSummaryFactory.CreateManyAsync(_dbContext, replies, userId);

        return Results.Ok(new ConversationResponse(
            Target: new ConversationTarget(targetState, targetSummary),
            Replies: replySummaries,
            NextCursor: nextCursor
        ));
    }
}
