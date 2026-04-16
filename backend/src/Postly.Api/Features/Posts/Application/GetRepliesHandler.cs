using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Shared.Pagination;
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

        if (!OpaqueCursor.TryParse(cursor, out var parsedCursor))
        {
            return Results.Problem(ProblemDetailsFactory.CreateValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["cursor"] = ["Cursor must be a valid continuation token."]
                },
                _httpContext.TraceIdentifier));
        }

        var page = OpaqueCursorPagination.Paginate(
            allReplies,
            parsedCursor,
            PageSize,
            reply => reply.CreatedAtUtc,
            reply => reply.Id);

        var replySummaries = await PostSummaryFactory.CreateManyAsync(
            _dbContext,
            page.Items,
            userId);

        return Results.Ok(new ReplyPageResponse(replySummaries, page.NextCursor));
    }
}
