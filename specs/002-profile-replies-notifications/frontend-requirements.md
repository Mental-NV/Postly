# Frontend Requirements: Postly Round 2

## Purpose

This document is a normative companion to `plan.md`. It defines the affected
screens, route ownership, required UI elements, state variants, shared layout
behavior, and stable `data-testid` hooks needed to implement and test Round 2.

## Route Ownership

| Screen ID | Route | Feature Owner | Purpose |
|-----------|-------|---------------|---------|
| `FE-07` | `/u/:username` | `frontend/src/features/profiles` | Preserve existing profile read mode and add own-profile edit plus profile-post continuation |
| `FE-08` | `/posts/:postId` | `frontend/src/features/posts` | Preserve baseline direct-post read route and extend it into a conversation-oriented surface with replies |
| `FE-09` | `/notifications` | `frontend/src/features/notifications` | Show notifications with unread/read state and destination-open behavior |
| `FE-10` | Shared behavior on `/`, `/u/:username`, `/posts/:postId` | `frontend/src/shared` + owning route feature | Provide a common continuation state contract |
| `FE-11` | Notification unavailable destination state | `frontend/src/features/notifications` | Show a truthful unavailable destination when a notification target no longer exists |

## Shared Layout Behavior

- Continue using the MVP shell and navigation patterns.
- Do not introduce a new settings route for profile editing; own-profile edit
  remains inline on `/u/:username`.
- Use the same post-card building blocks for replies where feasible instead of
  inventing reply-only primitives.
- Keep loading, empty, error, unavailable, retry, and end states visually and
  semantically distinct across all affected screens.
- Do not restyle or redesign baseline public profile/direct-post read mode as
  new Round 2 scope.

## Shared Stable Hooks

The same logical control MUST use the same test ID across all surfaces where it
appears.

### Existing MVP hooks that remain authoritative

- `post-card-<postId>`
- `author-link-<username>`
- `post-body-<postId>`
- `post-timestamp-<postId>`
- `post-permalink-<postId>`
- `post-like-button-<postId>`
- `post-like-count-<postId>`
- `post-edit-button-<postId>`
- `post-delete-button-<postId>`
- `post-editor-body-input-<postId>`
- `post-editor-save-button-<postId>`
- `post-editor-cancel-button-<postId>`

### New shared Round 2 hooks

- `post-avatar-<postId>`
- `collection-continuation-sentinel`
- `collection-continuation-loading`
- `collection-continuation-error`
- `collection-continuation-retry`
- `collection-end-state`

## FE-07 Profile Screen

**Route**: `/u/:username`  
**Owner**: `frontend/src/features/profiles`

### Required UI elements

- `profile-page`
- `profile-heading`
- `profile-display-name`
- `profile-bio`
- `profile-avatar-wrapper`
- `profile-avatar-image`
- `profile-avatar-fallback`
- `profile-posts`
- `profile-edit-button` only when `isSelf`
- `profile-edit-form` only in edit state
- `profile-display-name-input`
- `profile-bio-input`
- `profile-bio-counter`
- `profile-avatar-input`
- `profile-avatar-edit-overlay`
- `profile-save-button`
- `profile-cancel-button`
- `profile-form-status`

### State variants

- Public/read mode baseline state
- Own-profile read state
- Own-profile edit state
- Inline validation error state
- Pending save state
- Avatar fallback state
- Route-level loading state
- Route-level error/unavailable state inherited from existing behavior
- Profile-post continuation loading state
- Profile-post continuation failure with retry
- Profile-post explicit end-of-list state

### Behavioral requirements

- Only `isSelf` profiles render edit controls.
- Display name and bio inputs retain local draft values after failed save where
  possible.
- Save success updates the currently rendered profile header immediately.
- Updated identity must propagate to already visible in-scope timeline and
  conversation surfaces via shared query invalidation or equivalent refresh
  behavior.
- `profile-avatar-input` accepts only still JPEG/PNG file selection in Round 2.
- Round 2 does not provide manual cropping UI; avatar normalization remains a
  backend responsibility.
- Avatar rendering must never show a broken image; use fallback when no usable
  custom avatar is available.
- Successful avatar replacement must adopt the returned versioned `avatarUrl`
  so refreshed identity surfaces bypass stale cached images.

## FE-08 Conversation / Direct-Post Screen

**Route**: `/posts/:postId`  
**Owner**: `frontend/src/features/posts`

### Required UI elements

- `conversation-page`
- `conversation-target`
- `conversation-target-unavailable`
- `conversation-replies`
- `conversation-status`
- `reply-composer`
- `reply-composer-input`
- `reply-submit-button`
- `reply-form-status`
- `deleted-reply-placeholder-<replyId>`
- `reply-composer-unavailable` when composer actions are blocked

### State variants

- Target available with replies
- Target available with no replies yet
- Target unavailable with still-visible replies
- Reply submit pending
- Reply validation failure
- Reply target unavailable during submission
- Reply inline edit state using existing post editor controls
- Deleted-reply placeholder state
- Reply continuation loading/failure/retry/end states

