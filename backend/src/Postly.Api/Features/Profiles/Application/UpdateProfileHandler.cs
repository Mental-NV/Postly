using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Profiles.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Profiles.Application;

public class UpdateProfileHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly GetProfileHandler _getProfileHandler;
    private readonly HttpContext _httpContext;

    public UpdateProfileHandler(
        AppDbContext dbContext,
        ICurrentViewerAccessor currentViewer,
        GetProfileHandler getProfileHandler,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentViewer = currentViewer;
        _getProfileHandler = getProfileHandler;
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync(UpdateProfileRequest request)
    {
        var userId = _currentViewer.GetCurrentUserId();
        if (userId == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Results.Problem(ProblemDetailsFactory.CreateValidationProblem(errors, _httpContext.TraceIdentifier));
        }

        var user = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(account => account.Id == userId.Value);

        if (user == null)
        {
            return Results.Problem(ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        user.DisplayName = request.DisplayName.Trim();
        user.Bio = NormalizeBio(request.Bio);

        await _dbContext.SaveChangesAsync();

        return await _getProfileHandler.HandleSelfAsync(null);
    }

    public static Dictionary<string, string[]> Validate(UpdateProfileRequest request)
    {
        Dictionary<string, string[]> errors = [];

        var trimmedDisplayName = request.DisplayName.Trim();
        if (trimmedDisplayName.Length is < 1 or > 50)
        {
            errors["displayName"] = ["Display name must be between 1 and 50 characters after trimming."];
        }

        var normalizedBio = NormalizeBio(request.Bio);
        if (normalizedBio != null && normalizedBio.Length > 160)
        {
            errors["bio"] = ["Bio must be 160 characters or fewer."];
        }

        return errors;
    }

    private static string? NormalizeBio(string? bio)
    {
        if (bio == null)
        {
            return null;
        }

        var trimmedBio = bio.Trim();
        return trimmedBio.Length == 0 ? null : trimmedBio;
    }
}
