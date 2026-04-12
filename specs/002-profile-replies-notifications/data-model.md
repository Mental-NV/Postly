# Data Model: Postly Round 2

## Overview

Round 2 extends the MVP data model without replacing its core structure.
SQLite remains the system of record, EF Core remains the schema and migration
tool, and the smallest viable changes are applied to support profile editing,
avatar replacement, replies, notifications, and automatic continuation on
existing content collections.

## Entity Changes

### UserAccount

| Field | Type | Rules / Notes |
|-------|------|---------------|
| `Id` | `long` | Existing primary key |
| `Username` | `string` | Existing trimmed, user-facing value |
| `NormalizedUsername` | `string` | Existing case-insensitive unique lookup |
| `DisplayName` | `string` | Required; trimmed; 1-50 chars |
| `Bio` | `string?` | Optional; blank allowed; max 160 chars |
| `PasswordHash` | `string` | Existing ASP.NET password hasher output |
| `CreatedAtUtc` | `DateTimeOffset` | Existing immutable timestamp |
| `AvatarContentType` | `string?` | Present only when a custom avatar exists |
| `AvatarBytes` | `byte[]?` | Persisted custom avatar payload for current avatar only |
| `AvatarUpdatedAtUtc` | `DateTimeOffset?` | Last replacement timestamp for cache invalidation/projection freshness |

**Derived/read-model values**

- `HasCustomAvatar` derives from avatar persistence fields.
- `AvatarUrl` is a backend-owned read-model projection used by the frontend.
- `DefaultAvatar` remains the generated fallback derived from identity when no
  custom avatar exists.

### Session

No Round 2 schema changes are required. Existing persisted cookie-backed
sessions remain authoritative for authentication.

### Post

| Field | Type | Rules / Notes |
|-------|------|---------------|
| `Id` | `long` | Existing primary key |
| `AuthorId` | `long` | Existing FK to `UserAccount` |
| `Body` | `string` | Required for active posts/replies; 1-280 chars |
| `CreatedAtUtc` | `DateTimeOffset` | Existing publish timestamp |
| `EditedAtUtc` | `DateTimeOffset?` | Existing edit timestamp |
| `ReplyToPostId` | `long?` | Null for top-level posts; FK to parent target for replies |
| `DeletedAtUtc` | `DateTimeOffset?` | Null for active items; used for conversation-safe placeholder behavior |

**Derived/read-model values**

- `IsReply` is `ReplyToPostId != null`.
- `IsDeleted` is `DeletedAtUtc != null`.
- `ReplyCount` is projected for conversation-aware views where useful.
- `CanEdit`, `CanDelete`, and `CanReply` remain request-context values rather
  than stored columns.

### Follow

No schema changes are required, but successful follow creation now becomes a
notification trigger for the followed user.

### Like

No schema changes are required, but successful like creation now becomes a
notification trigger for the post author when business rules allow it.

### Notification

| Field | Type | Rules / Notes |
|-------|------|---------------|
| `Id` | `long` | Primary key |
| `RecipientUserId` | `long` | FK to the user receiving the notification |
| `ActorUserId` | `long` | FK to the user who caused the event |
| `Kind` | `string` | Enum-like value: `follow`, `like`, `reply` |
| `PostId` | `long?` | Optional FK to the related post or conversation target |
| `ReplyPostId` | `long?` | Optional FK to the created reply when kind is `reply` |
| `ProfileUserId` | `long?` | Optional FK for profile-oriented destinations such as follows |
| `CreatedAtUtc` | `DateTimeOffset` | Notification creation time |
| `ReadAtUtc` | `DateTimeOffset?` | Null while unread; set only after destination-open behavior marks it read |

**Derived/read-model values**

- `IsRead` is `ReadAtUtc != null`.
- `DestinationRoute` is projected from the stored target fields.
- `DestinationState` resolves as `available` or `unavailable` at read time.

## Relationships

- `UserAccount 1 -> many Session`
- `UserAccount 1 -> many Post` as author
- `Post 1 -> many Post` through `ReplyToPostId` for replies
- `UserAccount many -> many UserAccount` through `Follow`
- `UserAccount many -> many Post` through `Like`
- `UserAccount 1 -> many Notification` as recipient
- `UserAccount 1 -> many Notification` as actor
- `Post 1 -> many Notification` for post/reply-related destinations

## Validation Rules

### Profile Identity

- `DisplayName` is trimmed before validation and must contain between 1 and 50
  characters after trimming.
- `Bio` may be blank but must not exceed 160 characters.
- Avatar replacement is valid for Round 2 only when the supplied replacement is
  accepted as the user's current profile avatar and the previous avatar, if any,
  is no longer treated as current.
- Only the signed-in owner may mutate their profile identity or avatar.

### Post and Reply Rules

- Top-level posts still use the existing body validation rules.
- Replies use the same body validation rules as posts: trimmed body length must
  stay between 1 and 280 characters.
