# User Flows: Postly Round 2

## Purpose

This document is a normative companion to `plan.md`. It translates each
approved Round 2 user story into deterministic, browser-automation-friendly
flows with explicit routes, visible states, required UI elements, and
verification intent.

## Flow Conventions

- Route transitions are listed even when the path does not change so
  mutation-state and continuation-state changes remain testable.
- Existing MVP read routes remain baseline behavior; these flows cover only the
  new Round 2 behaviors layered onto them.
- Repeated controls keep the same logical `data-testid` across surfaces.
- Seeded users:
  - `bob`: default signed-in actor
  - `alice`: visible user with reachable profile/posts
  - `charlie`: additional actor for notification/reply scenarios

## Flow Index

| Flow ID | Story | Type | Outcome |
|---------|-------|------|---------|
| `UF-01` | US1 | Primary | Successful profile edit updates identity on all in-scope surfaces |
| `UF-02` | US1 | Recovery | Invalid profile edits preserve the saved identity and draft inputs |
| `UF-03` | US1 | Recovery | Missing custom avatar falls back to the generated default avatar |
| `UF-04` | US2 | Primary | Signed-in user creates a reply from the conversation route |
| `UF-05` | US2 | Primary | Reply author edits and deletes their own reply, leaving a placeholder |
| `UF-06` | US2 | Recovery | Conversation stays open when the parent post is unavailable |
| `UF-07` | US2 | Recovery | Non-authors are not offered edit or delete actions for replies |
| `UF-08` | US3 | Primary | Opening an available notification destination marks only that notification read |
| `UF-09` | US3 | Recovery | Opening an unavailable notification still lands on a truthful unavailable destination and marks only that notification read |
| `UF-10` | US3 | Recovery | Viewing the notifications list alone does not change unread state |
| `UF-11` | US4 | Primary | Home timeline automatically loads additional items at the continuation point |
| `UF-12` | US4 | Recovery | Profile continuation failure preserves visible items and recovers through retry |
| `UF-13` | US4 | Primary | Conversation continuation reaches an explicit end-of-list state |

## Deterministic Preconditions

- Non-production seeded data includes:
  - a Bob-owned profile at `/u/bob`
  - at least one Bob-authored post visible on `/`
  - at least one conversation under `/posts/:postId` where Bob identity is
    visible and replies span more than one page
  - at least one Bob-authored reply
  - at least one reply authored by another user in the same conversation
  - at least one notification with an available destination
  - at least one notification with an unavailable destination
  - enough data on timeline, profile, and conversation surfaces to require at
    least one continuation request
- Protected flows may use UI sign-in or seeded Playwright storage state as long
  as they hit the same backend-hosted app.

## Detailed Flows

### UF-01 Successful Profile Edit

**Story**: US1  
**Start route**: `/u/bob`  
**Trigger**: Bob enters edit mode on his own profile  
**Route transitions**:

- `/u/bob` read state -> `/u/bob` edit state
- `/u/bob` -> `/`
- `/` -> `/posts/:postId`

**Visible states**:

- profile read state
- profile edit state
- pending save state
- save success state
- updated identity on profile/timeline/conversation surfaces

**Required UI elements**:

- `profile-page`
- `profile-edit-button`
- `profile-display-name-input`
- `profile-bio-input`
- `profile-avatar-input`
- `profile-save-button`
- `profile-display-name`
- `timeline-feed`
- `conversation-page`

**Verification intent**:

- Prove a valid display name, bio, and avatar save succeeds.
- Prove the updated identity appears on:
  - Bob's own profile
  - places on `/` that already show Bob's identity
  - direct-post/conversation surfaces where Bob's identity is shown

### UF-02 Invalid Profile Edit Rejection

**Story**: US1  
**Start route**: `/u/bob`  
**Trigger**: Bob submits an invalid display name, bio, or avatar  
**Route transitions**:

- `/u/bob` read state -> `/u/bob` edit state
- `/u/bob` edit state -> `/u/bob` edit state with validation errors

**Visible states**:

- edit state
- inline validation error state
- preserved draft state
- unchanged saved identity state

