using System.Security.Claims;

namespace Postly.Api.Security;

public interface ICurrentViewerAccessor
{
    long? GetCurrentUserId();
    bool IsAuthenticated();
}

public class CurrentViewerAccessor : ICurrentViewerAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentViewerAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public long? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
