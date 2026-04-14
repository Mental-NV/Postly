# Data Model: Postly Round 2

## Purpose

This document captures the Round 2 entity changes and read-model impact needed
for profile editing, avatar replacement, replies, notifications, and
continuation loading.

## Entity Changes

### UserAccount

| Field | Type | Rules / Notes |
|-------|------|---------------|
| `Id` | `long` | Existing primary key |
| `Username` | `string` | Existing display/login identifier |
| `NormalizedUsername` | `string` | Existing unique lookup key |
| `DisplayName` | `string` | Trimmed; required; 1-50 chars after trimming |
| `Bio` | `string?` | Blank allowed; max 160 chars |
| `PasswordHash` | `string` | Existing password hasher output |
| `CreatedAtUtc` | `DateTimeOffset` | Existing immutable timestamp |
| `AvatarContentType` | `string?` | Null when no current custom avatar exists |
| `AvatarBytes` | `byte[]?` | Current custom avatar payload only |
| `AvatarUpdatedAtUtc` | `DateTimeOffset?` | Updated on successful avatar replacement |

**Rules**

- Only the signed-in owner can mutate `DisplayName`, `Bio`, or avatar fields.
- If custom avatar data is absent or unavailable, the read model must expose the
  generated default avatar instead of a broken image state.

### Post

| Field | Type | Rules / Notes |
|-------|------|---------------|
| `Id` | `long` | Existing primary key |
| `AuthorId` | `long` | Existing FK to `UserAccount` |
| `Body` | `string` | Required for active posts/replies; existing 1-280 char rules |
| `CreatedAtUtc` | `DateTimeOffset` | Existing publish timestamp |
| `EditedAtUtc` | `DateTimeOffset?` | Existing edit timestamp |
| `ReplyToPostId` | `long?` | Null for top-level posts; parent target ID for replies |
| `DeletedAtUtc` | `DateTimeOffset?` | Null for active items; set when reply is deleted and should render as placeholder |

**Rules**

- Replies are posts with `ReplyToPostId != null`.
- Only the author may edit or delete a reply.
- Deleting a reply does not remove its conversation slot; it moves the reply to
  placeholder state.
- Existing visibility/access rules for posts remain the baseline rules for
  replies.

### Notification

| Field | Type | Rules / Notes |
|-------|------|---------------|
| `Id` | `long` | Primary key |
| `RecipientUserId` | `long` | FK to receiving user |
| `ActorUserId` | `long` | FK to acting user |
| `Kind` | `string` | `follow`, `like`, or `reply` |
| `ProfileUserId` | `long?` | Set for profile-oriented destinations such as follows |
| `PostId` | `long?` | Set for post/conversation-related destinations |
| `ReplyPostId` | `long?` | Set when `Kind=reply` and the created reply is relevant to projections |
| `CreatedAtUtc` | `DateTimeOffset` | Creation timestamp |
| `ReadAtUtc` | `DateTimeOffset?` | Null until the selected notification is opened |

**Rules**

- Notifications are created only for successful follow/like/reply events when
  actor and recipient differ.
- Viewing the notifications list alone does not change `ReadAtUtc`.
- Opening one notification changes only that notification's `ReadAtUtc`.

### Follow

No schema change required. Successful follow creation may create a notification
for the followed user.

### Like

No schema change required. Successful like creation may create a notification
for the liked post owner when actor and recipient differ.

### Session

No Round 2 schema change required. Existing persisted sessions remain the auth
source of truth.

## Relationships

- `UserAccount 1 -> many Post` as author
- `Post 1 -> many Post` via `ReplyToPostId` for replies
- `UserAccount 1 -> many Notification` as recipient
- `UserAccount 1 -> many Notification` as actor
- `UserAccount many -> many UserAccount` via `Follow`
- `UserAccount many -> many Post` via `Like`

## Validation Rules

### Profile Identity

- `DisplayName` is trimmed before validation and must be 1-50 characters after
  trimming.
- `Bio` may be blank and must be at most 160 characters.
- Avatar replacement succeeds only when Postly accepts the uploaded image as the
  user's current avatar.
- Failed validation or processing leaves the previously saved profile identity
  unchanged.

### Replies

- Reply bodies use the same body validation rules as posts.
- Reply creation fails if the target post is unavailable at commit time.
- Reply edits and deletes are author-only.
- Deleted replies project as unavailable placeholders in conversation reads.

### Notifications

- Only follow, like, and reply activity can create Round 2 notifications.
- Self-initiated actions do not create notifications for the same user.
- Destination state is evaluated when the notification-open flow runs.

### Continuation

