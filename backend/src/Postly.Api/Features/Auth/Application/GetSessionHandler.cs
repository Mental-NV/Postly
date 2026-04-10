using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Auth.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Security;

namespace Postly.Api.Features.Auth.Application;

public class GetSessionHandler
{
    private readonly ICurrentViewerAccessor _currentViewer;
    private readonly AppDbContext _dbContext;

    public GetSessionHandler(
        ICurrentViewerAccessor currentViewer,
        AppDbContext dbContext)
    {
        _currentViewer = currentViewer;
        _dbContext = dbContext;
    }

    public async Task<IResult> HandleAsync()
    {
        var userId = _currentViewer.GetCurrentUserId();

        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var user = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (user == null)
        {
            return Results.Unauthorized();
        }

        var response = new SessionResponse(
            UserId: user.Id,
            Username: user.Username,
            DisplayName: user.DisplayName
        );

        return Results.Ok(response);
    }
}
