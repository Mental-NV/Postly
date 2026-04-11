using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Persistence.Entities;
using Postly.Api.Security;
using Postly.Api.UnitTests.TestHelpers;
using Xunit;

namespace Postly.Api.UnitTests.Features.Posts.Application;

public class PostInteractionHandlerTests
{
    [Fact]
    public async Task LikeHandleAsync_UnauthenticatedUser_ReturnsUnauthorized()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(viewer => viewer.GetCurrentUserId()).Returns((long?)null);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(accessor => accessor.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new LikePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        var result = await handler.HandleAsync(1);

        result.Should().BeOfType<ProblemHttpResult>();
        (result as ProblemHttpResult)!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task LikeHandleAsync_MissingPost_ReturnsNotFound()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(viewer => viewer.GetCurrentUserId()).Returns(1L);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(accessor => accessor.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new LikePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        var result = await handler.HandleAsync(999);

        result.Should().BeOfType<ProblemHttpResult>();
        (result as ProblemHttpResult)!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task LikeHandleAsync_AddsLikeAndReturnsUpdatedState()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var author = TestDataBuilder.CreateUserAccount(id: 2, username: "alice", displayName: "Alice Example");
        var viewer = TestDataBuilder.CreateUserAccount(id: 1, username: "bob", displayName: "Bob Tester");
        var post = TestDataBuilder.CreatePost(id: 42, authorId: author.Id, author: author);

        dbContext.UserAccounts.AddRange(viewer, author);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(currentViewer => currentViewer.GetCurrentUserId()).Returns(viewer.Id);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(accessor => accessor.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new LikePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        var result = await handler.HandleAsync(post.Id);

        result.Should().BeOfType<Ok<PostInteractionState>>();
        var okResult = result as Ok<PostInteractionState>;
        okResult!.Value.Should().Be(new PostInteractionState(post.Id, 1, true));
        dbContext.Likes.Should().ContainSingle(like => like.UserAccountId == viewer.Id && like.PostId == post.Id);
    }

    [Fact]
    public async Task LikeHandleAsync_RepeatedLike_IsIdempotent()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var author = TestDataBuilder.CreateUserAccount(id: 2, username: "alice", displayName: "Alice Example");
        var viewer = TestDataBuilder.CreateUserAccount(id: 1, username: "bob", displayName: "Bob Tester");
        var post = TestDataBuilder.CreatePost(id: 42, authorId: author.Id, author: author);

        dbContext.UserAccounts.AddRange(viewer, author);
        dbContext.Posts.Add(post);
        dbContext.Likes.Add(new Like
        {
            UserAccountId = viewer.Id,
            PostId = post.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(currentViewer => currentViewer.GetCurrentUserId()).Returns(viewer.Id);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(accessor => accessor.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new LikePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        var result = await handler.HandleAsync(post.Id);

        result.Should().BeOfType<Ok<PostInteractionState>>();
        var okResult = result as Ok<PostInteractionState>;
        okResult!.Value.Should().Be(new PostInteractionState(post.Id, 1, true));
        dbContext.Likes.Should().ContainSingle(like => like.UserAccountId == viewer.Id && like.PostId == post.Id);
    }

    [Fact]
    public async Task UnlikeHandleAsync_RemovesExistingLikeAndReturnsUpdatedState()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var author = TestDataBuilder.CreateUserAccount(id: 2, username: "alice", displayName: "Alice Example");
        var viewer = TestDataBuilder.CreateUserAccount(id: 1, username: "bob", displayName: "Bob Tester");
        var post = TestDataBuilder.CreatePost(id: 42, authorId: author.Id, author: author);

        dbContext.UserAccounts.AddRange(viewer, author);
        dbContext.Posts.Add(post);
        dbContext.Likes.Add(new Like
        {
            UserAccountId = viewer.Id,
            PostId = post.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(currentViewer => currentViewer.GetCurrentUserId()).Returns(viewer.Id);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(accessor => accessor.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UnlikePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        var result = await handler.HandleAsync(post.Id);

        result.Should().BeOfType<Ok<PostInteractionState>>();
        var okResult = result as Ok<PostInteractionState>;
        okResult!.Value.Should().Be(new PostInteractionState(post.Id, 0, false));
        dbContext.Likes.Should().BeEmpty();
    }

    [Fact]
    public async Task UnlikeHandleAsync_RepeatedUnlike_IsIdempotent()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var author = TestDataBuilder.CreateUserAccount(id: 2, username: "alice", displayName: "Alice Example");
        var viewer = TestDataBuilder.CreateUserAccount(id: 1, username: "bob", displayName: "Bob Tester");
        var post = TestDataBuilder.CreatePost(id: 42, authorId: author.Id, author: author);

        dbContext.UserAccounts.AddRange(viewer, author);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(currentViewer => currentViewer.GetCurrentUserId()).Returns(viewer.Id);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(accessor => accessor.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new UnlikePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);

        var result = await handler.HandleAsync(post.Id);

        result.Should().BeOfType<Ok<PostInteractionState>>();
        var okResult = result as Ok<PostInteractionState>;
        okResult!.Value.Should().Be(new PostInteractionState(post.Id, 0, false));
        dbContext.Likes.Should().BeEmpty();
    }
}
