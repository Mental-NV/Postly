using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Shared.Validation;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Persistence.Entities;
using Postly.Api.Security;

namespace Postly.Api.Features.Posts.Application;

public class CreateReplyHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;

    public CreateReplyHandler(
        AppDbContext dbContext,
        ICurrentViewerAccessor currentViewer,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentViewer = currentViewer;
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync(long postId, CreateReplyRequest request)
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

        // Verify target post exists and is not deleted
        var targetPost = await _dbContext.Posts.FindAsync(postId);
        if (targetPost == null || targetPost.DeletedAtUtc != null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("Target post not found or unavailable", _httpContext.TraceIdentifier));
        }

        var author = await _dbContext.UserAccounts.FindAsync(userId.Value);
        if (author == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("User not found", _httpContext.TraceIdentifier));
        }

        var reply = new Post
        {
            AuthorId = userId.Value,
            Body = request.Body.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ReplyToPostId = postId
        };

        _dbContext.Posts.Add(reply);

        // Create notification if not replying to own post
        if (targetPost.AuthorId != userId.Value)
        {
            _dbContext.Notifications.Add(new Notification
            {
                RecipientUserId = targetPost.AuthorId,
                ActorUserId = userId.Value,
                Kind = "reply",
                PostId = postId,
                ReplyPostId = reply.Id,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();

        var summary = PostSummaryFactory.Create(reply, userId, 0, false);
        return Results.Created($"/api/posts/{reply.Id}", new PostResponse(summary));
    }
}
