using Postly.Api.Features.Timeline.Contracts;

namespace Postly.Api.Features.Posts.Contracts;

public record ConversationTarget(
    string State,
    PostSummary? Post
);

public record ConversationResponse(
    ConversationTarget Target,
    PostSummary[] Replies,
    string? NextCursor
);

public record ReplyPageResponse(
    PostSummary[] Replies,
    string? NextCursor
);

public record PostResponse(PostSummary Post);
