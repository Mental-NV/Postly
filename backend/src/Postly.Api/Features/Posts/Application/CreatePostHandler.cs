using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Shared.Validation;
using Postly.Api.Persistence;
using Postly.Api.Persistence.Entities;
using Postly.Api.Security;

namespace Postly.Api.Features.Posts.Application;

public class CreatePostHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly HttpContext _httpContext;

    public CreatePostHandler(
        AppDbContext dbContext,
        ICurrentViewerAccessor currentViewer,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentViewer = currentViewer;
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync(CreatePostRequest request)
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

        // 3. Create post
        var post = new Post
        {
            AuthorId = userId.Value,
            Body = request.Body.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync();

        // 4. Load author for response
        await _dbContext.Entry(post).Reference(p => p.Author).LoadAsync();

        // 5. Return response
        var response = new PostResponse(
            post.Id,
            post.Author.Username,
            post.Author.DisplayName,
            post.Body,
            post.CreatedAtUtc,
            false,
            null
        );

        return Results.Created($"/api/posts/{post.Id}", response);
    }
}
