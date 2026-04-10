using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Auth.Application;

public class SignoutHandler
{
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly AppDbContext _dbContext;
    private readonly HttpContext _httpContext;

    public SignoutHandler(
        ICurrentViewerAccessor currentViewer,
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _currentViewer = currentViewer;
        _dbContext = dbContext;
        _httpContext = httpContextAccessor.HttpContext!;
    }

    public async Task<IResult> HandleAsync()
    {
        var userId = _currentViewer.GetCurrentUserId();

        if (userId == null)
        {
            return Results.Unauthorized();
        }

        await SessionCookieAuthentication.RevokeSessionAsync(_dbContext, _httpContext);

        return Results.NoContent();
    }
}
