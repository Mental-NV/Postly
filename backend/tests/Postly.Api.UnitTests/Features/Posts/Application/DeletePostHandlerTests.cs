using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Security;
using Postly.Api.UnitTests.TestHelpers;
using Xunit;

namespace Postly.Api.UnitTests.Features.Posts.Application;

public class DeletePostHandlerTests
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

        var handler = new DeletePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        // Act
        var result = await handler.HandleAsync(1);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedUser_CanDeleteOwnPost()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Test post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new DeletePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        // Act
        var result = await handler.HandleAsync(1);

        // Assert
        result.Should().BeOfType<NoContent>();
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedUser_CannotDeleteAnotherUsersPost()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user1 = TestDataBuilder.CreateUserAccount(id: 1, username: "user1", displayName: "User 1");
        var user2 = TestDataBuilder.CreateUserAccount(id: 2, username: "user2", displayName: "User 2");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Test post", author: user1);
        dbContext.UserAccounts.AddRange(user1, user2);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(2L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new DeletePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        // Act
        var result = await handler.HandleAsync(1);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(403);
    }

    #endregion

    #region Post Deletion Tests

    [Fact]
    public async Task HandleAsync_ValidRequest_RemovesPostFromDatabase()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Test post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new DeletePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        // Act
        await handler.HandleAsync(1);

        // Assert
        var deletedPost = await dbContext.Posts.FindAsync(1L);
        deletedPost.Should().BeNull();
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

        var handler = new DeletePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        // Act
        var result = await handler.HandleAsync(999);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulDeletion_Returns204NoContent()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Test post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new DeletePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        // Act
        var result = await handler.HandleAsync(1);

        // Assert
        result.Should().BeOfType<NoContent>();
        var noContentResult = result as NoContent;
        noContentResult!.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task HandleAsync_DeletingPost_DoesNotAffectOtherPosts()
    {
        // Arrange
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post1 = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Post 1", author: user);
        var post2 = TestDataBuilder.CreatePost(id: 2, authorId: 1, body: "Post 2", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.AddRange(post1, post2);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new DeletePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        // Act
        await handler.HandleAsync(1);

        // Assert
        var deletedPost = await dbContext.Posts.FindAsync(1L);
        var remainingPost = await dbContext.Posts.FindAsync(2L);
        deletedPost.Should().BeNull();
        remainingPost.Should().NotBeNull();
        remainingPost!.Body.Should().Be("Post 2");
    }

    #endregion
}
