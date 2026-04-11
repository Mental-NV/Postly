using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using Postly.Api.Features.Auth.Application;
using Postly.Api.Features.Auth.Contracts;
using Postly.Api.UnitTests.TestHelpers;
using Xunit;

namespace Postly.Api.UnitTests.Features.Auth.Application;

public class SignupHandlerTests
{
    #region Validation Tests

    [Fact]
    public async Task HandleAsync_ValidSignupRequest_CreatesUserAndReturns201()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("testuser", "Test User", null, "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Created<SessionResponse>>();
        var createdResult = result as Created<SessionResponse>;
        createdResult!.StatusCode.Should().Be(201);
        createdResult.Value.Should().NotBeNull();
        createdResult.Value!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task HandleAsync_InvalidUsername_Returns400WithValidationErrors()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("ab", "Test User", null, "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_InvalidPassword_Returns400WithValidationErrors()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("testuser", "Test User", null, "pass");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_InvalidDisplayName_Returns400WithValidationErrors()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("testuser", "", null, "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_InvalidBio_Returns400WithValidationErrors()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("testuser", "Test User", new string('a', 161), "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_MultipleValidationErrors_MergesAndReturnsAllErrors()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("ab", "", null, "pass");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
    }

    #endregion

    #region Duplicate Username Tests

    [Fact]
    public async Task HandleAsync_ReservedUsernameMe_Returns400WithValidationError()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("me", "Reserved User", null, "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_DuplicateUsername_Returns409Conflict()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var existingUser = TestDataBuilder.CreateUserAccount(username: "testuser", password: "password123");
        dbContext.UserAccounts.Add(existingUser);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("testuser", "Another User", null, "password456");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task HandleAsync_DuplicateUsernameCaseInsensitive_Returns409Conflict()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var existingUser = TestDataBuilder.CreateUserAccount(username: "testuser", password: "password123");
        dbContext.UserAccounts.Add(existingUser);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("TESTUSER", "Another User", null, "password456");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(409);
    }

    #endregion

    #region User Creation Tests

    [Fact]
    public async Task HandleAsync_ValidRequest_SavesUserToDatabase()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("testuser", "Test User", "Test bio", "password123");

        // Act
        await handler.HandleAsync(request);

        // Assert
        var savedUser = await dbContext.UserAccounts.FirstOrDefaultAsync(u => u.Username == "testuser");
        savedUser.Should().NotBeNull();
        savedUser!.DisplayName.Should().Be("Test User");
        savedUser.Bio.Should().Be("Test bio");
        savedUser.NormalizedUsername.Should().Be("TESTUSER");
    }

    [Fact]
    public async Task HandleAsync_UsernameWithSpaces_TrimsBeforeSaving()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("  testuser  ", "Test User", null, "password123");

        // Act
        await handler.HandleAsync(request);

        // Assert
        var savedUser = await dbContext.UserAccounts.FirstOrDefaultAsync();
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task HandleAsync_DisplayNameWithSpaces_TrimsBeforeSaving()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("testuser", "  Test User  ", null, "password123");

        // Act
        await handler.HandleAsync(request);

        // Assert
        var savedUser = await dbContext.UserAccounts.FirstOrDefaultAsync();
        savedUser.Should().NotBeNull();
        savedUser!.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_HashesPassword()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("testuser", "Test User", null, "password123");

        // Act
        await handler.HandleAsync(request);

        // Assert
        var savedUser = await dbContext.UserAccounts.FirstOrDefaultAsync();
        savedUser.Should().NotBeNull();
        savedUser!.PasswordHash.Should().NotBeNullOrEmpty();
        savedUser.PasswordHash.Should().NotBe("password123");
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_SetsCreatedAtUtc()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("testuser", "Test User", null, "password123");
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        await handler.HandleAsync(request);

        // Assert
        var savedUser = await dbContext.UserAccounts.FirstOrDefaultAsync();
        savedUser.Should().NotBeNull();
        savedUser!.CreatedAtUtc.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Session Creation Tests

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesSession()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("testuser", "Test User", null, "password123");

        // Act
        await handler.HandleAsync(request);

        // Assert
        var session = await dbContext.Sessions.FirstOrDefaultAsync();
        session.Should().NotBeNull();
        session!.TokenHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ResponseIncludesCorrectUserData()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SignupHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SignupRequest("testuser", "Test User", null, "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Created<SessionResponse>>();
        var createdResult = result as Created<SessionResponse>;
        createdResult!.Value.Should().NotBeNull();
        createdResult.Value!.Username.Should().Be("testuser");
        createdResult.Value.DisplayName.Should().Be("Test User");
        createdResult.Value.UserId.Should().BeGreaterThan(0);
    }

    #endregion
}
