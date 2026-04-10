using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Shared.Validation;
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
        // 1. Get current user
        var userId = _currentViewer.GetCurrentUserId();
        if (userId == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        // 2. Validate body
        var errors = ValidationHelpers.ValidatePostBody(request.Body);
        if (errors.Count > 0)
        {
            return Results.Problem(ProblemDetailsFactory.CreateValidationProblem(errors, _httpContext.TraceIdentifier));
        }

        // 3. Find post
        var post = await _dbContext.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("Post not found", _httpContext.TraceIdentifier));
        }

        // 4. Check ownership
        if (post.AuthorId != userId.Value)
        {
            return Results.Problem(ProblemDetailsFactory.CreateForbiddenProblem("You can only edit your own posts", _httpContext.TraceIdentifier));
        }

        // 5. Update post
        post.Body = request.Body.Trim();
        post.EditedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        // 6. Return response
        var response = new PostResponse(
            post.Id,
            post.Author.Username,
            post.Author.DisplayName,
            post.Body,
            post.CreatedAtUtc,
            true,
            post.EditedAtUtc
        );

        return Results.Ok(response);
    }
}
