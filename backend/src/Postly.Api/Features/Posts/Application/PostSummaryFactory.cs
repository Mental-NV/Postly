using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Profiles.Application;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using Postly.Api.Persistence.Entities;

namespace Postly.Api.Features.Posts.Application;

public static class PostSummaryFactory
{
    public static async Task<PostSummary[]> CreateManyAsync(
        AppDbContext dbContext,
        IReadOnlyList<Post> posts,
        long? viewerId)
    {
        if (posts.Count == 0)
        {
            return [];
        }

        var postIds = posts.Select(post => post.Id).ToArray();

        var likeCounts = await dbContext.Likes
            .Where(like => postIds.Contains(like.PostId))
            .GroupBy(like => like.PostId)
            .Select(group => new { PostId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(result => result.PostId, result => result.Count);

        HashSet<long> likedPostIds = [];

        if (viewerId != null)
        {
            likedPostIds = await dbContext.Likes
                .Where(like => like.UserAccountId == viewerId.Value && postIds.Contains(like.PostId))
                .Select(like => like.PostId)
                .ToHashSetAsync();
        }

        return posts
            .Select(post => Create(
                post,
                viewerId,
                likeCounts.GetValueOrDefault(post.Id, 0),
                likedPostIds.Contains(post.Id)))
            .ToArray();
    }

    public static PostSummary Create(
        Post post,
        long? viewerId,
        int likeCount,
        bool likedByViewer)
    {
        var isDeleted = post.DeletedAtUtc != null;
        var isOwnedByViewer = !isDeleted && viewerId != null && post.AuthorId == viewerId.Value;

        return new PostSummary(
            Id: post.Id,
            AuthorUsername: isDeleted ? null : post.Author.Username,
            AuthorDisplayName: isDeleted ? null : post.Author.DisplayName,
            AuthorAvatarUrl: isDeleted ? null : ProfileIdentityProjection.CreateAvatarUrl(post.Author),
            Body: isDeleted ? null : post.Body,
            CreatedAtUtc: post.CreatedAtUtc,
            IsEdited: post.EditedAtUtc != null,
            LikeCount: likeCount,
            LikedByViewer: likedByViewer,
            CanEdit: isOwnedByViewer,
            CanDelete: isOwnedByViewer,
            IsReply: post.ReplyToPostId != null,
            ReplyToPostId: post.ReplyToPostId,
            State: isDeleted ? "deleted" : "available"
        );
    }
}
