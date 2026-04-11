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

public class SigninHandlerTests
{
    #region Validation Tests

    [Fact]
    public async Task HandleAsync_ValidSigninRequest_Returns200OkWithSession()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(username: "testuser", displayName: "Test User", password: "password123");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SigninRequest("testuser", "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Ok<SessionResponse>>();
        var okResult = result as Ok<SessionResponse>;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_MissingUsernameOrPassword_Returns400ValidationError()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SigninRequest("", "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task HandleAsync_CorrectUsernameAndPassword_Returns200Ok()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(username: "testuser", displayName: "Test User", password: "password123");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SigninRequest("testuser", "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Ok<SessionResponse>>();
    }

    [Fact]
    public async Task HandleAsync_NonExistentUsername_Returns401Unauthorized()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SigninRequest("nonexistent", "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task HandleAsync_IncorrectPassword_Returns401Unauthorized()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(username: "testuser", displayName: "Test User", password: "password123");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SigninRequest("testuser", "wrongpassword");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task HandleAsync_UsernameLookupIsCaseInsensitive_Returns200Ok()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(username: "testuser", displayName: "Test User", password: "password123");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SigninRequest("TESTUSER", "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Ok<SessionResponse>>();
    }

    [Fact]
    public async Task HandleAsync_PasswordVerificationUsesPasswordHasher_VerifiesCorrectly()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(username: "testuser", displayName: "Test User", password: "password123");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SigninRequest("testuser", "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Ok<SessionResponse>>();
        var okResult = result as Ok<SessionResponse>;
        okResult!.Value!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task HandleAsync_ErrorMessageDoesNotRevealUsernameOrPasswordWrong_ReturnsSame401()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(username: "testuser", displayName: "Test User", password: "password123");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var wrongUserRequest = new SigninRequest("nonexistent", "password123");
        var wrongPasswordRequest = new SigninRequest("testuser", "wrongpassword");

        // Act
        var wrongUserResult = await handler.HandleAsync(wrongUserRequest);
        var wrongPasswordResult = await handler.HandleAsync(wrongPasswordRequest);

        // Assert
        wrongUserResult.Should().BeOfType<ProblemHttpResult>();
        wrongPasswordResult.Should().BeOfType<ProblemHttpResult>();
        var wrongUserProblem = wrongUserResult as ProblemHttpResult;
        var wrongPasswordProblem = wrongPasswordResult as ProblemHttpResult;
        wrongUserProblem!.StatusCode.Should().Be(401);
        wrongPasswordProblem!.StatusCode.Should().Be(401);
    }

    #endregion

    #region Session Management Tests

    [Fact]
    public async Task HandleAsync_ExistingActiveSessions_RevokesBeforeCreatingNew()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User", password: "password123");
        dbContext.UserAccounts.Add(user);

        var existingSession = TestDataBuilder.CreateSession(userAccountId: 1, revokedAtUtc: null);
        dbContext.Sessions.Add(existingSession);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SigninRequest("testuser", "password123");

        // Act
        await handler.HandleAsync(request);

        // Assert
        var revokedSession = await dbContext.Sessions.FindAsync(existingSession.Id);
        revokedSession.Should().NotBeNull();
        revokedSession!.RevokedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_SuccessfulSignin_CreatesNewSession()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(username: "testuser", displayName: "Test User", password: "password123");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SigninRequest("testuser", "password123");

        // Act
        await handler.HandleAsync(request);

        // Assert
        var session = await dbContext.Sessions.FirstOrDefaultAsync();
        session.Should().NotBeNull();
        session!.TokenHash.Should().NotBeNullOrEmpty();
        session.RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_SuccessfulSignin_ResponseIncludesCorrectUserData()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(username: "testuser", displayName: "Test User", password: "password123");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SigninRequest("testuser", "password123");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Ok<SessionResponse>>();
        var okResult = result as Ok<SessionResponse>;
        okResult!.Value.Should().NotBeNull();
        okResult.Value!.Username.Should().Be("testuser");
        okResult.Value.DisplayName.Should().Be("Test User");
        okResult.Value.UserId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task HandleAsync_MultipleSigninAttempts_CreatesNewSessionsAfterRevokingOld()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(username: "testuser", displayName: "Test User", password: "password123");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new SigninHandler(dbContext, mockHttpContextAccessor.Object);
        var request = new SigninRequest("testuser", "password123");

        // Act
        await handler.HandleAsync(request);
        await handler.HandleAsync(request);

        // Assert
        var sessions = await dbContext.Sessions.ToListAsync();
        sessions.Should().HaveCount(2);
        sessions.Count(s => s.RevokedAtUtc != null).Should().Be(1);
        sessions.Count(s => s.RevokedAtUtc == null).Should().Be(1);
    }

    #endregion
}
