# Feature Specification: Postly Round 2 Social Extensions

**Feature Branch**: `002-profile-replies-notifications`  
**Created**: 2026-04-12  
**Status**: Draft  
**Input**: User description: "Define Round 2 for Postly as a bounded feature phase that extends the current product with profile editing and avatar management, replies and threaded conversation view, in-app notifications, and loading more content across long lists, while treating existing public profile and public post reading as already established behavior."

> Keep this document story-first. `spec.md` records user behavior, business
> rules, edge cases, scope boundaries, and acceptance outcomes only. Do not add
> UI implementation detail, API shape, database design, or test selectors here.

## Clarifications

### Session 2026-04-12

- Q: How should deleted replies appear in conversation view? → A: Keep a
  non-interactive "reply unavailable" placeholder in the conversation for
  deleted replies.
- Q: How should conversation view behave when the parent post becomes
  unavailable? → A: Keep the conversation route open and show an
  unavailable-parent placeholder plus any replies that remain visible under
  current rules.
- Q: When should a notification change from unread to read? → A: When the user
  opens that notification's destination.
- Q: What avatar should be shown when no custom avatar exists or a custom
  avatar later becomes unavailable? → A: Show the system-generated default
  avatar for that user.
- Q: How should continuation loading behave on long lists? → A: Load
  automatically when the user reaches the end; on failure, keep visible content
  and show a clear retry action near the failure point; end of list shows a
  clear final state.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Update Profile Identity (Priority: P1)

A signed-in user updates their display name, bio, and avatar so their current
identity is shown consistently wherever Postly already presents them on profile,
timeline, and direct-post surfaces.

**Why this priority**: Identity editing is the foundation for user-controlled
presence and must work consistently before new engagement features build on top
of it.

**Independent Test**: A signed-in user updates their display name, bio, and
avatar, then confirms the updated identity appears on their profile and on
existing surfaces that already show their authored content.

**Acceptance Scenarios**:

1. **Given** a signed-in user viewing their own profile settings,
   **When** they submit a valid new display name and bio, **Then** the changes
   are saved and the updated profile details appear consistently on their
   profile and on in-scope surfaces that already display their identity.
2. **Given** a signed-in user viewing their own profile settings,
   **When** they upload a new valid avatar, **Then** the new avatar replaces the
   previous one and the updated avatar appears consistently on their profile
   and on in-scope surfaces that already display their identity.
3. **Given** a user's custom avatar does not exist or later becomes
   unavailable, **When** their identity is shown on an in-scope surface,
   **Then** the system-generated default avatar for that user is shown instead.
4. **Given** a signed-in user submits invalid profile changes,
   **When** validation fails, **Then** the user receives clear guidance, their
   in-progress edits are preserved where possible, and their previously saved
   profile remains unchanged.
5. **Given** a user is viewing someone else's profile data,
   **When** they attempt to edit that other user's profile identity,
   **Then** the action is not allowed and no changes are made.

**Edge Cases**:

- Clearing an existing bio MUST be supported if the resulting profile still
  meets the product's profile rules.
- If an avatar replacement fails validation or cannot be processed, the user's
  current avatar MUST remain unchanged.
- If a custom avatar is missing or later becomes unavailable after a successful
  upload, the user's system-generated default avatar MUST be shown instead.
- If updated profile details appear on multiple visible surfaces at once, those
  surfaces MUST not show conflicting versions of the same user's identity after
  a successful save.

---

### User Story 2 - Reply in Conversation View (Priority: P1)

A signed-in user participates in a threaded conversation by replying to a post
from its direct-post view, while authors keep control over editing and deleting
only their own replies.

**Why this priority**: Replies and conversation context turn single-post
viewing into an actual social discussion and are the core behavioral expansion
of this round.

**Independent Test**: A signed-in user opens an existing direct-post page,
adds a reply, sees the reply appear in the conversation, edits it, deletes it,
and confirms they cannot edit or delete replies written by someone else.

**Acceptance Scenarios**:

1. **Given** a signed-in user viewing a post they are allowed to interact with,
   **When** they submit a valid reply, **Then** the reply is added to that
   post's conversation and is shown with the author's identity and reply time.
2. **Given** a user opens a direct-post page for a post with replies,
   **When** the conversation view loads, **Then** the page shows the target
   post together with its visible replies so the discussion can be understood
   in context.
3. **Given** a user opens a conversation whose parent post has become
   unavailable, **When** the page loads, **Then** the conversation route stays
   available, the parent post area shows an unavailable-parent placeholder, and
   any replies that remain visible under current rules are still shown in
   context.
