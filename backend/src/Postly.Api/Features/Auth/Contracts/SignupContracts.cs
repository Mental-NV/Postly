namespace Postly.Api.Features.Auth.Contracts;

public record SignupRequest(
    string Username,
    string DisplayName,
    string? Bio,
    string Password
);

public record SessionResponse(
    long UserId,
    string Username,
    string DisplayName
);