- Only the author may edit or delete their own reply.
- Deleting a reply does not remove its conversation slot; instead it transitions
  to placeholder state through `DeletedAtUtc`.
- Reply visibility follows the same viewer access rules as the parent product's
  post visibility model unless Round 2 explicitly changes it, which it does not.

### Notification Rules

- Notifications are created only for successful follow, like, and reply events.
- Viewing the notifications list alone does not change `ReadAtUtc`.
- `ReadAtUtc` is set only when the user opens the associated destination from
  the notification flow.
- Opening one notification must not change the read state of other notifications.

### Continuation Rules

- Timeline, profile, and conversation collections continue to use newest-first
  ordering with a stable tie-breaker.
- `NextCursor` remains opaque to the frontend.
- Continuation failures must be retry-safe; repeated requests with the same
  cursor must not corrupt ordering or duplicate persisted state.

## Indexing Strategy

- Keep the existing unique index on `UserAccount.NormalizedUsername`.
- Keep the existing post indexes for newest-first timeline/profile reads.
- Add index on `Post.ReplyToPostId, Post.CreatedAtUtc DESC, Post.Id DESC` for
  conversation reply pagination.
- Add index on `Post.DeletedAtUtc` only if query plans show it is needed for
  placeholder-heavy reads; do not add it speculatively.
- Add index on `Notification.RecipientUserId, Notification.CreatedAtUtc DESC, Notification.Id DESC`.
- Add supporting index on `Notification.RecipientUserId, Notification.ReadAtUtc`.
- Keep existing session, follow, and like indexes unchanged unless migration
  work reveals a concrete need.

## State Transitions

### Profile Identity Lifecycle

`Saved Identity -> Editing -> Saved Identity`

- `Editing` is UI-only until a valid save succeeds.
- Avatar replacement changes the current avatar in place; Round 2 does not keep
  version history beyond what normal database history or audit tooling might
  provide outside product scope.

### Reply Lifecycle

`Published Reply -> Edited Reply -> Edited Reply`
`Published Reply/Edited Reply -> Deleted Placeholder`

- `Deleted Placeholder` remains addressable in the conversation read model but
  is non-interactive and does not expose removed content.

### Notification Lifecycle

`Unread -> Read`

- `Unread`: `ReadAtUtc` is null
- `Read`: `ReadAtUtc` set after destination-open behavior succeeds

No archive, dismiss, or delete state is introduced in Round 2.

## Read Models

### PostSummary

Round 2 extends the shared post summary used by timeline, profile, and
conversation surfaces with:

- `authorAvatarUrl` or equivalent avatar metadata
- `isReply`
- `replyToPostId` when applicable
- `state` with at least `available` and `deleted` for conversation rendering

Existing author, timestamp, like, and ownership fields remain unchanged.

### ProfileView

- Existing profile identity plus:
  - avatar metadata for current profile avatar
  - unchanged follower/following counts
  - paginated post collection using current cursor semantics

### ConversationView

- `target` region with:
  - `state`: `available` or `unavailable`
  - `post`: nullable `PostSummary`
- `replies`: paginated reply collection
- `nextCursor`: opaque cursor for reply continuation

### NotificationSummary

- `id`
- `kind`
- `actor` identity summary
- `createdAtUtc`
- `isRead`
- `destinationKind`
- `destinationRoute`
- `destinationState`
- optional post/profile summary fields needed for user-facing copy

## DataSeed

## Purpose

Round 2 seed data extends the MVP deterministic dataset so Playwright and local
verification can cover profile edits, reply ownership, notification lifecycle,
and continuation states without test-only public endpoints.

## Seed Rules

- Existing seeded users `bob`, `alice`, and `charlie` remain deterministic.
- Seeded profile identity values must obey all normal validation rules.
- Seeded avatar state may include both a generated-fallback user and a
  custom-avatar user.
- Seeded replies must include at least:
  - one available conversation target
  - one Bob-authored reply
  - one reply authored by another user
  - enough replies to require at least one continuation request
- Seeded notifications for `bob` must include at least one unread notification
  for each supported kind: follow, like, and reply.

## Flow Coverage Matrix

| Flow / Scenario | Required Seed Preconditions |
|-----------------|-----------------------------|
| Profile edit success | `bob` owns `/u/bob`; at least one Bob-authored identity surface exists on home and conversation routes |
| Profile edit validation | `bob` has a persisted baseline identity that can be compared after failed save |
| Reply create/edit/delete | One available conversation target exists and `bob` can reply to it |
| Unavailable parent conversation | One seeded route resolves to an unavailable parent plus at least one still-visible reply |
| Notification read lifecycle | `bob` has unread notifications with both available and unavailable destinations |
| Continuation retry/end | Timeline, profile, and conversation collections each contain more than one page of deterministic results |
