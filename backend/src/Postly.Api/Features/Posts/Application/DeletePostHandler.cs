using Postly.Api.Features.Shared.Errors;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Posts.Application;

public class DeletePostHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;

    public DeletePostHandler(
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

        // 2. Find post
        var post = await _dbContext.Posts.FindAsync(postId);

        if (post == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("Post not found", _httpContext.TraceIdentifier));
        }

        // 3. Check ownership
        if (post.AuthorId != userId.Value)
        {
            return Results.Problem(ProblemDetailsFactory.CreateForbiddenProblem("You can only delete your own posts", _httpContext.TraceIdentifier));
        }

        // 4. Delete post
        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync();

        return Results.NoContent();
    }
}