- Timeline, profile posts, and conversation replies all sort newest-first using
  the existing stable cursor approach.
- `nextCursor = null` means the list is exhausted.
- Repeating a request with the same cursor must be safe after a transient
  failure.

## Read Models

### ProfileView

| Field | Type | Notes |
|-------|------|-------|
| `username` | `string` | Existing |
| `displayName` | `string` | Updated by US1 |
| `bio` | `string?` | Updated by US1 |
| `avatarUrl` | `string?` | Current custom avatar URL when available |
| `hasCustomAvatar` | `boolean` | True only when a custom avatar exists and is usable |
| `isSelf` | `boolean` | Existing route behavior |
| `isFollowedByViewer` | `boolean` | Existing |
| `followerCount` | `number` | Existing |
| `followingCount` | `number` | Existing |
| `posts` | `PostSummary[]` | Initial profile posts page |
| `nextCursor` | `string?` | Continuation cursor for profile posts |

### PostSummary

| Field | Type | Notes |
|-------|------|-------|
| `id` | `long` | Existing |
| `authorUsername` | `string?` | Null only if projection rules require it for unavailable items |
| `authorDisplayName` | `string?` | Identity projection for cross-surface consistency |
| `authorAvatarUrl` | `string?` | Avatar metadata with fallback behavior applied by the client |
| `body` | `string?` | Null or omitted for deleted placeholder content |
| `createdAtUtc` | `DateTimeOffset` | Existing |
| `editedAtUtc` | `DateTimeOffset?` | Existing |
| `isReply` | `boolean` | Derived from `ReplyToPostId` |
| `replyToPostId` | `long?` | Parent target when reply |
| `state` | `string` | `available` or `deleted` for Round 2 conversation rendering |
| `canEdit` | `boolean` | Viewer-specific |
| `canDelete` | `boolean` | Viewer-specific |

### ConversationView

| Field | Type | Notes |
|-------|------|-------|
| `target.state` | `string` | `available` or `unavailable` |
| `target.post` | `PostSummary?` | Present only when target is available |
| `replies` | `PostSummary[]` | Includes deleted placeholders when applicable |
| `nextCursor` | `string?` | Continuation cursor for more replies |

### NotificationSummary

| Field | Type | Notes |
|-------|------|-------|
| `id` | `long` | Notification ID |
| `kind` | `string` | `follow`, `like`, `reply` |
| `actorUsername` | `string` | Actor identity |
| `actorDisplayName` | `string` | Actor identity |
| `actorAvatarUrl` | `string?` | Actor avatar metadata |
| `createdAtUtc` | `DateTimeOffset` | Timestamp |
| `isRead` | `boolean` | Derived from `ReadAtUtc` |
| `destinationKind` | `string` | `profile`, `post`, or `conversation` |
| `destinationRoute` | `string` | Target route to open when available |
| `destinationState` | `string` | `available` or `unavailable` |

### NotificationOpenResult

| Field | Type | Notes |
|-------|------|-------|
| `notification` | `NotificationSummary` | Selected notification after read transition |
| `destination.route` | `string` | Route to navigate to |
| `destination.kind` | `string` | `profile`, `post`, `conversation`, or `notification-unavailable` |
| `destination.state` | `string` | `available` or `unavailable` |

## Indexing Strategy

- Keep the existing unique index on `UserAccount.NormalizedUsername`.
- Keep existing timeline/profile ordering indexes.
- Add index on `Post.ReplyToPostId, Post.CreatedAtUtc DESC, Post.Id DESC` for
  reply pagination.
- Add index on `Notification.RecipientUserId, Notification.CreatedAtUtc DESC, Notification.Id DESC`.
- Add supporting index on `Notification.RecipientUserId, Notification.ReadAtUtc`.

## State Transitions

### Profile Identity

`Saved -> Editing (UI only) -> Saved`

- Invalid save returns to `Editing` with errors and preserves draft input where
  possible.

### Reply

`Published -> Edited`
`Published/Edited -> DeletedPlaceholder`

- `DeletedPlaceholder` is non-interactive and carries no deleted body content.

### Notification

`Unread -> Read`

- Transition occurs only through notification-open behavior.

## Seed Data Expectations

- `bob`, `alice`, and `charlie` remain deterministic test users.
- One seeded conversation must have:
  - an available target
  - one Bob-authored reply
  - one non-Bob reply
  - more than one page of replies
- One seeded conversation route must resolve to an unavailable parent plus at
  least one still-visible reply.
- Bob must have:
  - at least one unread available-destination notification
  - at least one unread unavailable-destination notification
  - at least one home timeline and conversation identity surface that will
    reflect profile updates
