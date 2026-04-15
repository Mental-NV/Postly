using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Postly.Api.Features.Profiles.Application;
using Postly.Api.Security;
using Postly.Api.UnitTests.TestHelpers;
using Xunit;

namespace Postly.Api.UnitTests.Features.Notifications;

public class NotificationCreationTests
{
    [Fact]
    public async Task FollowUser_CreatesNotificationForFollowedUser()
    {
        using var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var currentViewer = TestDataBuilder.CreateMockCurrentViewer(1);
        var httpContext = TestHttpContextFactory.CreateMockHttpContext();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new FollowUserHandler(dbContext, currentViewer.Object, httpContextAccessor.Object);

        var follower = TestDataBuilder.CreateUser(1, "alice");
        var followed = TestDataBuilder.CreateUser(2, "bob");
        dbContext.UserAccounts.AddRange(follower, followed);
        await dbContext.SaveChangesAsync();

        await handler.HandleAsync("bob");

        var notification = await dbContext.Notifications.FirstOrDefaultAsync();
        notification.Should().NotBeNull();
        notification!.RecipientUserId.Should().Be(2);
        notification.ActorUserId.Should().Be(1);
        notification.Kind.Should().Be("follow");
    }

    [Fact]
    public async Task FollowUser_SelfFollow_DoesNotCreateNotification()
    {
        using var dbContext = TestDbContextFactory.CreateInMemoryDbContext();
        var currentViewer = TestDataBuilder.CreateMockCurrentViewer(1);
        var httpContext = TestHttpContextFactory.CreateMockHttpContext();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new FollowUserHandler(dbContext, currentViewer.Object, httpContextAccessor.Object);

        var user = TestDataBuilder.CreateUser(1, "alice");
        dbContext.UserAccounts.Add(user);
        await dbContext.SaveChangesAsync();

        await handler.HandleAsync("alice");

        var notifications = await dbContext.Notifications.ToListAsync();
        notifications.Should().BeEmpty();
    }
}
