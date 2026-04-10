namespace Postly.Api.Features.Shared.Errors;

public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string InternalError = "INTERNAL_ERROR";
}

public class ProblemDetailsResponse
{
    public required string Type { get; set; }
    public required string Title { get; set; }
    public int Status { get; set; }
    public string? Detail { get; set; }
    public string? TraceId { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}
