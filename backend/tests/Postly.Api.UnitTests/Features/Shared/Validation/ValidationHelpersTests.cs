using FluentAssertions;
using Postly.Api.Features.Shared.Validation;
using Xunit;

namespace Postly.Api.UnitTests.Features.Shared.Validation;

public class ValidationHelpersTests
{
    #region ValidateUsername Tests

    [Fact]
    public void ValidateUsername_ValidUsername_ReturnsEmptyErrors()
    {
        // Arrange
        var username = "valid_user123";

        // Act
        var errors = ValidationHelpers.ValidateUsername(username);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateUsername_NullUsername_ReturnsRequiredError()
    {
        // Arrange
        string? username = null;

        // Act
        var errors = ValidationHelpers.ValidateUsername(username!);

        // Assert
        errors.Should().ContainKey("username");
        errors["username"].Should().Contain("Username is required.");
    }

    [Fact]
    public void ValidateUsername_EmptyString_ReturnsRequiredError()
    {
        // Arrange
        var username = "";

        // Act
        var errors = ValidationHelpers.ValidateUsername(username);

        // Assert
        errors.Should().ContainKey("username");
        errors["username"].Should().Contain("Username is required.");
    }

    [Fact]
    public void ValidateUsername_WhitespaceOnly_ReturnsRequiredError()
    {
        // Arrange
        var username = "   ";

        // Act
        var errors = ValidationHelpers.ValidateUsername(username);

        // Assert
        errors.Should().ContainKey("username");
        errors["username"].Should().Contain("Username is required.");
    }

    [Fact]
    public void ValidateUsername_TooShort_ReturnsLengthError()
    {
        // Arrange
        var username = "ab";

        // Act
        var errors = ValidationHelpers.ValidateUsername(username);

        // Assert
        errors.Should().ContainKey("username");
        errors["username"].Should().Contain("Username must be between 3 and 20 characters.");
    }

    [Fact]
    public void ValidateUsername_TooLong_ReturnsLengthError()
    {
        // Arrange
        var username = "a".PadRight(21, 'a');

        // Act
        var errors = ValidationHelpers.ValidateUsername(username);

        // Assert
        errors.Should().ContainKey("username");
        errors["username"].Should().Contain("Username must be between 3 and 20 characters.");
    }

    [Fact]
    public void ValidateUsername_SpecialCharacters_ReturnsFormatError()
    {
        // Arrange
        var username = "user@name!";

        // Act
        var errors = ValidationHelpers.ValidateUsername(username);

        // Assert
        errors.Should().ContainKey("username");
        errors["username"].Should().Contain("Username can only contain letters, digits, and underscores.");
    }

    [Fact]
    public void ValidateUsername_WithSpaces_ReturnsFormatError()
    {
        // Arrange
        var username = "user name";

        // Act
        var errors = ValidationHelpers.ValidateUsername(username);

        // Assert
        errors.Should().ContainKey("username");
        errors["username"].Should().Contain("Username can only contain letters, digits, and underscores.");
    }

    [Fact]
    public void ValidateUsername_WithLeadingTrailingSpaces_ReturnsEmptyErrors()
    {
        // Arrange
        var username = "  validuser  ";

        // Act
        var errors = ValidationHelpers.ValidateUsername(username);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateUsername_ExactlyThreeChars_ReturnsEmptyErrors()
    {
        // Arrange
        var username = "abc";

        // Act
        var errors = ValidationHelpers.ValidateUsername(username);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateUsername_ExactlyTwentyChars_ReturnsEmptyErrors()
    {
        // Arrange
        var username = "a".PadRight(20, 'a');

        // Act
        var errors = ValidationHelpers.ValidateUsername(username);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region ValidatePassword Tests

    [Fact]
    public void ValidatePassword_ValidPassword_ReturnsEmptyErrors()
    {
        // Arrange
        var password = "password123";

        // Act
        var errors = ValidationHelpers.ValidatePassword(password);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePassword_NullPassword_ReturnsRequiredError()
    {
        // Arrange
        string? password = null;

        // Act
        var errors = ValidationHelpers.ValidatePassword(password!);

        // Assert
        errors.Should().ContainKey("password");
        errors["password"].Should().Contain("Password is required.");
    }

    [Fact]
    public void ValidatePassword_EmptyString_ReturnsRequiredError()
    {
        // Arrange
        var password = "";

        // Act
        var errors = ValidationHelpers.ValidatePassword(password);

        // Assert
        errors.Should().ContainKey("password");
        errors["password"].Should().Contain("Password is required.");
    }

    [Fact]
    public void ValidatePassword_WhitespaceOnly_ReturnsRequiredError()
    {
        // Arrange
        var password = "   ";

        // Act
        var errors = ValidationHelpers.ValidatePassword(password);

        // Assert
        errors.Should().ContainKey("password");
        errors["password"].Should().Contain("Password is required.");
    }

    [Fact]
    public void ValidatePassword_TooShort_ReturnsLengthError()
    {
        // Arrange
        var password = "pass123";

        // Act
        var errors = ValidationHelpers.ValidatePassword(password);

        // Assert
        errors.Should().ContainKey("password");
        errors["password"].Should().Contain("Password must be between 8 and 64 characters.");
    }

    [Fact]
    public void ValidatePassword_TooLong_ReturnsLengthError()
    {
        // Arrange
        var password = "a".PadRight(65, 'a');

        // Act
        var errors = ValidationHelpers.ValidatePassword(password);

        // Assert
        errors.Should().ContainKey("password");
        errors["password"].Should().Contain("Password must be between 8 and 64 characters.");
    }

    [Fact]
    public void ValidatePassword_ExactlyEightChars_ReturnsEmptyErrors()
    {
        // Arrange
        var password = "pass1234";

        // Act
        var errors = ValidationHelpers.ValidatePassword(password);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePassword_ExactlySixtyFourChars_ReturnsEmptyErrors()
    {
        // Arrange
        var password = "a".PadRight(64, 'a');

        // Act
        var errors = ValidationHelpers.ValidatePassword(password);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region ValidateDisplayName Tests

    [Fact]
    public void ValidateDisplayName_ValidDisplayName_ReturnsEmptyErrors()
    {
        // Arrange
        var displayName = "John Doe";

        // Act
        var errors = ValidationHelpers.ValidateDisplayName(displayName);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateDisplayName_NullDisplayName_ReturnsRequiredError()
    {
        // Arrange
        string? displayName = null;

        // Act
        var errors = ValidationHelpers.ValidateDisplayName(displayName!);

        // Assert
        errors.Should().ContainKey("displayName");
        errors["displayName"].Should().Contain("Display name is required.");
    }

    [Fact]
    public void ValidateDisplayName_EmptyString_ReturnsRequiredError()
    {
        // Arrange
        var displayName = "";

        // Act
        var errors = ValidationHelpers.ValidateDisplayName(displayName);

        // Assert
        errors.Should().ContainKey("displayName");
        errors["displayName"].Should().Contain("Display name is required.");
    }

    [Fact]
    public void ValidateDisplayName_WhitespaceOnly_ReturnsRequiredError()
    {
        // Arrange
        var displayName = "   ";

        // Act
        var errors = ValidationHelpers.ValidateDisplayName(displayName);

        // Assert
        errors.Should().ContainKey("displayName");
        errors["displayName"].Should().Contain("Display name is required.");
    }

    [Fact]
    public void ValidateDisplayName_TooLong_ReturnsLengthError()
    {
        // Arrange
        var displayName = "a".PadRight(51, 'a');

        // Act
        var errors = ValidationHelpers.ValidateDisplayName(displayName);

        // Assert
        errors.Should().ContainKey("displayName");
        errors["displayName"].Should().Contain("Display name must be between 1 and 50 characters.");
    }

    [Fact]
    public void ValidateDisplayName_WithLeadingTrailingSpaces_ReturnsEmptyErrors()
    {
        // Arrange
        var displayName = "  John Doe  ";

        // Act
        var errors = ValidationHelpers.ValidateDisplayName(displayName);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateDisplayName_ExactlyOneChar_ReturnsEmptyErrors()
    {
        // Arrange
        var displayName = "A";

        // Act
        var errors = ValidationHelpers.ValidateDisplayName(displayName);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateDisplayName_ExactlyFiftyChars_ReturnsEmptyErrors()
    {
        // Arrange
        var displayName = "a".PadRight(50, 'a');

        // Act
        var errors = ValidationHelpers.ValidateDisplayName(displayName);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region ValidateBio Tests

    [Fact]
    public void ValidateBio_NullBio_ReturnsEmptyErrors()
    {
        // Arrange
        string? bio = null;

        // Act
        var errors = ValidationHelpers.ValidateBio(bio);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateBio_EmptyString_ReturnsEmptyErrors()
    {
        // Arrange
        var bio = "";

        // Act
        var errors = ValidationHelpers.ValidateBio(bio);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateBio_ExactlyOneHundredSixtyChars_ReturnsEmptyErrors()
    {
        // Arrange
        var bio = "a".PadRight(160, 'a');

        // Act
        var errors = ValidationHelpers.ValidateBio(bio);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateBio_TooLong_ReturnsLengthError()
    {
        // Arrange
        var bio = "a".PadRight(161, 'a');

        // Act
        var errors = ValidationHelpers.ValidateBio(bio);

        // Assert
        errors.Should().ContainKey("bio");
        errors["bio"].Should().Contain("Bio cannot exceed 160 characters.");
    }

    [Fact]
    public void ValidateBio_WhitespaceOnly_ReturnsEmptyErrors()
    {
        // Arrange
        var bio = "   ";

        // Act
        var errors = ValidationHelpers.ValidateBio(bio);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region ValidatePostBody Tests

    [Fact]
    public void ValidatePostBody_ValidBody_ReturnsEmptyErrors()
    {
        // Arrange
        var body = "This is a valid post body.";

        // Act
        var errors = ValidationHelpers.ValidatePostBody(body);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePostBody_NullBody_ReturnsRequiredError()
    {
        // Arrange
        string? body = null;

        // Act
        var errors = ValidationHelpers.ValidatePostBody(body!);

        // Assert
        errors.Should().ContainKey("body");
        errors["body"].Should().Contain("Post body is required.");
    }

    [Fact]
    public void ValidatePostBody_EmptyString_ReturnsRequiredError()
    {
        // Arrange
        var body = "";

        // Act
        var errors = ValidationHelpers.ValidatePostBody(body);

        // Assert
        errors.Should().ContainKey("body");
        errors["body"].Should().Contain("Post body is required.");
    }

    [Fact]
    public void ValidatePostBody_WhitespaceOnly_ReturnsRequiredError()
    {
        // Arrange
        var body = "   ";

        // Act
        var errors = ValidationHelpers.ValidatePostBody(body);

        // Assert
        errors.Should().ContainKey("body");
        errors["body"].Should().Contain("Post body is required.");
    }

    [Fact]
    public void ValidatePostBody_TooLong_ReturnsLengthError()
    {
        // Arrange
        var body = "a".PadRight(281, 'a');

        // Act
        var errors = ValidationHelpers.ValidatePostBody(body);

        // Assert
        errors.Should().ContainKey("body");
        errors["body"].Should().Contain("Post body must be between 1 and 280 characters.");
    }

    [Fact]
    public void ValidatePostBody_ExactlyOneChar_ReturnsEmptyErrors()
    {
        // Arrange
        var body = "a";

        // Act
        var errors = ValidationHelpers.ValidatePostBody(body);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePostBody_ExactlyTwoHundredEightyChars_ReturnsEmptyErrors()
    {
        // Arrange
        var body = "a".PadRight(280, 'a');

        // Act
        var errors = ValidationHelpers.ValidatePostBody(body);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region MergeErrors Tests

    [Fact]
    public void MergeErrors_EmptyDictionaries_ReturnsEmptyResult()
    {
        // Arrange
        var dict1 = new Dictionary<string, string[]>();
        var dict2 = new Dictionary<string, string[]>();

        // Act
        var result = ValidationHelpers.MergeErrors(dict1, dict2);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void MergeErrors_SingleDictionary_ReturnsSameDictionary()
    {
        // Arrange
        var dict = new Dictionary<string, string[]>
        {
            ["username"] = ["Username is required."]
        };

        // Act
        var result = ValidationHelpers.MergeErrors(dict);

        // Assert
        result.Should().ContainKey("username");
        result["username"].Should().Contain("Username is required.");
    }

    [Fact]
    public void MergeErrors_MultipleDictionariesWithDifferentKeys_MergesCorrectly()
    {
        // Arrange
        var dict1 = new Dictionary<string, string[]>
        {
            ["username"] = ["Username is required."]
        };
        var dict2 = new Dictionary<string, string[]>
        {
            ["password"] = ["Password is required."]
        };

        // Act
        var result = ValidationHelpers.MergeErrors(dict1, dict2);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("username");
        result.Should().ContainKey("password");
        result["username"].Should().Contain("Username is required.");
        result["password"].Should().Contain("Password is required.");
    }

    [Fact]
    public void MergeErrors_MultipleDictionariesWithSameKey_ConcatenatesErrorArrays()
    {
        // Arrange
        var dict1 = new Dictionary<string, string[]>
        {
            ["username"] = ["Username is required."]
        };
        var dict2 = new Dictionary<string, string[]>
        {
            ["username"] = ["Username must be between 3 and 20 characters."]
        };

        // Act
        var result = ValidationHelpers.MergeErrors(dict1, dict2);

        // Assert
        result.Should().ContainKey("username");
        result["username"].Should().HaveCount(2);
        result["username"].Should().Contain("Username is required.");
        result["username"].Should().Contain("Username must be between 3 and 20 characters.");
    }

    #endregion
}
