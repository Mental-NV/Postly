namespace Postly.Api.Features.Auth.Contracts;

public record SigninRequest(
    string Username,
    string Password
);
