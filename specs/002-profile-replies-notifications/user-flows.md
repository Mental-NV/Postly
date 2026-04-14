# User Flows: Postly Round 2

## Purpose

This document translates the approved Round 2 stories into deterministic,
browser-automation-friendly flows. Each flow names the route transitions,
required UI elements, visible states, and the verification outcome that
Playwright should prove.

## Conventions

- `UI element` values reference required controls from
  [frontend-requirements.md](./frontend-requirements.md).
- Existing Round 1 post-card selectors remain authoritative on conversation
  surfaces where replies are still rendered as posts.
- Route transitions are explicit even when the route does not change so
  continuation and mutation state changes remain reviewable.

## Deterministic Test Preconditions

- `bob` is the default signed-in actor unless a flow says otherwise.
- `alice` exists as another visible user whose profile and posts remain
  reachable in the seeded dataset.
- `charlie` exists as an additional seeded user so notification, reply, and
  continuation flows can cover multi-user state transitions without creating
  every dependency through the UI.
- The non-production seed includes:
  - at least one conversation target with visible replies
  - at least one reply authored by `bob`
  - at least one unread follow, like, or reply notification for `bob`
  - enough timeline, profile, and conversation data to trigger continuation
    behavior beyond the first page
- Protected flows may establish the signed-in state through UI sign-in or a
  storage-state helper backed by the same deterministic seed data.

## Flow Index

| Flow ID | Story | Outcome |
|---------|-------|---------|
| `UF-01` | US1 | Signed-in user edits display name, bio, and avatar successfully |
| `UF-02` | US1 | Invalid profile edits are rejected without changing visible identity |
| `UF-03` | US2 | Signed-in user replies to a post and sees the reply in conversation |
| `UF-04` | US2 | Reply author edits and deletes their own reply, leaving a placeholder |
| `UF-05` | US2 | Conversation remains open when parent post is unavailable |
| `UF-06` | US3 | Opening an available notification destination marks only that notification read |
| `UF-07` | US3 | Viewing notifications without opening a destination leaves read state unchanged |
| `UF-08` | US4 | Home timeline loads more items automatically at the continuation point |
| `UF-09` | US4 | Profile post list retries after continuation failure without losing visible items |
| `UF-10` | US4 | Conversation replies reach an explicit end-of-list state |

## Detailed Flows

### UF-01 Successful Profile Edit

**Start route**: `/u/bob`
**Preconditions**: Signed in as `bob`

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `profile-page` | Observe own profile | Own profile identity is visible in read state |
| 2 | `profile-edit-button` | Click edit | Profile enters editable state on the same route |
| 3 | `profile-display-name-input` | Enter a valid updated display name | Updated value is visible in the input |
| 4 | `profile-bio-input` | Enter a valid updated bio | Updated value is visible in the input |
| 4a | `profile-bio-counter` | Observe character count | Count reflects the current bio length (e.g. "50/160") |
| 5 | `profile-avatar-edit-overlay` | Click the edit overlay | File picker opens |
| 5a | `profile-avatar-input` | Choose a valid replacement avatar | Selected file is accepted by the form |
| 6 | `profile-save-button` | Click save | Save enters pending state and duplicate submits are prevented |
| 7 | `profile-display-name` | Observe profile header | Updated display name is visible on `/u/bob` |
| 8 | `nav-home-link` | Navigate to home | Route changes to `/` |
| 9 | `post-avatar-<postId>` or `author-link-bob` | Observe Bob-authored identity surfaces | Updated identity is visible on the home timeline |
| 10 | `post-permalink-<postId>` | Open a Bob-authored conversation | Route changes to `/posts/:postId` |
| 11 | `conversation-page` | Observe author identity | Updated identity is visible on conversation surfaces where Bob appears |

### UF-02 Invalid Profile Edit

**Start route**: `/u/bob`
**Preconditions**: Signed in as `bob`

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `profile-edit-button` | Click edit | Edit state opens on the same route |
| 2 | `profile-display-name-input` | Enter an invalid display name | Input reflects the invalid draft |
| 3 | `profile-bio-input` | Enter an invalid bio value | Input reflects the invalid draft |
| 4 | `profile-save-button` | Click save | Save is rejected and the route remains `/u/bob` |
| 5 | `profile-form-status` | Observe error state | Validation feedback is visible and reviewable |
| 6 | `profile-display-name` | Exit or refresh form state | Previously saved identity remains unchanged |

### UF-03 Create Reply in Conversation

**Start route**: `/posts/:postId`
**Preconditions**: Signed in as `bob`; seeded target post is available

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `conversation-target` | Observe target post | Target post is visible |
| 2 | `reply-composer-input` | Enter valid reply text | Draft text is visible |
| 3 | `reply-submit-button` | Click submit | Composer enters pending state |
| 4 | `conversation-replies` | Observe reply list | New reply appears in the conversation replies collection |
| 4a | `conversation-thread-line` | Observe threading | A vertical thread line connects the parent avatar to the new reply avatar |
| 5 | `post-body-<postId>` | Observe new reply body | Saved reply text is visible in its reply card |

