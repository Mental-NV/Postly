using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Security;
using Postly.Api.UnitTests.TestHelpers;
using Xunit;

namespace Postly.Api.UnitTests.Features.Posts.Application;

public class UpdatePostHandlerTests
{
    #region Authorization Tests

    [Fact]
    public async Task HandleAsync_UnauthenticatedUser_Returns401Unauthorized()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns((long?)null);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new UpdatePostRequest("Updated post body");

        // Act
        var result = await handler.HandleAsync(1, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedUser_CanUpdateOwnPost()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new UpdatePostRequest("Updated post body");

        // Act
        var result = await handler.HandleAsync(1, request);

        // Assert
        result.Should().BeOfType<Ok<PostResponse>>();
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedUser_CannotUpdateAnotherUsersPost()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user1 = TestDataBuilder.CreateUserAccount(id: 1, username: "user1", displayName: "User 1");
        var user2 = TestDataBuilder.CreateUserAccount(id: 2, username: "user2", displayName: "User 2");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user1);
        dbContext.UserAccounts.AddRange(user1, user2);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(2L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new UpdatePostRequest("Updated post body");

        // Act
        var result = await handler.HandleAsync(1, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(403);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task HandleAsync_ValidPostBody_UpdatesPostAndReturns200Ok()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new UpdatePostRequest("Updated post body");

        // Act
        var result = await handler.HandleAsync(1, request);

        // Assert
        result.Should().BeOfType<Ok<PostResponse>>();
        var okResult = result as Ok<PostResponse>;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value!.Body.Should().Be("Updated post body");
    }

    [Fact]
    public async Task HandleAsync_EmptyBody_Returns400ValidationError()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new UpdatePostRequest("");

        // Act
        var result = await handler.HandleAsync(1, request);

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
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new UpdatePostRequest(new string('a', 281));

        // Act
        var result = await handler.HandleAsync(1, request);

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
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new UpdatePostRequest("  Updated post body  ");

        // Act
        var result = await handler.HandleAsync(1, request);

        // Assert
        result.Should().BeOfType<Ok<PostResponse>>();
        var okResult = result as Ok<PostResponse>;
        okResult!.Value!.Body.Should().Be("Updated post body");
    }

    #endregion

    #region Post Update Tests

    [Fact]
    public async Task HandleAsync_ValidRequest_UpdatesPostBodyInDatabase()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new UpdatePostRequest("Updated post body");

        // Act
        await handler.HandleAsync(1, request);

        // Assert
        var updatedPost = await dbContext.Posts.FindAsync(1L);
        updatedPost.Should().NotBeNull();
        updatedPost!.Body.Should().Be("Updated post body");
    }

    [Fact]
    public async Task HandleAsync_NonExistentPostId_Returns404NotFound()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new UpdatePostRequest("Updated post body");

        // Act
        var result = await handler.HandleAsync(999, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_SetsEditedAtUtc()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new UpdatePostRequest("Updated post body");
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        await handler.HandleAsync(1, request);

        // Assert
        var updatedPost = await dbContext.Posts.FindAsync(1L);
        updatedPost.Should().NotBeNull();
        updatedPost!.EditedAtUtc.Should().NotBeNull();
        updatedPost.EditedAtUtc!.Value.Should().BeCloseTo(beforeUpdate, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ResponseIncludesUpdatedPostData()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var request = new UpdatePostRequest("Updated post body");

        // Act
        var result = await handler.HandleAsync(1, request);

        // Assert
        result.Should().BeOfType<Ok<PostResponse>>();
        var okResult = result as Ok<PostResponse>;
        okResult!.Value.Should().NotBeNull();
        okResult.Value!.Body.Should().Be("Updated post body");
        okResult.Value.IsEdited.Should().BeTrue();
        okResult.Value.EditedAtUtc.Should().NotBeNull();
    }

    #endregion
}
