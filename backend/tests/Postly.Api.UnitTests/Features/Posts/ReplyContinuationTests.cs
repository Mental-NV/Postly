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
        var (author, _, _) = TestDataBuilder.CreateRound2Users();
        var scenario = TestDataBuilder.CreateAvailableConversationThread(author);
        dbContext.UserAccounts.Add(author);
        dbContext.Posts.Add(scenario.ParentPost);
        dbContext.Posts.AddRange(scenario.Replies);
        await dbContext.SaveChangesAsync();

        var viewer = new Mock<ICurrentViewerAccessor>();
        viewer.Setup(accessor => accessor.GetCurrentUserId()).Returns(author.Id);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(accessor => accessor.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new GetRepliesHandler(dbContext, viewer.Object, httpContextAccessor.Object);

        var firstResult = await handler.HandleAsync(scenario.ParentPost.Id, null);
        var firstPage = ((Ok<ReplyPageResponse>)firstResult).Value!;

        var secondResult = await handler.HandleAsync(scenario.ParentPost.Id, firstPage.NextCursor);
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
        var (author, _, _) = TestDataBuilder.CreateRound2Users();
        var scenario = TestDataBuilder.CreateAvailableConversationThread(author, replyCount: 0);
        dbContext.UserAccounts.Add(author);
        dbContext.Posts.Add(scenario.ParentPost);
        await dbContext.SaveChangesAsync();

        var viewer = new Mock<ICurrentViewerAccessor>();
        viewer.Setup(accessor => accessor.GetCurrentUserId()).Returns(author.Id);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(accessor => accessor.HttpContext)
            .Returns(TestHttpContextFactory.CreateMockHttpContext());

        var handler = new GetRepliesHandler(dbContext, viewer.Object, httpContextAccessor.Object);

        var result = await handler.HandleAsync(scenario.ParentPost.Id, "invalid-cursor");

        result.Should().BeOfType<ProblemHttpResult>();
        ((ProblemHttpResult)result).StatusCode.Should().Be(400);
    }
}
