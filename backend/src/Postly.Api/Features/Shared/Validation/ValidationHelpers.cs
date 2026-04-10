using System.Text.RegularExpressions;

namespace Postly.Api.Features.Shared.Validation;

public static partial class ValidationHelpers
{
    [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
    private static partial Regex UsernameRegex();

    public static Dictionary<string, string[]> ValidateUsername(string username)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(username))
        {
            errors["username"] = ["Username is required."];
            return errors;
        }

        var trimmed = username.Trim();

        if (trimmed.Length < 3 || trimmed.Length > 20)
        {
            errors["username"] = ["Username must be between 3 and 20 characters."];
        }

        if (!UsernameRegex().IsMatch(trimmed))
        {
            errors["username"] = ["Username can only contain letters, digits, and underscores."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidatePassword(string password)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors["password"] = ["Password is required."];
            return errors;
        }

        if (password.Length < 8 || password.Length > 64)
        {
            errors["password"] = ["Password must be between 8 and 64 characters."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateDisplayName(string displayName)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            errors["displayName"] = ["Display name is required."];
            return errors;
        }

        var trimmed = displayName.Trim();

        if (trimmed.Length < 1 || trimmed.Length > 50)
        {
            errors["displayName"] = ["Display name must be between 1 and 50 characters."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateBio(string? bio)
    {
        var errors = new Dictionary<string, string[]>();

        if (bio != null && bio.Length > 160)
        {
            errors["bio"] = ["Bio cannot exceed 160 characters."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidatePostBody(string body)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(body))
        {
            errors["body"] = ["Post body is required."];
            return errors;
        }

        if (body.Length < 1 || body.Length > 280)
        {
            errors["body"] = ["Post body must be between 1 and 280 characters."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> MergeErrors(params Dictionary<string, string[]>[] errorDictionaries)
    {
        var merged = new Dictionary<string, string[]>();

        foreach (var dict in errorDictionaries)
        {
            foreach (var kvp in dict)
            {
                if (merged.ContainsKey(kvp.Key))
                {
                    merged[kvp.Key] = merged[kvp.Key].Concat(kvp.Value).ToArray();
                }
                else
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }
        }

        return merged;
    }
}
