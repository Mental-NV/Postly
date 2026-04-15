using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Postly.Api.Features.Profiles.Application;
using Postly.Api.Features.Profiles.Contracts;
using Postly.Api.Security;
using Postly.Api.UnitTests.TestHelpers;
using Xunit;

namespace Postly.Api.UnitTests.Features.Profiles;

public class ProfileEditValidationTests
{
    [Fact]
    public void Validate_TrimmedDisplayNameAndBlankBio_AcceptsValidRequest()
    {
        var request = new UpdateProfileRequest("  Bob Updated  ", "   ");

        var errors = UpdateProfileHandler.Validate(request);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyTrimmedDisplayName_ReturnsDisplayNameError()
    {
        var request = new UpdateProfileRequest("   ", "Valid bio");

        var errors = UpdateProfileHandler.Validate(request);

        errors.Should().ContainKey("displayName");
    }

    [Fact]
    public void Validate_DisplayNameLongerThanFiftyCharacters_ReturnsDisplayNameError()
    {
        var request = new UpdateProfileRequest(new string('a', 51), "Valid bio");

        var errors = UpdateProfileHandler.Validate(request);

        errors.Should().ContainKey("displayName");
    }

    [Fact]
    public void Validate_BioLongerThanOneHundredSixtyCharacters_ReturnsBioError()
    {
        var request = new UpdateProfileRequest("Bob Updated", new string('b', 161));

        var errors = UpdateProfileHandler.Validate(request);

        errors.Should().ContainKey("bio");
    }

    [Fact]
    public async Task HandleAsync_WhenViewerIsMissing_ReturnsUnauthorized()
    {
        using var dbContext = TestDbContextFactory.CreateInMemoryDbContext();

        var currentViewer = new Mock<ICurrentViewerAccessor>();
        currentViewer.Setup(accessor => accessor.GetCurrentUserId()).Returns((long?)null);

        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var getProfileHandler = new GetProfileHandler(dbContext, currentViewer.Object, httpContextAccessor);
        var handler = new UpdateProfileHandler(
            dbContext,
            currentViewer.Object,
            getProfileHandler,
            httpContextAccessor);

        var result = await handler.HandleAsync(new UpdateProfileRequest("Bob Updated", "Bio"));

        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)result).StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ReplaceAvatar_WhenViewerIsMissing_ReturnsUnauthorized()
    {
        using var dbContext = TestDbContextFactory.CreateInMemoryDbContext();

        var currentViewer = new Mock<ICurrentViewerAccessor>();
        currentViewer.Setup(accessor => accessor.GetCurrentUserId()).Returns((long?)null);

        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var getProfileHandler = new GetProfileHandler(dbContext, currentViewer.Object, httpContextAccessor);
        var handler = new ReplaceAvatarHandler(
            dbContext,
            currentViewer.Object,
            getProfileHandler,
            new ProfileAvatarProcessor(),
            httpContextAccessor);

        var avatarFile = new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "avatar", "avatar.png");

        var result = await handler.HandleAsync(avatarFile);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)result).StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
