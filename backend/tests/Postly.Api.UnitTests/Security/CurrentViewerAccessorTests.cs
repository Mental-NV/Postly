using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Postly.Api.Security;
using Xunit;

namespace Postly.Api.UnitTests.Security;

public class CurrentViewerAccessorTests
{
    #region GetCurrentUserId Tests

    [Fact]
    public void GetCurrentUserId_ValidNameIdentifierClaim_ReturnsUserId()
    {
        // Arrange
        var userId = 123L;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var accessor = new CurrentViewerAccessor(mockHttpContextAccessor.Object);

        // Act
        var result = accessor.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_HttpContextIsNull_ReturnsNull()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var accessor = new CurrentViewerAccessor(mockHttpContextAccessor.Object);

        // Act
        var result = accessor.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_UserIsNull_ReturnsNull()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var emptyPrincipal = new ClaimsPrincipal();
        mockHttpContext.Setup(x => x.User).Returns(emptyPrincipal);
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var accessor = new CurrentViewerAccessor(mockHttpContextAccessor.Object);

        // Act
        var result = accessor.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_NameIdentifierClaimMissing_ReturnsNull()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var accessor = new CurrentViewerAccessor(mockHttpContextAccessor.Object);

        // Act
        var result = accessor.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_NameIdentifierClaimNotValidLong_ReturnsNull()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-number")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var accessor = new CurrentViewerAccessor(mockHttpContextAccessor.Object);

        // Act
        var result = accessor.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsAuthenticated Tests

    [Fact]
    public void IsAuthenticated_UserIsAuthenticated_ReturnsTrue()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var mockIdentity = new Mock<ClaimsIdentity>();
        mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
        var claimsPrincipal = new ClaimsPrincipal(mockIdentity.Object);

        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var accessor = new CurrentViewerAccessor(mockHttpContextAccessor.Object);

        // Act
        var result = accessor.IsAuthenticated();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_HttpContextIsNull_ReturnsFalse()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var accessor = new CurrentViewerAccessor(mockHttpContextAccessor.Object);

        // Act
        var result = accessor.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_UserIsNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var mockIdentity = new Mock<ClaimsIdentity>();
        mockIdentity.Setup(x => x.IsAuthenticated).Returns(false);
        var claimsPrincipal = new ClaimsPrincipal(mockIdentity.Object);

        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var accessor = new CurrentViewerAccessor(mockHttpContextAccessor.Object);

        // Act
        var result = accessor.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
