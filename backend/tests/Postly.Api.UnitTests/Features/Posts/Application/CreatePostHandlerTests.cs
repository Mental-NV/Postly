using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Timeline.Contracts;
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
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns((long?)null);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(new CreatePostRequest("Test post body"));

        result.Should().BeOfType<ProblemHttpResult>();
        ((ProblemHttpResult)result).StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedUser_CanCreatePost()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(new CreatePostRequest("Test post body"));

        result.Should().BeOfType<Created<PostResponse>>();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task HandleAsync_ValidPostBody_CreatesPostAndReturns201()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(new CreatePostRequest("This is a valid post body"));

        result.Should().BeOfType<Created<PostResponse>>();
        var createdResult = (Created<PostResponse>)result;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value!.Post.Body.Should().Be("This is a valid post body");
    }

    [Fact]
    public async Task HandleAsync_EmptyBody_Returns400ValidationError()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(new CreatePostRequest(""));

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

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(new CreatePostRequest(new string('a', 281)));

        ((ProblemHttpResult)result).StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_BodyWithLeadingTrailingSpaces_TrimmedBeforeSaving()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(new CreatePostRequest("  Test post body  "));

        ((Created<PostResponse>)result).Value!.Post.Body.Should().Be("Test post body");
    }

    #endregion

    #region Post Creation Tests

    [Fact]
    public async Task HandleAsync_ValidRequest_SavesPostToDatabase()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        await handler.HandleAsync(new CreatePostRequest("Test post body"));

        var savedPost = dbContext.Posts.FirstOrDefault();
        savedPost.Should().NotBeNull();
        savedPost!.AuthorId.Should().Be(1L);
        savedPost.Body.Should().Be("Test post body");
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ResponseIncludesAuthorDetails()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(new CreatePostRequest("Test post body"));

        var post = ((Created<PostResponse>)result).Value!.Post;
        post.AuthorUsername.Should().Be("testuser");
        post.AuthorDisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ResponseIncludesCorrectPostData()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1, username: "testuser", displayName: "Test User");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var mockCurrentViewer = new Mock<ICurrentViewerAccessor>();
        mockCurrentViewer.Setup(x => x.GetCurrentUserId()).Returns(1L);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new CreatePostHandler(dbContext, mockCurrentViewer.Object, mockHttpContextAccessor.Object);
        var result = await handler.HandleAsync(new CreatePostRequest("Test post body"));

        var post = ((Created<PostResponse>)result).Value!.Post;
        post.Id.Should().BeGreaterThan(0);
        post.Body.Should().Be("Test post body");
        post.IsEdited.Should().BeFalse();
        post.State.Should().Be("available");
    }

    #endregion
}
