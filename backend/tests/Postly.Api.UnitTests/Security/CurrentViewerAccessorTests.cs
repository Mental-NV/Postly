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

    [Theory]
    [InlineData("null_context")] // HttpContext is null
    [InlineData("empty_principal")] // User has no claims
    [InlineData("missing_claim")] // NameIdentifier claim missing
    public void GetCurrentUserId_InvalidContextOrClaims_ReturnsNull(string scenario)
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        if (scenario == "null_context")
        {
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        }
        else
        {
            var mockHttpContext = new Mock<HttpContext>();

            if (scenario == "empty_principal")
            {
                var emptyPrincipal = new ClaimsPrincipal();
                mockHttpContext.Setup(x => x.User).Returns(emptyPrincipal);
            }
            else if (scenario == "missing_claim")
            {
                var claims = new List<Claim> { new Claim(ClaimTypes.Name, "testuser") };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                var claimsPrincipal = new ClaimsPrincipal(identity);
                mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            }

            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
        }

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

    [Theory]
    [InlineData(true)] // HttpContext is null
    [InlineData(false)] // User is not authenticated
    public void IsAuthenticated_InvalidContextOrNotAuthenticated_ReturnsFalse(bool isNullContext)
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        if (isNullContext)
        {
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        }
        else
        {
            var mockHttpContext = new Mock<HttpContext>();
            var mockIdentity = new Mock<ClaimsIdentity>();
            mockIdentity.Setup(x => x.IsAuthenticated).Returns(false);
            var claimsPrincipal = new ClaimsPrincipal(mockIdentity.Object);
            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
        }

        var accessor = new CurrentViewerAccessor(mockHttpContextAccessor.Object);

        // Act
        var result = accessor.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
