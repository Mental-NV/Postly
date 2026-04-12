# Frontend Requirements: Postly Round 2

## Purpose

This document defines the required Round 2 screens, route ownership, state
variants, and stable automation hooks needed to implement and test the feature
within the current React + TypeScript application.

This document is normative for:

- route ownership and affected screen inventory
- required UI elements and shared layout behavior
- loading, validation, empty, error, retry, unavailable, and end states
- stable `data-testid` hooks used by Playwright

## Screen Inventory

| Screen ID | Route | Access | Purpose |
|-----------|-------|--------|---------|
| `FE-07` | `/u/:username` | Protected with current public-read behavior preserved | Show profile details, own-profile edit state, and profile post collection |
| `FE-08` | `/posts/:postId` | Protected with current public-read behavior preserved | Show a conversation-oriented direct-post view with target post, replies, and placeholder states |
| `FE-09` | `/notifications` | Protected | Show follow, like, and reply notifications with read/unread state and destination navigation |
| `FE-10` | Shared collection state on `/`, `/u/:username`, `/posts/:postId` | Protected | Provide a consistent automatic continuation contract for timeline, profile, and conversation collections |

## Shared UI Contract

### Shared layout regions

All affected Round 2 screens MUST continue using these shared regions where
applicable:

- `page-shell`
- `page-heading`
- `page-status`
- `primary-action-region`
- `content-region`

### Shared automation hook rules

- Critical interactive elements MUST have stable `data-testid` values.
- The same logical control MUST use the same test ID across all surfaces where
  it appears.
- Existing Round 1 post-card hooks remain authoritative on reply cards:
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
- Round 2 adds the following shared hooks:
  - `post-avatar-<postId>`
  - `collection-continuation-sentinel`
  - `collection-continuation-loading`
  - `collection-continuation-error`
  - `collection-continuation-retry`
  - `collection-end-state`

### Deterministic e2e fixture contract

Non-production end-to-end environments MUST provide deterministic fixture data
or equivalent setup so all Round 2 flows are repeatable.

The fixture contract MUST guarantee:

- `bob`, `alice`, and `charlie` can sign in with known test credentials
- `bob` owns a reachable profile at `/u/bob`
- `alice` has at least one visible post and at least one reachable conversation
- at least one seeded conversation contains replies across more than one page
- at least one unread notification for `bob` exists at startup
- continuation scenarios can reach both retry and end-of-list states without
  adding test-only public product endpoints

## Screen Specifications

### FE-07 Profile Screen with Own-Edit State

**Route**: `/u/:username`  
**Access**: Protected, while preserving current public-read behavior  
**Route ownership**: `frontend/src/features/profiles`

**Primary goals**

- Show profile identity and post list as in the MVP
- Allow the signed-in owner to edit display name, bio, and avatar
- Reflect updated identity consistently across other surfaces that render that
  user

**Required UI elements**

- `profile-page`
- `profile-heading`
- `profile-display-name`
- `profile-bio`
- `profile-avatar-image` or `profile-avatar-fallback`
- `profile-posts`
- `profile-edit-button` when `isSelf`
- `profile-edit-form` in edit state
- `profile-display-name-input`
- `profile-bio-input`
- `profile-avatar-input`
- `profile-save-button`
- `profile-cancel-button`
- `profile-form-status`

**State variants**

- Read-only own profile state
- Read-only other-user profile state
- Edit form default state
- Edit validation error state
- Edit pending-save state
- Avatar replacement success state
- Route-level loading, empty, error, and retry states for profile data
- Collection continuation loading, failure, retry, and end states for profile posts

**Required behaviors**

- `profile-edit-button` appears only for the signed-in owner.
- Edit mode remains on the same profile route rather than navigating to a
  separate settings route.
- Invalid display name, bio, or avatar replacement must not change the visible
  saved identity.
- Successful saves must refresh identity on the current screen and any later
  visited home timeline or conversation surface that renders the same user.

### FE-08 Conversation Screen

**Route**: `/posts/:postId`  
**Access**: Protected, while preserving current public-read behavior  
**Route ownership**: `frontend/src/features/posts`

**Primary goals**

- Present the direct-post route as a conversation view
- Show the target post or an unavailable-parent placeholder
- Allow reply creation and author-only reply edits/deletions

**Required UI elements**

- `conversation-page`
- `conversation-heading`
- `conversation-target` or `conversation-target-unavailable`
- `conversation-replies`
- `reply-composer`
- `reply-composer-input`
- `reply-submit-button`
- `reply-form-status`
- `deleted-reply-placeholder-<postId>` when a reply has been deleted but must
  remain represented in the thread

**State variants**

- Available conversation target state
- Unavailable parent placeholder state
- Empty-replies state
- Reply composer validation and pending states
- Reply edit state using existing post editor hooks
- Deleted reply placeholder state
- Collection continuation loading, failure, retry, and end states for replies

**Required behaviors**

- Reply cards reuse the shared post-card contract wherever the logical control
  is the same.
- Non-authors must not see `post-edit-button-<postId>` or
  `post-delete-button-<postId>` on replies they do not own.
- Deleted replies remain visible only as non-interactive placeholders.
- The route remains `/posts/:postId` even when the parent target is unavailable.

### FE-09 Notifications Screen

**Route**: `/notifications`  
**Access**: Protected  
**Route ownership**: `frontend/src/features/notifications`

**Primary goals**

- Show follow, like, and reply notifications
- Make unread and read state visible
- Allow the user to navigate to the relevant destination

**Required UI elements**

- `notifications-page`
- `notifications-heading`
- `notifications-list`
- `notification-item-<notificationId>`
- `notification-unread-indicator-<notificationId>`
- `notification-read-indicator-<notificationId>`
- `notifications-empty-state`
- `notifications-error-state`
- `notifications-retry-button`
- `nav-notifications-link`

**State variants**

- Loading state
- Populated list with mixed read/unread items
- Empty state
- Route-level error with retry
- Destination-open success state
- Destination-unavailable state after selection

**Required behaviors**

- Merely viewing `/notifications` must not mark notifications as read.
- Opening a notification destination must mark only that notification as read.
- If the target later becomes unavailable, the user still reaches a clear
  destination state associated with that notification.

### FE-10 Shared Automatic Continuation Contract

**Routes**: `/`, `/u/:username`, `/posts/:postId`  
**Access**: Protected  
**Route ownership**: shared between the owning feature route and `frontend/src/shared`

**Primary goals**

- Start continuation automatically when the last currently visible item becomes
  the continuation point for that surface
- Keep loading, error, retry, and end states explicit and consistent
- Preserve already visible content when continuation fails

**Required UI elements**

- `collection-continuation-sentinel`
- `collection-continuation-loading`
- `collection-continuation-error`
- `collection-continuation-retry`
- `collection-end-state`

**State variants**

- Initial page only
- Automatic continuation in progress
- Continuation failure with retry
- End-of-list state

**Required behaviors**

- Home timeline continuation point: the last currently visible item in the home
  timeline feed.
- Profile continuation point: the last currently visible post in the profile
  post list.
- Conversation continuation point: the last currently visible item in the
  conversation view reply collection.
- Continuation failure must keep already loaded items visible.
- End-of-list state must be explicit when no additional content remains.
