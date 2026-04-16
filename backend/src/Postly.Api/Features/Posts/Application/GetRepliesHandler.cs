using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Posts.Application;

public class GetRepliesHandler
{
    private const int PageSize = 20;

    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;

    public GetRepliesHandler(
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

        var exists = await _dbContext.Posts.AnyAsync(p => p.Id == postId);
        if (!exists)
        {
            return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("Post not found", _httpContext.TraceIdentifier));
        }

        var allReplies = await _dbContext.Posts
            .Include(p => p.Author)
            .Where(p => p.ReplyToPostId == postId)
            .ToListAsync();

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

        return Results.Ok(new ReplyPageResponse(replySummaries, nextCursor));
    }
}
