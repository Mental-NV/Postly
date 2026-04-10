using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Postly.Api.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace Postly.Api.Security;

public static class SessionCookieAuthentication
{
    public const string AuthenticationScheme = "PostlySession";
    private const string SessionTokenClaim = "SessionToken";

    public static void AddSessionCookieAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(AuthenticationScheme)
            .AddCookie(AuthenticationScheme, options =>
            {
                options.Cookie.Name = "postly_session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = false;
                options.Events.OnValidatePrincipal = ValidateSessionAsync;

                // Return 401 for API requests instead of redirecting
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization();
    }

    private static async Task ValidateSessionAsync(CookieValidatePrincipalContext context)
    {
        var sessionToken = context.Principal?.FindFirst(SessionTokenClaim)?.Value;
        if (string.IsNullOrEmpty(sessionToken))
        {
            context.RejectPrincipal();
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var tokenHash = HashToken(sessionToken);

        var session = await dbContext.Sessions
            .Include(s => s.UserAccount)
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash);

        if (session == null ||
            session.RevokedAtUtc != null ||
            session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            context.RejectPrincipal();
            return;
        }

        session.LastSeenAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    public static async Task<string> CreateSessionAsync(
        AppDbContext dbContext,
        long userAccountId,
        HttpContext httpContext)
    {
        var sessionToken = GenerateSecureToken();
        var tokenHash = HashToken(sessionToken);

        var session = new Persistence.Entities.Session
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            TokenHash = tokenHash,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            LastSeenAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userAccountId.ToString()),
            new Claim(SessionTokenClaim, sessionToken)
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(AuthenticationScheme, principal);

        return sessionToken;
    }

    public static async Task RevokeSessionAsync(
        AppDbContext dbContext,
        HttpContext httpContext)
    {
        var sessionToken = httpContext.User.FindFirst(SessionTokenClaim)?.Value;
        if (!string.IsNullOrEmpty(sessionToken))
        {
            var tokenHash = HashToken(sessionToken);
            var session = await dbContext.Sessions
                .FirstOrDefaultAsync(s => s.TokenHash == tokenHash);

            if (session != null)
            {
                session.RevokedAtUtc = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }

        await httpContext.SignOutAsync(AuthenticationScheme);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