### UF-04 Edit and Delete Own Reply

**Start route**: `/posts/:postId`
**Preconditions**: Signed in as `bob`; Bob has a visible reply in this conversation

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `post-edit-button-<replyId>` | Click edit on Bob's reply | Reply enters inline edit state |
| 2 | `post-editor-body-input-<replyId>` | Replace text with valid edited reply text | Updated draft is visible |
| 3 | `post-editor-save-button-<replyId>` | Click save | Save enters pending state |
| 4 | `post-edited-badge-<replyId>` | Observe saved reply | Edited indicator becomes visible |
| 5 | `post-delete-button-<replyId>` | Click delete | Delete confirmation appears |
| 6 | `confirm-dialog-confirm` | Confirm delete | Delete enters pending state |
| 7 | `deleted-reply-placeholder-<replyId>` | Observe conversation | Deleted reply remains as a non-interactive placeholder |

### UF-05 Unavailable Parent Conversation

**Start route**: `/posts/:postId`
**Preconditions**: Signed in as `bob`; seeded route resolves to an unavailable parent with at least one visible reply

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `conversation-target-unavailable` | Observe target region | Route stays open and shows the unavailable-parent placeholder |
| 2 | `conversation-replies` | Observe reply list | Any still-visible replies remain visible below the placeholder |
| 3 | `reply-composer` | Observe availability | Reply composer is hidden or unavailable according to target availability rules |

### UF-06 Notification Opens Available Destination

**Start route**: `/notifications`
**Preconditions**: Signed in as `bob`; at least one unread notification has an available destination

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 0 | `nav-notifications-badge` | Observe navigation | Unread badge shows a count (e.g. "1") |
| 1 | `notifications-list` | Observe list | At least one unread notification is visible |
| 2 | `notification-unread-indicator-<notificationId>` | Observe unread state | Notification has a light-blue background and blue accent bar |
| 3 | `notification-item-<notificationId>` | Open the notification destination | App navigates to the route associated with the notification |
| 4 | Destination surface | Observe route content | The relevant profile or conversation route loads successfully |
| 5 | `nav-notifications-link` | Return to notifications | Route changes back to `/notifications` |
| 6 | `notification-read-indicator-<notificationId>` | Observe updated state | The opened notification background is now neutral |
| 7 | `nav-notifications-badge` | Observe navigation | Unread badge count has decreased or is hidden if no more unread items remain |

### UF-07 Notifications List-Only View

**Start route**: `/notifications`
**Preconditions**: Signed in as `bob`; at least one unread notification exists

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `notifications-list` | Observe list | Notifications route loads successfully |
| 2 | `notification-unread-indicator-<notificationId>` | Observe one unread item | Unread state is visible |
| 3 | `notifications-heading` | Remain on the list without opening a destination | No destination route is opened |
| 4 | `notification-unread-indicator-<notificationId>` | Re-check the same item | The notification remains unread |

### UF-08 Home Timeline Automatic Continuation

**Start route**: `/`
**Preconditions**: Signed in as `bob`; home timeline has more than one page of results

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `timeline-feed` | Observe initial page | First page of posts is visible |
| 2 | `collection-continuation-sentinel` | Scroll until the last currently visible item is the continuation point | Automatic continuation begins without a full route reload |
| 3 | `collection-continuation-loading` | Observe loading state | Continuation loading is explicit and visible |
| 4 | `timeline-feed` | Observe appended items | Additional posts appear below the initial page without removing visible content |

### UF-09 Profile Continuation Retry

**Start route**: `/u/alice`
**Preconditions**: Signed in as `bob`; profile has additional pages; test harness can force one continuation failure

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `profile-posts` | Observe initial posts | Initial page is visible |
| 2 | `collection-continuation-sentinel` | Scroll to continuation point | Automatic continuation attempt begins |
| 3 | `collection-continuation-error` | Observe failure state | Existing visible posts remain on screen and retry is offered |
| 4 | `collection-continuation-retry` | Click retry | Continuation re-runs from the same route |
| 5 | `profile-posts` | Observe collection | Additional posts append after retry succeeds |

### UF-10 Conversation End of List

**Start route**: `/posts/:postId`
**Preconditions**: Signed in as `bob`; conversation has multiple reply pages and a deterministic final page

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `conversation-replies` | Observe initial replies | First reply page is visible |
| 2 | `collection-continuation-sentinel` | Continue scrolling until no more replies remain | Additional replies load as needed |
| 3 | `collection-end-state` | Observe final collection state | Explicit end-of-list state is visible and no further continuation occurs |