4. **Given** a signed-in user is the author of a reply,
   **When** they submit a valid edit to that reply, **Then** the updated reply
   replaces the previous version wherever that reply is currently visible under
   the product's existing visibility rules.
5. **Given** a signed-in user is the author of a reply,
   **When** they delete that reply, **Then** the original reply content is
   removed, a non-interactive reply-unavailable placeholder remains in the
   conversation, and the deleted reply no longer appears as active authored
   content on any in-scope surface.
6. **Given** a signed-in user is viewing a reply written by another user,
   **When** they inspect available actions, **Then** edit and delete actions
   for that reply are not available to them.

**Edge Cases**:

- Reply submission MUST fail with clear guidance if the reply is empty or does
  not meet the product's post content rules, and the user's draft reply MUST be
  preserved where possible.
- If the target post becomes unavailable before a reply is submitted, the user
  MUST receive a clear message and the reply MUST not be created.
- If the parent post for a conversation becomes unavailable after replies
  exist, the conversation route MUST remain accessible with an
  unavailable-parent placeholder plus any replies that remain visible under
  current rules.
- If a reply is deleted after being visible in a conversation, the conversation
  MUST retain a non-interactive placeholder that makes the missing reply clear
  without exposing its deleted content.
- Conversation content MUST respect the same ownership and visibility rules
  already used for posts unless this round explicitly changes them.

---

### User Story 3 - Stay Aware with In-App Notifications (Priority: P2)

A signed-in user sees in-app notifications for follows, likes, and replies so
they can notice relevant activity and jump directly to the affected destination.

**Why this priority**: Notifications make the new and existing social actions
discoverable after they happen and create an ongoing return loop for users.

**Independent Test**: A signed-in user receives notifications triggered by
other users' follow, like, and reply activity, sees unread and read states, and
opens each notification to reach the relevant profile or conversation
destination.

**Acceptance Scenarios**:

1. **Given** another user follows, likes, or replies in a way that affects a
   signed-in user, **When** the activity is recorded, **Then** the affected
   user receives an in-app notification describing that activity.
2. **Given** a signed-in user has new notifications,
   **When** they open their notifications view, **Then** unread notifications
   are clearly distinguishable from notifications they have already read.
3. **Given** a signed-in user selects a notification,
   **When** the destination opens, **Then** the user is taken to the relevant
   profile, post, or conversation associated with that notification and that
   notification becomes read.
4. **Given** a signed-in user has opened or otherwise consumed a notification,
   **When** they return to the notifications view, **Then** only notifications
   whose destinations have been opened are shown as read.

**Edge Cases**:

- Actions initiated by a user toward their own content or relationship state
  MUST not create notifications for that same user.
- If a notification points to content that later becomes unavailable, the user
  MUST still reach a clear not-available destination rather than a broken or
  misleading screen.
- If a signed-in user has no notifications, the notifications view MUST show an
  empty state that explains there is nothing to review yet.
- Merely opening the notifications list MUST not mark notifications as read
  until the user opens a specific notification's destination.

---

### User Story 4 - Load More Content Across Long Lists (Priority: P3)

A signed-in user can continue browsing beyond the initial set of visible items
on timelines, profile post lists, and conversation views without losing context
or encountering inconsistent state handling.

**Why this priority**: Continuation loading becomes important once replies and
ongoing activity create longer content lists, but the product still delivers
core value without it.

**Independent Test**: A signed-in user opens the home timeline, a profile post
list, and a conversation view, retrieves additional content beyond the initial
set on each surface, and successfully recovers from a failed continuation
attempt.

**Acceptance Scenarios**:

1. **Given** a signed-in user reaches the end of the initial set of items on
   the timeline, a profile post list, or a conversation view, **When** the
   user reaches the continuation point, **Then** additional older items are
   loaded automatically without removing or duplicating items already shown.
2. **Given** a signed-in user requests more content and additional items are
   available, **When** loading succeeds, **Then** the newly revealed items are
   appended in a way that preserves the user's current reading context.
3. **Given** a signed-in user requests more content and loading fails,
   **When** the failure is shown, **Then** the user keeps any content that was
   already visible and is offered a clear retry action near the failure point.
4. **Given** no additional content remains for a list,
   **When** the user reaches the end of available items, **Then** the product
   clearly indicates with a final end-of-list state that there is nothing more
   to load.

**Edge Cases**:

- Initial empty states and "no more items" states MUST remain distinct so users
  can tell whether a list never had content or has simply reached the end.
- A failed attempt to load more content MUST not erase, reorder, or duplicate
  items the user has already loaded successfully.
