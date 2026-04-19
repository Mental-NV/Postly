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

namespace Postly.Api.UnitTests.Features.Posts;

public class ReplyContinuationTests
{
    [Fact]
    public async Task HandleAsync_WithContinuationCursor_DoesNotOverlapPages_AndExhausts()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var author = TestDataBuilder.CreateUserAccount(id: 1, username: "bob");
        dbContext.UserAccounts.Add(author);

        var parentPost = new Post
        {
            Id = 100,
            AuthorId = author.Id,
            Author = author,
            Body = "Conversation target",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.Posts.Add(parentPost);

        var replies = Enumerable.Range(1, 22)
            .Select(index => new Post
            {
                Id = 200 + index,
                AuthorId = author.Id,
                Author = author,
                Body = $"Reply #{index}",
                ReplyToPostId = parentPost.Id,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-index),
            });

        dbContext.Posts.AddRange(replies);
        await dbContext.SaveChangesAsync();

        var viewer = new Mock<ICurrentViewerAccessor>();
        viewer.Setup(accessor => accessor.GetCurrentUserId()).Returns(author.Id);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(accessor => accessor.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new GetRepliesHandler(dbContext, viewer.Object, httpContextAccessor.Object);

        var firstResult = await handler.HandleAsync(parentPost.Id, null);
        var firstPage = ((Ok<ReplyPageResponse>)firstResult).Value!;

        var secondResult = await handler.HandleAsync(parentPost.Id, firstPage.NextCursor);
        var secondPage = ((Ok<ReplyPageResponse>)secondResult).Value!;

        firstPage.Replies.Should().HaveCount(20);
        firstPage.NextCursor.Should().NotBeNull();
        secondPage.Replies.Should().HaveCount(2);
        secondPage.NextCursor.Should().BeNull();
        secondPage.Replies.Select(reply => reply.Id)
            .Should()
            .NotIntersectWith(firstPage.Replies.Select(reply => reply.Id));
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCursor_ReturnsProblemDetails()
    {
        var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var author = TestDataBuilder.CreateUserAccount(id: 1, username: "bob");
        dbContext.UserAccounts.Add(author);
        dbContext.Posts.Add(new Post
        {
            Id = 100,
            AuthorId = author.Id,
            Author = author,
            Body = "Conversation target",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var viewer = new Mock<ICurrentViewerAccessor>();
        viewer.Setup(accessor => accessor.GetCurrentUserId()).Returns(author.Id);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(accessor => accessor.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new GetRepliesHandler(dbContext, viewer.Object, httpContextAccessor.Object);

        var result = await handler.HandleAsync(100, "invalid-cursor");

        result.Should().BeOfType<ProblemHttpResult>();
        ((ProblemHttpResult)result).StatusCode.Should().Be(400);
    }
}
