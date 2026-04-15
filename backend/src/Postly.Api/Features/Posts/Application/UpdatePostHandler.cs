using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Shared.Validation;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Posts.Application;

public class UpdatePostHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;

    public UpdatePostHandler(
        AppDbContext dbContext,
        ICurrentViewerAccessor currentViewer,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentViewer = currentViewer;
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync(long postId, UpdatePostRequest request)
    {
        var userId = _currentViewer.GetCurrentUserId();
        if (userId == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        var errors = ValidationHelpers.ValidatePostBody(request.Body);
        if (errors.Count > 0)
        {
            return Results.Problem(ProblemDetailsFactory.CreateValidationProblem(errors, _httpContext.TraceIdentifier));
        }

        var post = await _dbContext.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null || post.DeletedAtUtc != null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("Post not found", _httpContext.TraceIdentifier));
        }

        if (post.AuthorId != userId.Value)
        {
            return Results.Problem(ProblemDetailsFactory.CreateForbiddenProblem("You can only edit your own posts", _httpContext.TraceIdentifier));
        }

        post.Body = request.Body.Trim();
        post.EditedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        var likeCount = await _dbContext.Likes.CountAsync(l => l.PostId == postId);
        var likedByViewer = await _dbContext.Likes.AnyAsync(l => l.PostId == postId && l.UserAccountId == userId.Value);
        var summary = PostSummaryFactory.Create(post, userId, likeCount, likedByViewer);

        return Results.Ok(new PostResponse(summary));
    }
}
