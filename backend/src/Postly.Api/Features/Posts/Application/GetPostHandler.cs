using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Posts.Application;

public class GetPostHandler
{
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

    public async Task<IResult> HandleAsync(long postId)
    {
        var userId = _currentViewer.GetCurrentUserId();

        var post = await _dbContext.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("Post not found", _httpContext.TraceIdentifier));
        }

        var likeCount = await _dbContext.Likes
            .CountAsync(l => l.PostId == postId);

        var likedByViewer = userId != null && await _dbContext.Likes
            .AnyAsync(l => l.PostId == postId && l.UserAccountId == userId.Value);

        var postSummary = PostSummaryFactory.Create(post, userId, likeCount, likedByViewer);

        return Results.Ok(postSummary);
    }
}
