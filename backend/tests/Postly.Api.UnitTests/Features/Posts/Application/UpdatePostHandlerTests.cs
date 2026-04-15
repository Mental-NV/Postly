using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Security;
using Postly.Api.UnitTests.TestHelpers;
using Xunit;

namespace Postly.Api.UnitTests.Features.Posts.Application;

public class UpdatePostHandlerTests
{
    [Fact]
    public async Task HandleAsync_UnauthenticatedUser_Returns401Unauthorized()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns((long?)null);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(1, new UpdatePostRequest("Updated post body"));

        ((ProblemHttpResult)result).StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedUser_CanUpdateOwnPost()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(1, new UpdatePostRequest("Updated post body"));

        result.Should().BeOfType<Ok<PostResponse>>();
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedUser_CannotUpdateAnotherUsersPost()
    {
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
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(1, new UpdatePostRequest("Updated post body"));

        ((ProblemHttpResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task HandleAsync_ValidPostBody_UpdatesPostAndReturns200Ok()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(1, new UpdatePostRequest("Updated post body"));

        result.Should().BeOfType<Ok<PostResponse>>();
        ((Ok<PostResponse>)result).Value!.Post.Body.Should().Be("Updated post body");
    }

    [Fact]
    public async Task HandleAsync_EmptyBody_Returns400ValidationError()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(1, new UpdatePostRequest(""));

        ((ProblemHttpResult)result).StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_BodyExceeding280Chars_Returns400ValidationError()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(1, new UpdatePostRequest(new string('a', 281)));

        ((ProblemHttpResult)result).StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_BodyWithLeadingTrailingSpaces_TrimmedBeforeSaving()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(1, new UpdatePostRequest("  Updated post body  "));

        ((Ok<PostResponse>)result).Value!.Post.Body.Should().Be("Updated post body");
    }

    [Fact]
    public async Task HandleAsync_NonExistentPostId_Returns404NotFound()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(999, new UpdatePostRequest("Updated post body"));

        ((ProblemHttpResult)result).StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_SetsEditedAtUtc()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var beforeUpdate = DateTimeOffset.UtcNow;
        await handler.HandleAsync(1, new UpdatePostRequest("Updated post body"));

        var updatedPost = await dbContext.Posts.FindAsync(1L);
        updatedPost!.EditedAtUtc.Should().NotBeNull();
        updatedPost.EditedAtUtc!.Value.Should().BeCloseTo(beforeUpdate, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ResponseIncludesUpdatedPostData()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        var post = TestDataBuilder.CreatePost(id: 1, authorId: 1, body: "Original post", author: user);
        dbContext.UserAccounts.Add(user);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UpdatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(1, new UpdatePostRequest("Updated post body"));

        var postSummary = ((Ok<PostResponse>)result).Value!.Post;
        postSummary.Body.Should().Be("Updated post body");
        postSummary.IsEdited.Should().BeTrue();
        postSummary.State.Should().Be("available");
    }
}
