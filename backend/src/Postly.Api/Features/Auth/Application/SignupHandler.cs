using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Auth.Contracts;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Shared.Validation;
using Postly.Api.Persistence;
using Postly.Api.Persistence.Entities;
using Postly.Api.Security;

namespace Postly.Api.Features.Auth.Application;

public class SignupHandler
{
    private readonly AppDbContext _dbContext;
    private readonly PasswordHasher<UserAccount> _passwordHasher;
    private readonly HttpContext _httpContext;

    public SignupHandler(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _passwordHasher = new PasswordHasher<UserAccount>();
        _httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");
    }

    public async Task<IResult> HandleAsync(SignupRequest request)
    {
        // Validate all fields
        var errors = ValidationHelpers.MergeErrors(
            ValidationHelpers.ValidateUsername(request.Username ?? string.Empty),
            ValidationHelpers.ValidateDisplayName(request.DisplayName ?? string.Empty),
            ValidationHelpers.ValidateBio(request.Bio),
            ValidationHelpers.ValidatePassword(request.Password ?? string.Empty)
        );

        if (errors.Count > 0)
        {
            var problemDetails = ProblemDetailsFactory.CreateValidationProblem(
                errors,
                _httpContext.TraceIdentifier
            );
            return Results.Problem(problemDetails);
        }

        // Trim and normalize username
        var username = request.Username!.Trim();
        var normalizedUsername = username.ToUpperInvariant();

        // Check for duplicate username
        var existingUser = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername);

        if (existingUser != null)
        {
            var conflictProblem = ProblemDetailsFactory.CreateConflictProblem(
                "Username is already taken.",
                _httpContext.TraceIdentifier
            );
            return Results.Problem(conflictProblem);
        }

        // Create user account
        var userAccount = new UserAccount
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            DisplayName = request.DisplayName!.Trim(),
            Bio = request.Bio,
            PasswordHash = string.Empty,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Hash password
        userAccount.PasswordHash = _passwordHasher.HashPassword(userAccount, request.Password!);

        // Save to database
        _dbContext.UserAccounts.Add(userAccount);
        await _dbContext.SaveChangesAsync();

        // Create session
        await SessionCookieAuthentication.CreateSessionAsync(
            _dbContext,
            userAccount.Id,
            _httpContext
        );

        // Return session response
        var response = new SessionResponse(
            userAccount.Id,
            userAccount.Username,
            userAccount.DisplayName
        );

        return Results.Created($"/api/auth/session", response);
    }
}