**Required UI elements**:

- `profile-display-name-input`
- `profile-bio-input`
- `profile-save-button`
- `profile-form-status`
- `profile-display-name`
- `profile-avatar-image` or `profile-avatar-fallback`

**Verification intent**:

- Prove invalid saves do not mutate the persisted profile.
- Prove the user gets clear validation feedback.
- Prove in-progress edits remain available where possible after failure.

### UF-03 Avatar Fallback After Missing Custom Avatar

**Story**: US1  
**Start route**: `/u/alice` or `/u/bob` depending on seeded fallback case  
**Trigger**: The viewed user has no custom avatar or the custom avatar is
unavailable  
**Route transitions**:

- `/u/:username` -> `/`
- `/` -> `/posts/:postId`

**Visible states**:

- fallback avatar on profile
- fallback avatar on timeline identity surface
- fallback avatar on conversation identity surface

**Required UI elements**:

- `profile-avatar-fallback`
- `post-avatar-<postId>`
- `conversation-page`

**Verification intent**:

- Prove the same generated default avatar is used instead of a broken image on
  all in-scope identity surfaces.

### UF-04 Create Reply in Conversation

**Story**: US2  
**Start route**: `/posts/:postId`  
**Trigger**: Bob submits a valid reply on an available conversation target  
**Route transitions**:

- `/posts/:postId` read state -> `/posts/:postId` reply pending state
- `/posts/:postId` reply pending state -> `/posts/:postId` updated conversation

**Visible states**:

- conversation target available
- reply composer idle
- reply submit pending
- reply saved in conversation list

**Required UI elements**:

- `conversation-page`
- `conversation-target`
- `reply-composer`
- `reply-composer-input`
- `reply-submit-button`
- `conversation-replies`

**Verification intent**:

- Prove a valid reply is created from the direct-post route and appears in the
  visible conversation with author identity and timestamp.

### UF-05 Edit and Delete Own Reply

**Story**: US2  
**Start route**: `/posts/:postId`  
**Trigger**: Bob edits and then deletes his own reply  
**Route transitions**:

- `/posts/:postId` reply read state -> inline edit state
- `/posts/:postId` inline edit state -> saved edited state
- `/posts/:postId` saved edited state -> delete confirmation -> placeholder
  state

**Visible states**:

- reply inline edit
- reply pending save
- edited reply state
- delete confirmation state
- deleted placeholder state

**Required UI elements**:

- `post-edit-button-<replyId>`
- `post-editor-body-input-<replyId>`
- `post-editor-save-button-<replyId>`
- `post-delete-button-<replyId>`
- `confirm-dialog-confirm`
- `deleted-reply-placeholder-<replyId>`

**Verification intent**:

- Prove authors can edit and delete only their own reply.
- Prove deleted replies remain as non-interactive placeholders in the
  conversation instead of disappearing.

### UF-06 Unavailable Parent Conversation

**Story**: US2  
**Start route**: `/posts/:postId`  
**Trigger**: The conversation target is unavailable when the route loads  
**Route transitions**:

- `/posts/:postId` -> `/posts/:postId` unavailable-parent state

**Visible states**:

- unavailable parent placeholder
- still-visible replies below the placeholder
- composer unavailable or hidden

**Required UI elements**:

- `conversation-target-unavailable`
- `conversation-replies`
- `reply-composer-unavailable` or absence of `reply-submit-button`

**Verification intent**:

- Prove the route stays open and truthful.
- Prove visible replies remain accessible in context.

### UF-07 Non-Author Reply Controls Are Absent

**Story**: US2  
**Start route**: `/posts/:postId`  
**Trigger**: Bob views a reply authored by someone else  
**Route transitions**:

- `/posts/:postId` -> `/posts/:postId`

**Visible states**:

- reply visible
- no edit control
- no delete control

**Required UI elements**:

- `post-card-<replyId>`
- absence of `post-edit-button-<replyId>`
- absence of `post-delete-button-<replyId>`

**Verification intent**:

- Prove non-authors are not offered reply edit/delete actions.

