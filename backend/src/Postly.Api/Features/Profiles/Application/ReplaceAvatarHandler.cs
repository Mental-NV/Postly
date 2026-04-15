using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Profiles.Application;

public class ReplaceAvatarHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly GetProfileHandler _getProfileHandler;
    private readonly ProfileAvatarProcessor _avatarProcessor;
    private readonly HttpContext _httpContext;

    public ReplaceAvatarHandler(
        AppDbContext dbContext,
        ICurrentViewerAccessor currentViewer,
        GetProfileHandler getProfileHandler,
        ProfileAvatarProcessor avatarProcessor,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentViewer = currentViewer;
        _getProfileHandler = getProfileHandler;
        _avatarProcessor = avatarProcessor;
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync(IFormFile? avatar)
    {
        var userId = _currentViewer.GetCurrentUserId();
        if (userId == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        if (avatar == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["avatar"] = ["Avatar upload is required."]
                },
                _httpContext.TraceIdentifier));
        }

        var user = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(account => account.Id == userId.Value);

        if (user == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        await using var stream = avatar.OpenReadStream();
        var processedAvatar = await _avatarProcessor.ProcessAsync(stream, _httpContext.RequestAborted);
        if (!processedAvatar.IsSuccess || processedAvatar.AvatarBytes == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["avatar"] = [processedAvatar.ErrorMessage ?? "Avatar upload could not be processed."]
                },
                _httpContext.TraceIdentifier));
        }

        user.AvatarBytes = processedAvatar.AvatarBytes;
        user.AvatarContentType = ProfileIdentityProjection.AvatarContentType;
        user.AvatarUpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return await _getProfileHandler.HandleSelfAsync(null);
    }
}