- Automatic continuation loading MUST not hide the retry action or the final
  end-of-list state.
- If new activity occurs while a user is browsing older content, the already
  visible older content MUST remain stable until the user intentionally refreshes
  or otherwise returns to newer items.

## Scope Boundaries *(mandatory)*

- **In Scope**: Profile editing for a signed-in user's own display name and bio;
  avatar upload and replacement for a signed-in user's own profile; consistent
  reflection of profile updates on in-scope profile, timeline, and direct-post
  surfaces; replying to posts; conversation-focused direct-post pages that show
  a target post and its replies; reply editing and deletion for reply authors
  only; in-app notifications for follows, likes, and replies; unread and read
  notification state; continuation loading beyond the initial content set for
  timelines, profile post lists, and conversation views; explicit loading,
  empty, error, and retry behavior on those in-scope surfaces.
- **Out of Scope**: Re-specifying or redesigning the existing public profile
  reading experience; re-specifying or redesigning the existing public direct
  post permalink reading experience; direct messages; reposts; hashtags or
  trending features; moderation or admin tooling; advanced recommendation
  systems; media in posts other than profile avatar support; email or push
  notifications; changes to current ownership, access, or consistency rules
  unless this round explicitly states otherwise.

## Business Rules *(mandatory)*

- **BR-001**: Existing public profile reading and public direct-post permalink
  reading are established product behavior and are not newly defined by this
  round.
- **BR-002**: Existing post ownership, access, and visibility rules remain the
  default rules for replies unless this round explicitly changes them.
- **BR-003**: Only the signed-in owner of a profile may edit that profile's
  display name, bio, or avatar.
- **BR-004**: Successful profile identity changes MUST be reflected
  consistently across all in-scope surfaces that already show that user's
  identity.
- **BR-004a**: If no custom avatar exists or a custom avatar later becomes
  unavailable, the user's system-generated default avatar MUST be used.
- **BR-005**: A reply belongs to its author and to a specific target post; only
  the reply author may edit or delete that reply.
- **BR-006**: Conversation view MUST present the target post together with the
  replies that are visible under the product's current rules.
- **BR-006a**: When a reply is deleted, the conversation MUST keep a
  non-interactive placeholder in that reply position rather than silently
  collapsing the conversation history.
- **BR-006b**: When the parent post becomes unavailable, the conversation route
  MUST remain available with an unavailable-parent placeholder and any replies
  that remain visible under current rules.
- **BR-007**: Follow, like, and reply activity initiated by another user may
  create notifications; self-initiated actions MUST not create notifications
  for the acting user.
- **BR-008**: Notification state MUST distinguish unread items from read items
  and preserve that distinction until the user has opened that notification's
  destination.
- **BR-009**: Notification destinations MUST lead to the relevant profile, post,
  or conversation context, or to a clear not-available destination if that
  content is no longer available.
- **BR-010**: Continuation loading on long lists MUST preserve already visible
  content, avoid duplicates, and keep loading, empty, error, and retry behavior
  explicit and consistent.
- **BR-010a**: Continuation loading on in-scope long lists MUST trigger
  automatically at the continuation point, expose a retry action near the
  failure point when loading fails, and show a clear final state when no more
  items remain.

## Edge Cases

- A signed-in user who removes all bio text during profile editing MUST be able
  to save a blank bio if the resulting profile remains valid.
- A user MUST never be shown editable controls for another user's profile or
  replies, even if stale content remains visible on screen.
- If a custom avatar cannot be shown for any reason, the user's
  system-generated default avatar MUST appear instead of a broken or empty
  avatar state.
- A reply draft MUST not be silently lost when submission fails or when the
  target post becomes unavailable during submission.
- A deleted reply MUST leave behind a clear non-interactive placeholder in the
  conversation instead of disappearing without explanation.
- An unavailable parent post MUST leave the conversation route intact with a
  clear unavailable-parent placeholder rather than breaking access to visible
  replies.
- Notifications that point to unavailable content MUST still resolve to a clear,
  truthful destination state instead of a broken navigation path.
- Opening the notifications list alone MUST not change unread notifications to
  read.
- Continuation loading failures MUST keep already visible content on screen and
  show a retry action near the failed continuation point.
- Timeline, profile, and conversation surfaces MUST clearly distinguish between
  an initial empty list, a recoverable loading failure, and a list that has no
  additional items remaining.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow a signed-in user to edit their own display
  name.
- **FR-002**: The system MUST allow a signed-in user to edit their own bio.
- **FR-003**: The system MUST allow a signed-in user to upload or replace their
  own profile avatar.
