using FluentAssertions;
using Postly.Api.Features.Shared.Validation;
using Xunit;

namespace Postly.Api.UnitTests.Features.Shared.Validation;

public class ValidationHelpersTests
{
    #region ValidateUsername Tests

    [Theory]
    [InlineData("valid_user123")]
    [InlineData("abc")] // exactly 3 chars (min boundary)
    [InlineData("aaaaaaaaaaaaaaaaaaaa")] // exactly 20 chars (max boundary)
    [InlineData("  validuser  ")] // leading/trailing spaces (should be trimmed)
    public void ValidateUsername_ValidInputs_ReturnsEmptyErrors(string username)
    {
        var errors = ValidationHelpers.ValidateUsername(username);
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Username is required.")]
    [InlineData("", "Username is required.")]
    [InlineData("   ", "Username is required.")]
    public void ValidateUsername_RequiredValidation_ReturnsError(string? username, string expectedError)
    {
        var errors = ValidationHelpers.ValidateUsername(username!);
        errors.Should().ContainKey("username");
        errors["username"].Should().Contain(expectedError);
    }

    [Theory]
    [InlineData("ab")] // too short
    [InlineData("aaaaaaaaaaaaaaaaaaaaa")] // 21 chars, too long
    public void ValidateUsername_LengthValidation_ReturnsError(string username)
    {
        var errors = ValidationHelpers.ValidateUsername(username);
        errors.Should().ContainKey("username");
        errors["username"].Should().Contain("Username must be between 3 and 20 characters.");
    }

    [Theory]
    [InlineData("user@name!")]
    [InlineData("user name")]
    public void ValidateUsername_FormatValidation_ReturnsError(string username)
    {
        var errors = ValidationHelpers.ValidateUsername(username);
        errors.Should().ContainKey("username");
        errors["username"].Should().Contain("Username can only contain letters, digits, and underscores.");
    }

    [Theory]
    [InlineData("me")]
    [InlineData("ME")]
    [InlineData(" ME ")]
    public void ValidateUsername_ReservedUsername_ReturnsError(string username)
    {
        var errors = ValidationHelpers.ValidateUsername(username);
        errors.Should().ContainKey("username");
        errors["username"].Should().Contain("Username \"me\" is reserved.");
    }

    #endregion

    #region ValidatePassword Tests

    [Theory]
    [InlineData("password123")]
    [InlineData("pass1234")] // exactly 8 chars (min boundary)
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // exactly 64 chars (max boundary)
    public void ValidatePassword_ValidInputs_ReturnsEmptyErrors(string password)
    {
        var errors = ValidationHelpers.ValidatePassword(password);
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Password is required.")]
    [InlineData("", "Password is required.")]
    [InlineData("   ", "Password is required.")]
    public void ValidatePassword_RequiredValidation_ReturnsError(string? password, string expectedError)
    {
        var errors = ValidationHelpers.ValidatePassword(password!);
        errors.Should().ContainKey("password");
        errors["password"].Should().Contain(expectedError);
    }

    [Theory]
    [InlineData("pass123")] // 7 chars, too short
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 65 chars, too long
    public void ValidatePassword_LengthValidation_ReturnsError(string password)
    {
        var errors = ValidationHelpers.ValidatePassword(password);
        errors.Should().ContainKey("password");
        errors["password"].Should().Contain("Password must be between 8 and 64 characters.");
    }

    #endregion

    #region ValidateDisplayName Tests

    [Theory]
    [InlineData("John Doe")]
    [InlineData("A")] // exactly 1 char (min boundary)
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // exactly 50 chars (max boundary)
    [InlineData("  John Doe  ")] // leading/trailing spaces (should be trimmed)
    public void ValidateDisplayName_ValidInputs_ReturnsEmptyErrors(string displayName)
    {
        var errors = ValidationHelpers.ValidateDisplayName(displayName);
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Display name is required.")]
    [InlineData("", "Display name is required.")]
    [InlineData("   ", "Display name is required.")]
    public void ValidateDisplayName_RequiredValidation_ReturnsError(string? displayName, string expectedError)
    {
        var errors = ValidationHelpers.ValidateDisplayName(displayName!);
        errors.Should().ContainKey("displayName");
        errors["displayName"].Should().Contain(expectedError);
    }

    [Fact]
    public void ValidateDisplayName_TooLong_ReturnsError()
    {
        var displayName = new string('a', 51);
        var errors = ValidationHelpers.ValidateDisplayName(displayName);
        errors.Should().ContainKey("displayName");
        errors["displayName"].Should().Contain("Display name must be between 1 and 50 characters.");
    }

    #endregion

    #region ValidateBio Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Valid bio text")]
    public void ValidateBio_ValidInputs_ReturnsEmptyErrors(string? bio)
    {
        var errors = ValidationHelpers.ValidateBio(bio);
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateBio_ExactlyMaxLength_ReturnsEmptyErrors()
    {
        var bio = new string('a', 160);
        var errors = ValidationHelpers.ValidateBio(bio);
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateBio_TooLong_ReturnsError()
    {
        var bio = new string('a', 161);
        var errors = ValidationHelpers.ValidateBio(bio);
        errors.Should().ContainKey("bio");
        errors["bio"].Should().Contain("Bio cannot exceed 160 characters.");
    }

    #endregion

    #region ValidatePostBody Tests

    [Theory]
    [InlineData("This is a valid post body.")]
    [InlineData("a")] // exactly 1 char (min boundary)
    public void ValidatePostBody_ValidInputs_ReturnsEmptyErrors(string body)
    {
        var errors = ValidationHelpers.ValidatePostBody(body);
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePostBody_ExactlyMaxLength_ReturnsEmptyErrors()
    {
        var body = new string('a', 280);
        var errors = ValidationHelpers.ValidatePostBody(body);
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Post body is required.")]
    [InlineData("", "Post body is required.")]
    [InlineData("   ", "Post body is required.")]
    public void ValidatePostBody_RequiredValidation_ReturnsError(string? body, string expectedError)
    {
        var errors = ValidationHelpers.ValidatePostBody(body!);
        errors.Should().ContainKey("body");
        errors["body"].Should().Contain(expectedError);
    }

    [Fact]
    public void ValidatePostBody_TooLong_ReturnsError()
    {
        var body = new string('a', 281);
        var errors = ValidationHelpers.ValidatePostBody(body);
        errors.Should().ContainKey("body");
        errors["body"].Should().Contain("Post body must be between 1 and 280 characters.");
    }

    #endregion

    #region MergeErrors Tests

    [Fact]
    public void MergeErrors_MultipleDictionariesWithDifferentKeys_MergesCorrectly()
    {
        var dict1 = new Dictionary<string, string[]>
        {
            ["username"] = ["Username is required."]
        };
        var dict2 = new Dictionary<string, string[]>
        {
            ["password"] = ["Password is required."]
        };

        var result = ValidationHelpers.MergeErrors(dict1, dict2);

        result.Should().HaveCount(2);
        result.Should().ContainKey("username");
        result.Should().ContainKey("password");
        result["username"].Should().Contain("Username is required.");
        result["password"].Should().Contain("Password is required.");
    }

    [Fact]
    public void MergeErrors_MultipleDictionariesWithSameKey_ConcatenatesErrorArrays()
    {
        var dict1 = new Dictionary<string, string[]>
        {
            ["username"] = ["Username is required."]
        };
        var dict2 = new Dictionary<string, string[]>
        {
            ["username"] = ["Username must be between 3 and 20 characters."]
        };

        var result = ValidationHelpers.MergeErrors(dict1, dict2);

        result.Should().ContainKey("username");
        result["username"].Should().HaveCount(2);
        result["username"].Should().Contain("Username is required.");
        result["username"].Should().Contain("Username must be between 3 and 20 characters.");
    }

    #endregion
}