### Behavioral requirements

- Conversation target and replies render as separate regions on the same route.
- The route remains readable while signed out; reply creation and author-only
  edit/delete controls appear only when authentication and ownership allow
  them.
- Reply cards reuse the existing post-card control IDs for edit/delete/edit-save
  where those controls are applicable.
- Non-authored replies do not render edit/delete controls.
- Deleted replies render as placeholders with no interactive controls and no
  deleted body text.
- If the parent post is unavailable, the route remains accessible and the
  placeholder target is shown above the visible replies.

## FE-09 Notifications Screen

**Route**: `/notifications`  
**Owner**: `frontend/src/features/notifications`

### Required UI elements

- `notifications-page`
- `notifications-heading`
- `notifications-list`
- `notification-item-<notificationId>`
- `notification-unread-indicator-<notificationId>`
- `notification-read-indicator-<notificationId>`
- `notifications-empty-state`
- `notifications-status`

### State variants

- Initial loading
- Populated list with mixed unread/read rows
- Empty state
- Open-destination pending state for the selected row
- Route-level error state if the list fetch fails

### Behavioral requirements

- Fetching the list must not change any row from unread to read.
- Selecting one row initiates the notification-open flow for that row only.
- The selected row becomes read after successful destination-open handling.
- Unselected unread rows remain unread.
- Notification items remain deterministic and stable under back/forward
  navigation.

## FE-11 Notification Unavailable Destination

**Route**: Implementation may use a dedicated route such as
`/notifications/:notificationId/unavailable` or a route-state driven
destination, but the result must be a destination screen, not a toast-only
failure.

### Required UI elements

- `notification-unavailable-destination`
- `notification-unavailable-message`
- `notification-unavailable-back-link`

### Behavioral requirements

- The destination must be clearly tied to the selected notification, not a
  generic app error.
- It must remain distinct from profile/post route-level 404 handling so the
  notification flow is truthful and reviewable.

## FE-10 Shared Continuation Contract

**Applies to**: `/`, `/u/:username`, `/posts/:postId`

### Required UI elements

- `collection-continuation-sentinel`
- `collection-continuation-loading`
- `collection-continuation-error`
- `collection-continuation-retry`
- `collection-end-state`

### State contract

- `idle`: initial page rendered, no continuation currently in progress
- `loading-more`: sentinel reached and next page requested
- `load-more-error`: continuation failed, visible items retained, retry shown
- `exhausted`: no more items remain, end state shown

### Behavioral requirements

- The continuation point is the last currently visible item on the surface.
- Reaching the sentinel triggers the next page automatically.
- A continuation failure preserves all previously visible items.
- Retry uses the same pending cursor/context and appends results on success.
- The end state remains visible after exhaustion and suppresses additional
  continuation attempts.
- Initial empty state must remain visually and semantically distinct from the
  exhausted end state.

## Stable Hook Matrix

| Surface | Required element | Stable selector / test id | Verification purpose |
|---------|------------------|---------------------------|----------------------|
| `/u/:username` | Profile edit trigger | `profile-edit-button` | Enter own-profile edit state |
| `/u/:username` | Display name input | `profile-display-name-input` | Validate/edit name |
| `/u/:username` | Bio input | `profile-bio-input` | Validate/edit bio |
| `/u/:username` | Avatar upload input | `profile-avatar-input` | Replace avatar |
| `/u/:username` | Save status area | `profile-form-status` | Assert validation/success feedback |
| `/posts/:postId` | Reply input | `reply-composer-input` | Create reply |
| `/posts/:postId` | Reply submit | `reply-submit-button` | Submit reply |
| `/posts/:postId` | Deleted reply placeholder | `deleted-reply-placeholder-<replyId>` | Assert placeholder retention |
| `/posts/:postId` | Parent unavailable target | `conversation-target-unavailable` | Assert unavailable-parent state |
| `/notifications` | Notification row | `notification-item-<notificationId>` | Open a single notification |
| `/notifications` | Unread row indicator | `notification-unread-indicator-<notificationId>` | Assert unread state |
| `/notifications` | Read row indicator | `notification-read-indicator-<notificationId>` | Assert read state |
| Shared collections | Continuation sentinel | `collection-continuation-sentinel` | Trigger automatic continuation |
| Shared collections | Continuation retry | `collection-continuation-retry` | Recover from continuation failure |
| Shared collections | End state | `collection-end-state` | Assert exhaustion is explicit |

## Fixture Requirements for Playwright

- Seed users `bob`, `alice`, and `charlie` with deterministic credentials.
- Seed a Bob-visible home timeline item and conversation item that display Bob's
  identity.
- Seed one unavailable-parent conversation plus one available conversation with
  multiple reply pages.
- Seed unread notifications for both available and unavailable destinations.
- Seed continuation data and fault-injection support sufficient to reach retry
  and end states without adding public test-only product endpoints.