- **FR-003a**: The system MUST show the user's system-generated default avatar
  whenever no custom avatar exists or a custom avatar becomes unavailable.
- **FR-004**: The system MUST reflect successful profile identity updates
  consistently across the signed-in user's profile and across in-scope timeline
  and direct-post surfaces that already show that user's identity.
- **FR-005**: The system MUST prevent a user from editing another user's
  profile identity.
- **FR-006**: The system MUST allow a signed-in user to reply to a post they
  are allowed to interact with under the product's current rules.
- **FR-007**: The system MUST present a conversation-focused direct-post view
  that includes the target post and its visible replies.
- **FR-008**: The system MUST allow the author of a reply to edit that reply.
- **FR-009**: The system MUST allow the author of a reply to delete that reply.
- **FR-010**: The system MUST prevent non-authors from editing or deleting a
  reply they do not own.
- **FR-010a**: The system MUST preserve a non-interactive unavailable-reply
  placeholder in the conversation when a visible reply is deleted.
- **FR-010b**: The system MUST keep the conversation route available with an
  unavailable-parent placeholder when the parent post becomes unavailable and
  visible replies still remain under current rules.
- **FR-011**: The system MUST preserve current post visibility and ownership
  behavior for replies unless explicitly changed by this round.
- **FR-012**: The system MUST create in-app notifications for follow, like, and
  reply activity that affects a signed-in user when the action was initiated by
  another user.
- **FR-013**: The system MUST distinguish unread notifications from read
  notifications.
- **FR-014**: The system MUST allow a signed-in user to open a notification and
  reach the relevant destination associated with that notification.
- **FR-014a**: The system MUST mark a notification as read when the user opens
  that notification's destination, not merely when the notifications list is
  viewed.
- **FR-015**: The system MUST provide a clear not-available outcome when a
  notification or conversation references content that is no longer available.
- **FR-016**: The system MUST allow users to retrieve additional content beyond
  the initial set on the home timeline, profile post lists, and conversation
  views.
- **FR-017**: The system MUST preserve already visible items when a request for
  additional content fails and MUST provide a clear retry path.
- **FR-017a**: The system MUST trigger continuation loading automatically when
  the user reaches the continuation point on in-scope long-list surfaces.
- **FR-018**: The system MUST make loading, empty, error, retry, and end-of-list
  outcomes explicit and consistent on the home timeline, profile post lists,
  and conversation views.

## Key Entities

- **Profile Identity**: The user-facing identity information for an account,
  including display name, bio, and avatar.
- **Reply**: A user-authored post that is attached to a target post and appears
  within that post's conversation context.
- **Conversation**: The direct-post experience for a target post together with
  its visible replies.
- **Notification**: An in-app record that informs a user about a follow, like,
  or reply event relevant to them and links to the related destination.
- **Content List**: A user-visible sequence of posts or replies on a timeline,
  profile, or conversation surface that can reveal more items over time.

## Critical User Outcomes

- **CU-001**: A signed-in user updates their display name, bio, and avatar and
  sees the new identity consistently on profile, timeline, and direct-post
  surfaces.
- **CU-002**: A signed-in user opens a direct-post page, adds a reply, and sees
  the target post plus replies together as a conversation.
- **CU-003**: A reply author edits and deletes their own reply while non-authors
  are not offered those actions.
- **CU-004**: A signed-in user views unread notifications for follows, likes,
  and replies and reaches the correct destination from each notification.
- **CU-005**: A signed-in user loads additional content beyond the initial set
  on timeline, profile, and conversation surfaces while keeping prior content
  visible and recoverable after failures.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In acceptance testing, at least 90% of signed-in users can
  complete a profile identity update, including avatar replacement, on their
  first attempt without assistance.
- **SC-002**: In acceptance testing, at least 95% of valid replies appear in
  the relevant conversation for the replying user without requiring them to
  restart the flow.
- **SC-003**: In acceptance testing, at least 90% of notification interactions
  take users to the intended destination on the first attempt.
- **SC-004**: In acceptance testing, users can retrieve at least two additional
  content sets from each in-scope long-list surface without losing already
  visible items or encountering duplicate items.

## Assumptions

- Existing public profile reading and public direct-post permalink reading
  already exist and remain in place; this round only extends those surfaces
  where new identity consistency or conversation content is relevant.
- Existing rules for who may view, like, edit, or delete posts remain the
  baseline rules for replies unless this specification explicitly states
  otherwise.
- A user has one current avatar at a time; avatar history, galleries, and other
  post media remain out of scope for this round.
- Notification delivery in this round is limited to in-app notification
  experiences and does not include external delivery channels.
