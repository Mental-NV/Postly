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

public class ReplyOwnershipTests
{
    private static (Mock<ICurrentViewerAccessor>, Mock<IHttpContextAccessor>) CreateMocks(long? userId)
    {
        var mockViewer = new Mock<ICurrentViewerAccessor>();
        mockViewer.Setup(x => x.GetCurrentUserId()).Returns(userId);
        var mockHttp = new Mock<IHttpContextAccessor>();
        mockHttp.Setup(x => x.HttpContext).Returns(TestHttpContextFactory.CreateMockHttpContext());
        return (mockViewer, mockHttp);
    }

    private static async Task<(Post parent, Post reply)> SeedReplyAsync(
        Postly.Api.Persistence.AppDbContext dbContext,
        long authorId = 1,
        bool softDeleted = false)
    {
        var author = TestDataBuilder.CreateUserAccount(id: authorId);
        dbContext.UserAccounts.Add(author);
        await dbContext.SaveChangesAsync();

        var parent = new Post { AuthorId = authorId, Body = "Parent", CreatedAtUtc = DateTimeOffset.UtcNow, Author = author };
        dbContext.Posts.Add(parent);
        await dbContext.SaveChangesAsync();

        var reply = new Post
        {
            AuthorId = authorId,
            Body = "Reply body",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ReplyToPostId = parent.Id,
            Author = author,
            DeletedAtUtc = softDeleted ? DateTimeOffset.UtcNow : null
        };
        dbContext.Posts.Add(reply);
        await dbContext.SaveChangesAsync();

        return (parent, reply);
    }

    [Fact]
    public async Task UpdatePost_AuthorCanEditOwnReply()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var (_, reply) = await SeedReplyAsync(dbContext, authorId: 1);
        var (mockViewer, mockHttp) = CreateMocks(1L);
        var handler = new UpdatePostHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(reply.Id, new UpdatePostRequest("Edited reply"));

        result.Should().BeOfType<Ok<PostResponse>>();
        ((Ok<PostResponse>)result).Value!.Post.Body.Should().Be("Edited reply");
    }

    [Fact]
    public async Task UpdatePost_NonAuthorCannotEditReply_Returns403()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var (_, reply) = await SeedReplyAsync(dbContext, authorId: 1);

        // Add a second user
        var otherUser = TestDataBuilder.CreateUserAccount(id: 2, username: "other");
        dbContext.UserAccounts.Add(otherUser);
        await dbContext.SaveChangesAsync();

        var (mockViewer, mockHttp) = CreateMocks(2L);
        var handler = new UpdatePostHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(reply.Id, new UpdatePostRequest("Unauthorized edit"));

        ((ProblemHttpResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task DeletePost_AuthorCanDeleteOwnReply_SoftDeletes()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var (_, reply) = await SeedReplyAsync(dbContext, authorId: 1);
        var (mockViewer, mockHttp) = CreateMocks(1L);
        var handler = new DeletePostHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(reply.Id);

        result.Should().BeOfType<NoContent>();

        // Reply should still exist but be soft-deleted
        var deletedReply = await dbContext.Posts.FindAsync(reply.Id);
        deletedReply.Should().NotBeNull();
        deletedReply!.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task DeletePost_NonAuthorCannotDeleteReply_Returns403()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var (_, reply) = await SeedReplyAsync(dbContext, authorId: 1);

        var otherUser = TestDataBuilder.CreateUserAccount(id: 2, username: "other");
        dbContext.UserAccounts.Add(otherUser);
        await dbContext.SaveChangesAsync();

        var (mockViewer, mockHttp) = CreateMocks(2L);
        var handler = new DeletePostHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(reply.Id);

        ((ProblemHttpResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task DeletePost_AlreadySoftDeletedReply_Returns404()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var (_, reply) = await SeedReplyAsync(dbContext, authorId: 1, softDeleted: true);
        var (mockViewer, mockHttp) = CreateMocks(1L);
        var handler = new DeletePostHandler(dbContext, mockViewer.Object, mockHttp.Object);

        var result = await handler.HandleAsync(reply.Id);

        ((ProblemHttpResult)result).StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PostSummaryFactory_DeletedReply_ProjectsAsPlaceholder()
    {
        var author = TestDataBuilder.CreateUserAccount(id: 1);
        var reply = new Post
        {
            Id = 5,
            AuthorId = 1,
            Body = "This should not appear",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ReplyToPostId = 1,
            DeletedAtUtc = DateTimeOffset.UtcNow,
            Author = author
        };

        var summary = PostSummaryFactory.Create(reply, viewerId: 1L, likeCount: 0, likedByViewer: false);

        summary.State.Should().Be("deleted");
        summary.Body.Should().BeNull();
        summary.AuthorUsername.Should().BeNull();
        summary.AuthorDisplayName.Should().BeNull();
        summary.CanEdit.Should().BeFalse();
        summary.CanDelete.Should().BeFalse();
    }

    [Fact]
    public async Task PostSummaryFactory_AvailableReply_ProjectsWithAuthorAndBody()
    {
        var author = TestDataBuilder.CreateUserAccount(id: 1, username: "alice", displayName: "Alice");
        var reply = new Post
        {
            Id = 5,
            AuthorId = 1,
            Body = "My reply",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ReplyToPostId = 1,
            Author = author
        };

        var summary = PostSummaryFactory.Create(reply, viewerId: 1L, likeCount: 0, likedByViewer: false);

        summary.State.Should().Be("available");
        summary.Body.Should().Be("My reply");
        summary.AuthorUsername.Should().Be("alice");
        summary.IsReply.Should().BeTrue();
        summary.ReplyToPostId.Should().Be(1);
        summary.CanEdit.Should().BeTrue();
        summary.CanDelete.Should().BeTrue();
    }

    [Fact]
    public async Task PostSummaryFactory_NonAuthorViewingReply_CanEditAndDeleteAreFalse()
    {
        var author = TestDataBuilder.CreateUserAccount(id: 1, username: "alice");
        var reply = new Post
        {
            Id = 5,
            AuthorId = 1,
            Body = "My reply",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ReplyToPostId = 1,
            Author = author
        };

        var summary = PostSummaryFactory.Create(reply, viewerId: 2L, likeCount: 0, likedByViewer: false);

        summary.CanEdit.Should().BeFalse();
        summary.CanDelete.Should().BeFalse();
    }
}
