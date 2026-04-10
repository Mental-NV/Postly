using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Auth.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Shared.Validation;
using Postly.Api.Persistence;
using Postly.Api.Persistence.Entities;
using Postly.Api.Security;

namespace Postly.Api.Features.Auth.Application;

public class SigninHandler
{
    private readonly AppDbContext _dbContext;
    private readonly PasswordHasher<UserAccount> _passwordHasher;
    private readonly HttpContext _httpContext;

    public SigninHandler(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _passwordHasher = new PasswordHasher<UserAccount>();
        _httpContext = httpContextAccessor.HttpContext!;
    }

    public async Task<IResult> HandleAsync(SigninRequest request)
    {
        // Validate input
        var errors = ValidationHelpers.MergeErrors(
            ValidationHelpers.ValidateUsername(request.Username ?? string.Empty),
            ValidationHelpers.ValidatePassword(request.Password ?? string.Empty)
        );

        if (errors.Count > 0)
        {
            return Results.Problem(
                ProblemDetailsFactory.CreateValidationProblem(errors, _httpContext.TraceIdentifier));
        }

        // Look up user by normalized username
        var normalizedUsername = request.Username!.ToUpperInvariant();
        var user = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername);

        // Verify password
        if (user == null)
        {
            // User not found - return generic error (don't reveal username doesn't exist)
            return Results.Problem(
                ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password!);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            // Wrong password - return generic error (don't reveal password is wrong)
            return Results.Problem(
                ProblemDetailsFactory.CreateUnauthorizedProblem(_httpContext.TraceIdentifier));
        }

        // Revoke any existing active sessions for this user
        var existingSessions = await _dbContext.Sessions
            .Where(s => s.UserAccountId == user.Id && s.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var session in existingSessions)
        {
            session.RevokedAtUtc = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        // Create new session
        await SessionCookieAuthentication.CreateSessionAsync(_dbContext, user.Id, _httpContext);

        // Return session response
        var response = new SessionResponse(
            UserId: user.Id,
            Username: user.Username,
            DisplayName: user.DisplayName
        );

        return Results.Ok(response);
    }
}
