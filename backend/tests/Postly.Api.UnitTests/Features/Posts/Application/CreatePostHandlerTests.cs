using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Security;
using Postly.Api.UnitTests.TestHelpers;
using Xunit;

namespace Postly.Api.UnitTests.Features.Posts.Application;

public class CreatePostHandlerTests
{
    #region Authorization Tests

    [Fact]
    public async Task HandleAsync_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns((long?)null);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new CreatePostRequest("Test post body");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedUser_CanCreatePost()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new CreatePostRequest("Test post body");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Created<PostResponse>>();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task HandleAsync_ValidPostBody_CreatesPostAndReturns201()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new CreatePostRequest("This is a valid post body");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Created<PostResponse>>();
        var createdResult = result as Created<PostResponse>;
        createdResult!.StatusCode.Should().Be(201);
        createdResult.Value.Should().NotBeNull();
        createdResult.Value!.Body.Should().Be("This is a valid post body");
    }

    [Fact]
    public async Task HandleAsync_EmptyBody_Returns400ValidationError()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new CreatePostRequest("");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_BodyExceeding280Chars_Returns400ValidationError()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new CreatePostRequest(new string('a', 281));

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_BodyWithLeadingTrailingSpaces_TrimmedBeforeSaving()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new CreatePostRequest("  Test post body  ");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Created<PostResponse>>();
        var createdResult = result as Created<PostResponse>;
        createdResult!.Value!.Body.Should().Be("Test post body");
    }

    #endregion

    #region Post Creation Tests

    [Fact]
    public async Task HandleAsync_ValidRequest_SavesPostToDatabase()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new CreatePostRequest("Test post body");

        // Act
        await handler.HandleAsync(request);

        // Assert
        var savedPost = dbContext.Posts.FirstOrDefault();
        savedPost.Should().NotBeNull();
        savedPost!.AuthorId.Should().Be(1L);
        savedPost.Body.Should().Be("Test post body");
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_SetsCreatedAtUtc()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new CreatePostRequest("Test post body");
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        await handler.HandleAsync(request);

        // Assert
        var savedPost = dbContext.Posts.FirstOrDefault();
        savedPost.Should().NotBeNull();
        savedPost!.CreatedAtUtc.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ResponseIncludesAuthorDetails()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new CreatePostRequest("Test post body");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Created<PostResponse>>();
        var createdResult = result as Created<PostResponse>;
        createdResult!.Value.Should().NotBeNull();
        createdResult.Value!.AuthorUsername.Should().Be("testuser");
        createdResult.Value.AuthorDisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ResponseIncludesCorrectPostData()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new CreatePostRequest("Test post body");

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        result.Should().BeOfType<Created<PostResponse>>();
        var createdResult = result as Created<PostResponse>;
        createdResult!.Value.Should().NotBeNull();
        createdResult.Value!.Id.Should().BeGreaterThan(0);
        createdResult.Value.Body.Should().Be("Test post body");
        createdResult.Value.IsEdited.Should().BeFalse();
        createdResult.Value.EditedAtUtc.Should().BeNull();
    }

    #endregion
}
