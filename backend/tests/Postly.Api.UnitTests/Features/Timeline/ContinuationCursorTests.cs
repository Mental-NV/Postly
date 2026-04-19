using FluentAssertions;
using Postly.Api.Features.Shared.Pagination;
using Xunit;

namespace Postly.Api.UnitTests.Features.Timeline;

public class ContinuationCursorTests
{
    [Fact]
    public void TryParse_WithInvalidCursor_ReturnsFalse()
    {
        var parsed = OpaqueCursor.TryParse("not-a-valid-cursor", out _);

        parsed.Should().BeFalse();
    }

    [Fact]
    public void Paginate_WithAdditionalItems_ReturnsStableNextCursorAndNoOverlap()
    {
        var posts = Enumerable.Range(1, 25)
            .Select(index => new CursorFixture(
                Id: index,
                CreatedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-index)))
            .ToArray();

        var firstPage = OpaqueCursorPagination.Paginate(
            posts,
            OpaqueCursor.Initial,
            20,
            post => post.CreatedAtUtc,
            post => post.Id);

        var parsed = OpaqueCursor.TryParse(firstPage.NextCursor, out var nextCursor);
        parsed.Should().BeTrue();

        var secondPage = OpaqueCursorPagination.Paginate(
            posts,
            nextCursor,
            20,
            post => post.CreatedAtUtc,
            post => post.Id);

        firstPage.Items.Should().HaveCount(20);
        firstPage.NextCursor.Should().NotBeNull();
        secondPage.Items.Should().HaveCount(5);
        secondPage.NextCursor.Should().BeNull();
        secondPage.Items.Select(post => post.Id)
            .Should()
            .NotIntersectWith(firstPage.Items.Select(post => post.Id));
    }

    [Fact]
    public void Paginate_RepeatingSameCursor_ReturnsSamePage()
    {
        var posts = Enumerable.Range(1, 24)
            .Select(index => new CursorFixture(
                Id: index,
                CreatedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-index)))
            .ToArray();

        var firstPage = OpaqueCursorPagination.Paginate(
            posts,
            OpaqueCursor.Initial,
            20,
            post => post.CreatedAtUtc,
            post => post.Id);

        OpaqueCursor.TryParse(firstPage.NextCursor, out var nextCursor).Should().BeTrue();

        var secondPage = OpaqueCursorPagination.Paginate(
            posts,
            nextCursor,
            20,
            post => post.CreatedAtUtc,
            post => post.Id);

        var repeatedSecondPage = OpaqueCursorPagination.Paginate(
            posts,
            nextCursor,
            20,
            post => post.CreatedAtUtc,
            post => post.Id);

        repeatedSecondPage.Items.Select(post => post.Id)
            .Should()
            .Equal(secondPage.Items.Select(post => post.Id));
        repeatedSecondPage.NextCursor.Should().Be(secondPage.NextCursor);
    }

    private sealed record CursorFixture(long Id, DateTimeOffset CreatedAtUtc);
}
