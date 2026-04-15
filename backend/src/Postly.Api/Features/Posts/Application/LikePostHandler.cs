using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Persistence;
using Postly.Api.Persistence.Entities;
using Postly.Api.Security;

namespace Postly.Api.Features.Posts.Application;

public class LikePostHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;

    public LikePostHandler(
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
        var viewerId = _currentViewer.GetCurrentUserId();
        if (viewerId == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        var post = await _dbContext.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == postId);
            
        if (post == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateNotFoundProblem("Post not found", _httpContext.TraceIdentifier));
        }

        var existingLike = await _dbContext.Likes.FindAsync(viewerId.Value, postId);
        if (existingLike == null)
        {
            _dbContext.Likes.Add(new Like
            {
                UserAccountId = viewerId.Value,
                PostId = postId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            // Create notification for the post author (only if not self-action)
            if (viewerId.Value != post.AuthorId)
            {
                var notification = new Notification
                {
                    RecipientUserId = post.AuthorId,
                    ActorUserId = viewerId.Value,
                    Kind = "like",
                    PostId = postId,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };
                _dbContext.Notifications.Add(notification);
            }

            await _dbContext.SaveChangesAsync();
        }

        var likeCount = await _dbContext.Likes.CountAsync(like => like.PostId == postId);
        return Results.Ok(new PostInteractionState(postId, likeCount, true));
    }
}
