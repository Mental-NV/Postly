using Microsoft.AspNetCore.Mvc;

namespace Postly.Api.Features.Shared.Errors;

public static class ProblemDetailsFactory
{
    public static ProblemDetails CreateValidationProblem(
        Dictionary<string, string[]> errors,
        string traceId)
    {
        return new ProblemDetails
        {
            Type = ErrorCodes.ValidationFailed,
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Extensions = new Dictionary<string, object?>
            {
                ["traceId"] = traceId,
                ["errors"] = errors
            }
        };
    }

    public static ProblemDetails CreateUnauthorizedProblem(string traceId)
    {
        return new ProblemDetails
        {
            Type = ErrorCodes.Unauthorized,
            Title = "Authentication is required.",
            Status = StatusCodes.Status401Unauthorized,
            Extensions = new Dictionary<string, object?>
            {
                ["traceId"] = traceId
            }
        };
    }

    public static ProblemDetails CreateForbiddenProblem(string detail, string traceId)
    {
        return new ProblemDetails
        {
            Type = ErrorCodes.Forbidden,
            Title = "Access to this resource is forbidden.",
            Status = StatusCodes.Status403Forbidden,
            Detail = detail,
            Extensions = new Dictionary<string, object?>
            {
                ["traceId"] = traceId
            }
        };
    }

    public static ProblemDetails CreateNotFoundProblem(string detail, string traceId)
    {
        return new ProblemDetails
        {
            Type = ErrorCodes.NotFound,
            Title = "The requested resource was not found.",
            Status = StatusCodes.Status404NotFound,
            Detail = detail,
            Extensions = new Dictionary<string, object?>
            {
                ["traceId"] = traceId
            }
        };
    }

    public static ProblemDetails CreateConflictProblem(string detail, string traceId)
    {
        return new ProblemDetails
        {
            Type = ErrorCodes.Conflict,
            Title = "A conflict occurred.",
            Status = StatusCodes.Status409Conflict,
            Detail = detail,
            Extensions = new Dictionary<string, object?>
            {
                ["traceId"] = traceId
            }
        };
    }
}
