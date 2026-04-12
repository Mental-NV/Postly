# Research: Postly Round 2

## Decision 1: Persist custom avatars in the existing SQLite database and deliver them through the backend

- **Decision**: Store the current custom avatar as profile-owned persisted data
  in the existing SQLite database and expose it through a backend-owned avatar
  endpoint or projected avatar URL, while preserving the current generated
  fallback avatar when no custom avatar exists.
- **Rationale**: Round 2 only needs avatar replacement for profile identity, not
  a general media platform. Keeping avatar persistence inside the existing EF
  Core + SQLite stack avoids new infrastructure, stays compatible with the
  backend-served SPA model, and keeps authorization and cache behavior local to
  the current application.
- **Alternatives considered**:
  - Filesystem storage under `wwwroot`: rejected because it complicates cleanup,
    deployment portability, and replacement semantics without providing a clear
    product win.
  - External object storage: rejected because it adds infrastructure and secret
    management far beyond Round 2 scope.

## Decision 2: Model replies as posts with a nullable parent reference and soft-delete support

- **Decision**: Extend the existing `Post` entity with a nullable parent-post
  reference for replies and add a soft-delete marker so deleted replies can
  remain as non-interactive placeholders in conversation views when the spec
  requires context to persist.
- **Rationale**: Replies share most behavior with existing posts: author
  ownership, timestamps, edit/delete permissions, identity rendering, and
  profile/timeline projection rules. Reusing the `Post` model keeps contracts
  and UI hooks consistent. Soft-delete support is the smallest viable way to
  satisfy the required deleted-reply and unavailable-parent conversation states
  without a separate placeholder table.
- **Alternatives considered**:
  - Separate `Reply` entity: rejected because it duplicates post behavior and
    would force avoidable divergence in contracts and authorization logic.
  - Hard-deleting replies as in the MVP: rejected because it cannot preserve the
    required conversation placeholders.

## Decision 3: Generate notifications synchronously inside existing follow, like, and reply write paths

- **Decision**: Add a first-class `Notification` entity and create notifications
  synchronously inside the existing request pipeline when follow, like, or
  reply mutations succeed. Track read state with `ReadAtUtc` and expose an
  explicit mark-read endpoint used after destination open.
- **Rationale**: The current app has no queue, worker, or outbox infrastructure,
  and Round 2 does not require one. Synchronous creation keeps behavior easy to
  reason about, deterministic in tests, and aligned with the same transaction
  boundaries already used for current mutations.
- **Alternatives considered**:
  - Background job or message bus delivery: rejected because it introduces
    unnecessary operational complexity.
  - Implicitly marking notifications read when the list is fetched: rejected
    because it conflicts with the clarified lifecycle in the spec.

## Decision 4: Reuse existing cursor semantics and add a shared frontend continuation contract

- **Decision**: Keep the existing cursor pattern based on newest-first
  `CreatedAtUtc` plus stable tie-breaker ordering for timeline and profile
  collections, and extend that same pattern to conversation replies. On the
  frontend, add one shared continuation contract for automatic loading,
  continuation failure, retry, and end-of-list behavior.
- **Rationale**: The backend already uses cursor semantics, and the user
  explicitly asked to reuse them where possible. Aligning conversation replies
  to the same pattern minimizes new concepts, and a shared frontend contract
  prevents timeline/profile/conversation from drifting into three different
  loading behaviors.
- **Alternatives considered**:
  - Offset pagination for replies or notifications: rejected because it adds a
    second pagination model and is less stable under concurrent writes.
  - Surface-specific continuation implementations: rejected because it increases
    UI inconsistency and test maintenance cost.
