using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence.Entities;
using Postly.Api.Security;
using Postly.Api.UnitTests.TestHelpers;
using Xunit;

namespace Postly.Api.UnitTests.Features.Posts;

public class ReplyValidationTests
{
    private static (Mock<ICurrentViewerAccessor>, Mock<IHttpContextAccessor>) CreateMocks(long? userId = 1L)
    {
        var mockViewer = new Mock<ICurrentViewerAccessor>();
        mockViewer.Setup(x => x.GetCurrentUserId()).Returns(userId);
        var mockHttp = new Mock<IHttpContextAccessor>();
        mockHttp.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());
        return (mockViewer, mockHttp);
    }

    [Fact]
    public async Task CreateReply_EmptyBody_Returns400()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var (mockViewer, mockHttp) = CreateMocks();
        var handler = new CreateReplyHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(1, new CreateReplyRequest(""));

        ((ProblemHttpResult)result).StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateReply_BodyOver280Chars_Returns400()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var (mockViewer, mockHttp) = CreateMocks();
        var handler = new CreateReplyHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(1, new CreateReplyRequest(new string('x', 281)));

        ((ProblemHttpResult)result).StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateReply_TargetPostNotFound_Returns404()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var user = TestDataBuilder.CreateUserAccount(id: 1);
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        var (mockViewer, mockHttp) = CreateMocks();
        var handler = new CreateReplyHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(999, new CreateReplyRequest("Valid reply body"));

        ((ProblemHttpResult)result).StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateReply_TargetPostDeleted_Returns404()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var author = TestDataBuilder.CreateUserAccount(id: 1);
        dbContext.UserAccounts.Add(author);
        await dbContext.SaveChangesAsync();

        // A soft-deleted top-level post (simulated via DeletedAtUtc)
        var deletedPost = new Post
        {
            Id = 10,
            AuthorId = 1,
            Body = "deleted",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            DeletedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Posts.Add(deletedPost);
        await dbContext.SaveChangesAsync();

        var (mockViewer, mockHttp) = CreateMocks();
        var handler = new CreateReplyHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(10, new CreateReplyRequest("Valid reply body"));

        ((ProblemHttpResult)result).StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateReply_Unauthenticated_Returns401()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var (mockViewer, mockHttp) = CreateMocks(null);
        var handler = new CreateReplyHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(1, new CreateReplyRequest("Valid reply body"));

        ((ProblemHttpResult)result).StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task CreateReply_ValidBodyAndTarget_Returns201WithReply()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var author = TestDataBuilder.CreateUserAccount(id: 1);
        dbContext.UserAccounts.Add(author);
        await dbContext.SaveChangesAsync();

        var targetPost = new Post
        {
            AuthorId = 1,
            Body = "Target post",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Author = author
        };
        dbContext.Posts.Add(targetPost);
        await dbContext.SaveChangesAsync();

        var (mockViewer, mockHttp) = CreateMocks(1L);
        var handler = new CreateReplyHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(targetPost.Id, new CreateReplyRequest("My reply"));

        result.Should().BeOfType<Created<PostResponse>>();
        var created = (Created<PostResponse>)result;
        created.Value!.Post.IsReply.Should().BeTrue();
        created.Value.Post.ReplyToPostId.Should().Be(targetPost.Id);
        created.Value.Post.State.Should().Be("available");
    }

    [Fact]
    public async Task CreateReply_BodyTrimmed_SavesTrimmedValue()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var author = TestDataBuilder.CreateUserAccount(id: 1);
        dbContext.UserAccounts.Add(author);
        await dbContext.SaveChangesAsync();

        var targetPost = new Post
        {
            AuthorId = 1,
            Body = "Target post",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Author = author
        };
        dbContext.Posts.Add(targetPost);
        await dbContext.SaveChangesAsync();

        var (mockViewer, mockHttp) = CreateMocks(1L);
        var handler = new CreateReplyHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(targetPost.Id, new CreateReplyRequest("  trimmed reply  "));

        ((Created<PostResponse>)result).Value!.Post.Body.Should().Be("trimmed reply");
    }
}
