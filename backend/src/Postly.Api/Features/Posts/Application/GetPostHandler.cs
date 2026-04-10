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
        // 1. Get current user
        var userId = _currentViewer.GetCurrentUserId();
        if (userId == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        // 2. Find post with author
        var post = await _dbContext.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            return Results.NotFound(new { error = "Post not found" });
        }

        // 3. Calculate viewer context
        var likeCount = await _dbContext.Likes
            .CountAsync(l => l.PostId == postId);

        var likedByViewer = await _dbContext.Likes
            .AnyAsync(l => l.PostId == postId && l.UserAccountId == userId.Value);

        // 4. Build response
        var postSummary = new PostSummary(
            post.Id,
            post.Author.Username,
            post.Author.DisplayName,
            post.Body,
            post.CreatedAtUtc,
            post.EditedAtUtc != null,
            likeCount,
            likedByViewer,
            post.AuthorId == userId.Value,
            post.AuthorId == userId.Value
        );

        return Results.Ok(postSummary);
    }
}
