# Research: Postly Round 2

## Decision 1: Persist the current avatar on `UserAccount` and serve it through backend-owned projection

- **Decision**: Store only the current custom avatar on `UserAccount` and
  expose it via backend-owned avatar metadata plus a current-avatar endpoint.
- **Rationale**: Round 2 needs avatar replacement, fallback, and cross-surface
  identity reflection, not a general media system. Keeping the current avatar in
  the existing SQLite + EF Core model avoids new storage infrastructure and
  keeps authorization local to the existing profile feature.
- **Alternatives considered**:
  - Files under `wwwroot`: rejected because cleanup, replacement, and publish
    portability become harder without solving a product requirement.
  - External blob/object storage: rejected as unnecessary infrastructure for
    this scope.

## Decision 2: Reuse `Post` for replies and soft-delete replies to preserve conversation placeholders

- **Decision**: Model replies as normal posts with nullable `ReplyToPostId`,
  and represent deleted replies with `DeletedAtUtc` so the conversation can keep
  a non-interactive placeholder at the same position.
- **Rationale**: Replies share author ownership, timestamps, edit/delete rules,
  and most projection requirements with existing posts. Reusing `Post` keeps the
  smallest possible domain model while meeting the clarified placeholder
  semantics.
- **Alternatives considered**:
  - Separate `Reply` table: rejected because it duplicates existing post
    behavior and increases contract drift.
  - Hard delete: rejected because it cannot preserve the required placeholder.

## Decision 3: Resolve notification destination and selected-item read transition through one notification-open endpoint

- **Decision**: Add a `POST /api/notifications/{notificationId}/open` endpoint
  that resolves the destination for the selected notification and marks only
  that notification as read as part of the open action.
- **Rationale**: The clarified spec ties the read transition to opening the
  destination, not to viewing the list. A dedicated open endpoint keeps that
  lifecycle explicit, supports both available and unavailable destinations, and
  avoids race-prone client sequences such as “navigate first, then separately
  mark read”.
- **Alternatives considered**:
  - Mark-read-on-list-fetch: rejected because it violates the clarified spec.
  - Separate resolve and mark-read endpoints: rejected because it makes the
    lifecycle easier to desynchronize and harder to test deterministically.

## Decision 4: Keep synchronous notification creation inside successful follow/like/reply writes

- **Decision**: Create notifications synchronously within the existing follow,
  like, and reply mutation handlers when the actor and recipient differ.
- **Rationale**: The current app has no queue/outbox infrastructure and Round 2
  does not need one. Synchronous creation keeps tests deterministic and keeps
  notification persistence in the same transactional boundary as the triggering
  action.
- **Alternatives considered**:
  - Background processing or outbox pattern: rejected as unnecessary operational
    complexity for this scope.

## Decision 5: Reuse the existing newest-first opaque cursor model for all continuation surfaces

- **Decision**: Preserve the existing cursor semantics for timeline and profile
  collections and extend the same opaque newest-first cursor pattern to
  conversation replies.
- **Rationale**: The user explicitly asked to reuse existing cursor semantics
  where possible. One pagination model across all long-list surfaces reduces
  backend/frontend branching and keeps continuation logic reviewable.
- **Alternatives considered**:
  - Offset pagination for replies: rejected because it introduces a second
    paging model and is more fragile under concurrent writes.
  - Surface-specific continuation contracts: rejected because it would create
    avoidable UI inconsistency and extra Playwright complexity.

## Decision 6: Keep notification unavailable destinations explicit instead of falling back to generic 404 handling

- **Decision**: When a notification target becomes unavailable, route the user
  to a destination state explicitly tied to the selected notification rather
  than silently redirecting to a generic error page or a stale route.
- **Rationale**: The clarified spec requires a clear not-available destination
  for that same notification. Making the state notification-specific prevents
  ambiguity about whether the app failed or the target genuinely no longer
  exists.
- **Alternatives considered**:
  - Redirect to home or notifications list: rejected because it obscures the
    destination outcome.
  - Reuse generic route-level 404 only: rejected because it makes notification
    outcome less explicit and harder to test.