### UF-08 Open Available Notification Destination

**Story**: US3  
**Start route**: `/notifications`  
**Trigger**: Bob selects an unread notification with an available destination  
**Route transitions**:

- `/notifications` -> destination route (`/u/:username` or `/posts/:postId`)
- destination route -> `/notifications`

**Visible states**:

- unread notification list state
- destination loading state
- available destination success state
- returned notification row now read

**Required UI elements**:

- `notifications-list`
- `notification-item-<notificationId>`
- `notification-unread-indicator-<notificationId>`
- `notification-read-indicator-<notificationId>`
- `notifications-empty-state` when not applicable

**Verification intent**:

- Prove selecting one notification opens the correct available destination.
- Prove only the selected notification becomes read.

### UF-09 Open Unavailable Notification Destination

**Story**: US3  
**Start route**: `/notifications`  
**Trigger**: Bob selects an unread notification whose target is unavailable  
**Route transitions**:

- `/notifications` -> `/notifications/:notificationId/unavailable`
  or equivalent unavailable destination state
- unavailable destination -> `/notifications`

**Visible states**:

- unread notification list state
- destination unavailable state
- selected row now read after open

**Required UI elements**:

- `notification-item-<notificationId>`
- `notification-unread-indicator-<notificationId>`
- `notification-unavailable-destination`
- `notification-read-indicator-<notificationId>`

**Verification intent**:

- Prove unavailable targets still route to a clear, truthful destination for
  that same notification.
- Prove the selected notification is marked read only after the destination-open
  action.

### UF-10 Notifications List-Only View

**Story**: US3  
**Start route**: `/notifications`  
**Trigger**: Bob opens the notifications list and leaves without selecting an
item  
**Route transitions**:

- `/notifications` -> `/`
  or another route without any notification destination open

**Visible states**:

- unread notification visible before leaving
- same notification remains unread after returning

**Required UI elements**:

- `notifications-list`
- `notification-unread-indicator-<notificationId>`
- `nav-home-link`

**Verification intent**:

- Prove viewing the list alone never marks notifications read.

### UF-11 Home Timeline Automatic Continuation

**Story**: US4  
**Start route**: `/`  
**Trigger**: Bob scrolls until the last currently visible timeline item is the
continuation point  
**Route transitions**:

- `/` -> `/` continuation loading state -> `/` appended state

**Visible states**:

- initial timeline page
- continuation loading indicator
- appended timeline page

**Required UI elements**:

- `timeline-feed`
- `collection-continuation-sentinel`
- `collection-continuation-loading`

**Verification intent**:

- Prove continuation starts automatically at the continuation point and appends
  older items without removing or duplicating visible ones.

### UF-12 Profile Continuation Failure and Retry

**Story**: US4  
**Start route**: `/u/alice`  
**Trigger**: Continuation fails once on the profile post list, then the user
retries  
**Route transitions**:

- `/u/alice` -> `/u/alice` continuation error state
- `/u/alice` continuation error state -> `/u/alice` retry pending state
- `/u/alice` retry pending state -> `/u/alice` appended state

**Visible states**:

- initial profile posts
- continuation failure with retry
- preserved visible items during failure
- successful append after retry

**Required UI elements**:

- `profile-posts`
- `collection-continuation-sentinel`
- `collection-continuation-error`
- `collection-continuation-retry`

**Verification intent**:

- Prove failure does not erase, reorder, or duplicate the visible list.
- Prove retry resumes from the failed continuation point.

### UF-13 Conversation End-of-List State

**Story**: US4  
**Start route**: `/posts/:postId`  
**Trigger**: Bob keeps scrolling through replies until no additional pages
remain  
**Route transitions**:

- `/posts/:postId` -> repeated continuation appends -> `/posts/:postId`
  explicit end state

**Visible states**:

- initial replies page
- one or more continuation loading states
- explicit end-of-list state

**Required UI elements**:

- `conversation-replies`
- `collection-continuation-sentinel`
- `collection-end-state`

**Verification intent**:

- Prove end-of-list is explicit and distinct from initial empty or failure
  states.
