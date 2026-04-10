# Feature Specification: Postly Microblog MVP

**Feature Branch**: `001-microblog-mvp`  
**Created**: 2026-04-09  
**Status**: Draft  
**Input**: User description: "Define the MVP for Postly, a microblogging social web app similar in spirit to Twitter/X with account access, short text posts, profiles, follows, likes, a home timeline, and responsive accessible web UX."

## Clarifications

### Session 2026-04-09

- Q: Which credentials are used for signup and sign-in? → A: Signup uses
  username, display name, optional bio, and password; sign-in uses username and
  password only.
- Q: How long can an author edit their own post after publishing? → A: Authors
  can edit their own posts any time, with no edit-count limit.
- Q: What happens when a signed-out visitor opens a protected timeline, profile,
  or post URL? → A: Redirect to sign-in with a clear message and return to the
  requested page after successful sign-in.
- Q: How should timeline, profile, and composer flows behave when loading or
  submit actions fail? → A: Preserve any loaded content or draft, show an
  inline error, and offer retry; if nothing has loaded yet, show a dedicated
  error state with retry.
- Q: What are the username rules? → A: Usernames are trimmed before validation,
  MUST be 3 to 20 characters long, MUST use only letters, digits, and
  underscores, and MUST be unique case-insensitively.
- Q: What happens on invalid sign-in? → A: Failed sign-in shows the same
  generic inline error for unknown username or incorrect password, preserves the
  entered username, clears the password field, and keeps the user on the sign-in
  screen.
- Q: What does "default avatar" mean in the MVP? → A: Every account receives a
  system-generated default avatar based on the account's display name initials
  and a deterministic visual style.
- Q: What is a direct post view in the MVP? → A: Direct post view is in scope
  only as a deep-link destination. It shows the single target post, the author
  identity, edited status, aggregate like count, and actions allowed to the
  current signed-in user. Threaded conversation context is out of scope.
- Q: How can users reach another user's profile in the MVP? → A: Another user's
  profile is reachable only by selecting that user's visible identity from a
  post in the home timeline or direct post view, or by opening a known direct
  profile URL. Search, recommendations, and discovery features are out of
  scope.
- Q: What password rules apply in the MVP? → A: Passwords MUST be 8 to 64
  characters long, MUST not be blank or whitespace-only, and are otherwise
  unrestricted in the MVP.
- Q: What display name and bio constraints apply? → A: Display name is required,
  trimmed before validation, and MUST be 1 to 50 characters long. Bio is
  optional and, if provided, MUST be 160 characters or fewer.
- Q: How must controls behave across timeline, profile, and direct-post
  surfaces? → A: Edit and delete controls appear only for the author and on
  every surface where that author's post is shown. Like and unlike controls are
  available to any signed-in user on all post surfaces. Follow and unfollow
  controls appear only on another user's profile, never on the signed-in user's
  own profile or on post cards.
- Q: What happens after sign-out if the user refreshes, uses browser back, or
  directly reopens a protected page? → A: The user remains signed out and MUST
  be routed through sign-in before protected content is shown again.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sign Up (Priority: P1)

A first-time visitor creates an account from the sign-up screen and enters the
application already signed in.

**Why this priority**: Signup is the first critical conversion point and must
be fully specified before any protected social flow exists.

**Independent Test**: A visitor can open `/signup`, submit valid account data,
land on the home timeline, and see their signed-in identity and empty-state or
timeline content.

**Acceptance Scenarios**:

1. **Given** a signed-out visitor on `/signup`, **When** they submit a unique
   username, valid display name, optional valid bio, and valid password,
   **Then** the account is created, a default avatar is assigned, and the user
   lands on the signed-in home timeline.
2. **Given** a visitor submits signup with missing or invalid fields, **When**
   validation runs, **Then** field-level errors are shown for each invalid
   field, non-password values are preserved, and no account is created.
3. **Given** a visitor submits a username already in use, **When** signup
   fails, **Then** the username field shows a specific conflict message and the
   rest of the form stays intact except for any password-clearing rule the UI
   applies consistently.
4. **Given** signup is being submitted, **When** the request is in progress,
   **Then** the primary submit action shows a pending state and duplicate
   submission is prevented.

---

### User Story 2 - Sign In and Resume Protected Navigation (Priority: P1)

A returning user signs in from the sign-in screen and is routed either to the
home timeline or back to the protected page they originally requested.

**Why this priority**: Protected access and reliable session entry are required
before timeline, profile, and direct-post screens can behave predictably.

**Independent Test**: A signed-out visitor requesting `/`, `/u/:username`, or
`/posts/:postId` is redirected to `/signin`, signs in successfully, and returns
to the originally requested protected URL.

**Acceptance Scenarios**:

1. **Given** a registered user on `/signin`, **When** they submit a valid
   username and password, **Then** a session is established and the user lands
   on the originally requested protected destination or the home timeline.
2. **Given** a visitor attempts sign-in with an unknown username or incorrect
   password, **When** sign-in fails, **Then** the same generic inline error is
   shown, the username is preserved, the password is cleared, and the user
   remains on `/signin`.
3. **Given** a signed-out visitor requests a protected timeline, profile, or
   direct-post URL, **When** the app resolves access, **Then** the visitor is
   redirected to `/signin` with a clear message that sign-in is required.
4. **Given** a signed-in user signs out, **When** they refresh, use browser
   back, or directly revisit a previously protected URL, **Then** protected
   content is not shown until they sign in again.

---

### User Story 3 - Publish and Manage Own Posts (Priority: P1)

A signed-in user writes short posts, edits their own posts, and deletes their
own posts from the home timeline, profile, and direct-post surfaces.

**Why this priority**: Post creation and ownership-based management are the core
value loop after authentication.

**Independent Test**: A newly signed-in user can publish a post, edit it,
delete it, sign out, sign back in, and confirm their account still exists.

**Acceptance Scenarios**:

1. **Given** a signed-in user with no posts, **When** they submit a valid
   non-empty post of 280 characters or fewer, **Then** the submit control shows
   a pending state until completion and the post appears at the top of their
   home timeline and on their profile.
2. **Given** the author is viewing their own published post, **When** they edit
   the text and save a valid update, **Then** the save control shows a pending
   state until completion, the post content updates, an edited indicator is
   shown, and the post keeps its original timeline order.
3. **Given** the author is viewing their own post, **When** they confirm
   deletion, **Then** the delete control shows a pending state until completion
   and the post is removed from the home timeline and profile.
4. **Given** a signed-in user is viewing someone else's post on the timeline,
   profile, or direct-post surface, **When** they inspect available actions,
   **Then** edit and delete controls are absent.
5. **Given** a compose or edit attempt exceeds 280 characters or is empty,
   **When** client or server validation runs, **Then** the user receives clear
   inline guidance and their draft text is preserved.

---

### User Story 4 - Build a Personalized Timeline (Priority: P2)

A signed-in user explores profiles, follows and unfollows other users, and sees
their home timeline update to include their own posts plus posts from followed
accounts in newest-first order.

**Why this priority**: Following and the home timeline turn individual posting
into a usable social feed and make the product meaningfully multi-user.

**Independent Test**: A signed-in user can visit another profile, follow that
user, return to the home timeline to see followed content mixed with their own,
and unfollow to remove that content again.

**Acceptance Scenarios**:

1. **Given** a signed-in user with zero posts and zero follows, **When** they
   open the home timeline, **Then** they see an empty state explaining that no
   posts are available yet and prompting them to create a first post or follow
   other users.
2. **Given** a signed-in user with their own posts but zero follows, **When**
   they open the home timeline, **Then** they see only their own posts in
   newest-first order plus guidance to follow people for more content.
3. **Given** a signed-in user sees another user's visible name or avatar on a
   post in the home timeline or direct post view, **When** they select it,
   **Then** that user's profile opens without introducing search,
   recommendations, or other discovery features.
4. **Given** a signed-in user is viewing another user's profile, **When** they
   choose Follow, **Then** the follow control shows a pending state until the
   outcome is known, the profile updates to show the new relationship, follower
   and following counts update, and the followed user's posts appear in the
   signed-in user's home timeline ordered by original publish time, newest
   first.
5. **Given** a signed-in user is viewing a followed user's profile, **When**
   they choose Unfollow, **Then** the unfollow control shows a pending state
   until the outcome is known, the relationship is removed, counts update, and
   that user's posts no longer appear in the follower's home timeline.
6. **Given** a signed-in user is viewing their own profile, **When** the page
   loads, **Then** the page clearly indicates it is their own profile and does
   not offer a Follow action.
7. **Given** a signed-in user is viewing a post card in the home timeline,
   profile post list, or direct post view, **When** the page loads, **Then** no
   Follow action is shown on the post surface itself.
8. **Given** any user attempts to follow themselves, **When** the action is
   submitted, **Then** the action is blocked and a clear message explains that
   users cannot follow themselves.

---

### User Story 5 - React to Posts and View Profiles (Priority: P3)

A signed-in user likes and unlikes posts, views clear profile details for
themselves and others, and encounters consistent protected-access behavior for
timeline, profiles, and posts.

**Why this priority**: Likes and profile clarity strengthen engagement, while
access rules keep the MVP predictable and secure.

**Independent Test**: A signed-in user can like and unlike posts from the
timeline, profile, or direct-post view, see visible like counts update, and
confirm that signed-out visitors are redirected to sign in before protected
content is shown.

**Acceptance Scenarios**:

1. **Given** a signed-in user is viewing a post in the home timeline, on a
   profile, or in direct post view, **When** they choose Like, **Then** the
   like control shows a pending state until the outcome is known, the post
   shows that it is liked by them, and the visible like count increases by one.
2. **Given** a signed-in user has already liked a post in the home timeline, on
   a profile, or in direct post view, **When** they choose Unlike, **Then** the
   unlike control shows a pending state until the outcome is known, their like
   is removed, and the visible like count decreases by one.
3. **Given** a signed-in user is viewing any profile, **When** the page loads,
   **Then** it shows the user's bio, avatar, follower count, following count,
   and that user's posts, and clearly distinguishes whether the profile belongs
   to the signed-in user or another person.
4. **Given** a signed-out visitor requests the home timeline, a profile page,
   or a direct post view, **When** the page loads, **Then** the visitor is
   redirected to sign-in with a clear message and, after successful sign-in,
   returned to the originally requested page.
5. **Given** a signed-in user opens a direct post URL, **When** the page loads
   successfully, **Then** the page shows the single target post, its author
   identity, edited status if applicable, the aggregate like count, and only the
   actions allowed to that signed-in user.
6. **Given** a signed-in user opens a direct post URL for a missing or deleted
   post, **When** the page loads, **Then** the user sees a not-available state
   with a clear path back to the home timeline.
7. **Given** a signed-out visitor requests a direct post URL, **When** the post
   is deleted or becomes unavailable before sign-in completes, **Then** after
   successful sign-in the user sees a not-available state instead of protected
   content.
8. **Given** a signed-in user is viewing the same post on the home timeline,
   on a profile, or in direct post view, **When** the post belongs to that
   signed-in user, **Then** edit and delete controls are available on each post
   surface; otherwise those controls are absent, while like and unlike remain
   available on every visible post surface.

### Edge Cases

- A signup attempt with one or more missing required fields MUST fail with
  field-level guidance for each invalid field, preserve all entered non-password
  values, and create no account.
- A signup attempt with a username already in use MUST fail with a clear,
  field-specific message and preserve the user's other entered values.
- A signup attempt with a username that violates length, allowed-character,
  trimming, or case-insensitive uniqueness rules MUST fail with field-level
  guidance.
- A signup attempt with a password that is blank, whitespace-only, shorter than
  8 characters, or longer than 64 characters MUST fail with field-level
  guidance.
- A signup attempt with a display name that becomes empty after trimming or a
  bio longer than 160 characters MUST fail with field-level guidance.
- A failed sign-in attempt MUST show the same generic inline error whether the
  username is unknown or the password is incorrect, MUST preserve the entered
  username, and MUST clear the password field.
- An empty post or a post longer than 280 characters MUST be rejected before it
  is published, with clear guidance on the allowed limit.
- A signed-in user viewing their own profile with zero posts MUST see an empty
  state that explicitly prompts them to create their first post.
- A signed-in user viewing another user's profile with zero posts MUST see a
  clear "no posts yet" outcome for that profile.
- If a user follows no one and has no posts, the home timeline MUST show an
  empty onboarding state rather than a blank feed.
- If a user follows no one but has posted, the home timeline MUST still show
  their own posts in newest-first order.
- If the user deletes the last remaining post visible on the current home
  timeline, the home timeline MUST immediately transition to the correct empty
  or reduced-content state without requiring manual refresh.
- If a post is deleted, stale attempts to reopen, edit, or delete it again MUST
  leave data unchanged and MUST show a not-available state.
- Repeated like or unlike actions on the same post MUST not create duplicate
  likes or negative counts.
- Repeated follow or unfollow actions against the same user MUST not create
  duplicate relationships or inaccurate follower counts.
- If timeline or profile loading fails after content has already been shown, the
  previously loaded content MUST remain visible while an inline error and retry
  action are shown.
- If timeline or profile loading fails before any content is shown, the screen
  MUST display a dedicated error state with a retry action.
- If signup, sign-in, post creation, post editing, post deletion, follow,
  unfollow, like, or unlike submission is in progress, the initiating control
  MUST show a pending state and MUST prevent duplicate resubmission until the
  outcome is known.
- If post creation, post editing, post deletion, follow, unfollow, like, or
  unlike submission fails, the current screen state MUST remain visible, any
  unsaved composer text MUST be preserved, and the error MUST be explained
  inline with a retry path.
- After sign-out, refresh, browser back, and direct navigation to previously
  viewed protected URLs MUST not reveal protected content again before sign-in.

## System Boundaries & Contracts *(mandatory)*

### Affected Modules

- **Module**: Identity and access
- **Responsibility**: Create accounts, start and end user sessions, and protect
  signed-in-only content.
- **Public Interface**: Sign-up, sign-in, sign-out, and protected-access entry
  points for timeline, profiles, and posts.

- **Module**: Posting and engagement
- **Responsibility**: Create, edit, delete, and display posts; enforce post
  ownership; record likes and visible like counts.
- **Public Interface**: Post composer, post cards, author-only post controls,
  direct post view, and like/unlike actions.

- **Module**: Profiles and relationships
- **Responsibility**: Display profile details, maintain follow relationships and
  counts, and assemble the signed-in user's home timeline.
- **Public Interface**: Home timeline, profile pages, follow/unfollow actions,
  and own-profile vs other-profile presentation.

### Validation & Error Handling

- **Input Contract**: Account creation accepts username, display name, optional
  bio, and password. Usernames are trimmed before validation. Display names are
  trimmed before validation. Sign-in accepts username and password only. Direct
  post view accepts a valid post destination. Post creation and post editing
  accept non-empty text up to 280 characters. Follow, unfollow, like, and
  unlike actions require a signed-in user and a valid target user or post.
- **Validation Rules**:
  - Usernames MUST be trimmed before validation.
  - Usernames MUST be 3 to 20 characters long.
  - Usernames MUST use only letters, digits, and underscores.
  - Usernames MUST be unique case-insensitively.
  - Passwords MUST be 8 to 64 characters long and MUST not be blank or
    whitespace-only.
  - Display names MUST be 1 to 50 characters long after trimming.
  - Bio, when provided, MUST be 160 characters or fewer.
  - Users MUST be signed in to view timeline, profile, and direct post content.
  - Only the author of a post MAY edit or delete that post.
  - Author-only edit and delete controls MUST appear only when the current
    signed-in user is the post author, and MUST do so consistently on every
    surface where that user's post is shown.
  - Any signed-in user MAY like or unlike a visible post, and like controls
    MUST be available consistently on every visible post surface.
  - Any signed-in user MAY follow or unfollow another user, except themselves.
  - Follow and unfollow controls MUST appear only on another user's profile, not
    on the signed-in user's own profile or on post surfaces.
  - Another user's profile MAY be reached only from a visible author identity on
    a visible post or by opening a known direct profile URL.
  - Duplicate follow or like actions MUST resolve to a single current state.
- **Error Outcomes**:
  - Invalid form input MUST return field-level guidance for each invalid field.
  - Failed sign-in MUST return the same generic inline error for unknown
    username and incorrect password.
  - Forbidden actions MUST return a clear message without changing data.
  - Missing or deleted content MUST show a not-available state.
  - Signed-out access to protected content MUST redirect to sign-in with a
    clear message and, after successful sign-in, return the user to the
    originally requested page.
  - After sign-out, refresh, browser back, or direct navigation to a protected
    URL MUST require sign-in again before protected content is shown.
- **Success Outcomes**:
  - Successful signup MUST create the account and land the user signed in on
    the home timeline.
  - Successful sign-in MUST open the originally requested protected destination
    when one exists, otherwise the home timeline.
  - Successful compose MUST show the new post immediately in the user's home
    timeline and profile.
  - Successful follow or unfollow MUST update the relationship state and
    visible counts immediately.
  - Successful like or unlike MUST update the current-user liked state and
    aggregate like count immediately.
  - Successful edit or delete MUST update or remove the post without requiring
    a manual refresh.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow any new regular user to create an account
  through open signup.
- **FR-002**: Account creation MUST require a username, display name, and
  password, and MAY accept an optional bio.
- **FR-003**: Usernames MUST be trimmed before validation, MUST be 3 to 20
  characters long, MUST use only letters, digits, and underscores, and MUST be
  unique case-insensitively.
- **FR-004**: Passwords MUST be 8 to 64 characters long, MUST not be blank or
  whitespace-only, and are otherwise unrestricted in the MVP.
- **FR-005**: Display names MUST be required, MUST be trimmed before
  validation, and MUST be 1 to 50 characters long after trimming.
- **FR-006**: Bio MUST be optional and, when provided, MUST be 160 characters
  or fewer.
- **FR-007**: The system MUST assign every new account a system-generated
  default avatar based on the account's display name initials and a
  deterministic visual style, so every profile displays an avatar in the MVP
  without requiring media upload.
- **FR-008**: The system MUST allow registered users to sign in with username
  and password only, and to sign out.
- **FR-009**: Failed sign-in MUST show the same generic inline error for
  unknown username and incorrect password, MUST preserve the entered username,
  and MUST clear the password field.
- **FR-010**: The system MUST restrict the home timeline, profile pages, and
  direct post views to signed-in users.
- **FR-011**: After sign-out, refresh, browser back, or direct navigation to a
  previously viewed protected page MUST not reveal protected content again; the
  user MUST be treated as signed out and routed through sign-in before
  protected content is shown.
- **FR-012**: The system MUST support direct post view as a deep-link
  destination for a single post.
- **FR-013**: Direct post view MUST show the target post, author identity,
  edited status if applicable, aggregate like count, and only the actions
  allowed to the current signed-in user.
- **FR-014**: Another user's profile MUST be reachable only by selecting a
  visible author identity from a visible post in the home timeline or direct
  post view, or by opening a known direct profile URL. Search,
  recommendations, and other discovery features are out of scope.
- **FR-015**: The system MUST allow signed-in users to create text-only posts.
- **FR-016**: A post MUST contain between 1 and 280 characters inclusive.
- **FR-017**: The system MUST allow users to edit only their own posts after
  publishing, with no time-window or edit-count restriction in the MVP.
- **FR-018**: Edited posts MUST continue to obey the 280-character limit, MUST
  show that they were edited, and MUST keep their original publish time for
  newest-first ordering.
- **FR-019**: The system MUST allow users to delete only their own posts.
- **FR-020**: The system MUST prevent any user from editing or deleting another
  user's post, even if the action is attempted outside normal UI controls.
- **FR-021**: Author-only edit and delete controls MUST appear only for the
  post author and MUST do so consistently on every surface where that author's
  post is shown.
- **FR-022**: The home timeline MUST show the signed-in user's own posts and
  the posts of users they follow, ordered newest first by original publish
  time.
- **FR-023**: If a signed-in user follows no one and has no posts, the home
  timeline MUST show an empty state with explicit next steps to create a post
  or follow users.
- **FR-024**: If a signed-in user follows no one but has posts, the home
  timeline MUST show the user's own posts and guidance that following people
  adds more content to the feed.
- **FR-025**: If the user deletes the last remaining visible post from the
  current home timeline, the screen MUST immediately transition to the correct
  empty or reduced-content state without requiring manual refresh.
- **FR-026**: Profile pages MUST show bio, avatar, follower count, following
  count, and the profile owner's posts.
- **FR-027**: Profile pages MUST clearly distinguish the signed-in user's own
  profile from another user's profile.
- **FR-028**: A signed-in user viewing their own profile with zero posts MUST
  see an empty state that prompts them to create their first post.
- **FR-029**: A signed-in user viewing another user's profile with zero posts
  MUST see a clear "no posts yet" outcome for that profile.
- **FR-030**: The system MUST allow signed-in users to follow and unfollow
  other users.
- **FR-031**: The system MUST prevent users from following themselves.
- **FR-032**: Follow and unfollow controls MUST appear only on another user's
  profile and MUST not appear on the signed-in user's own profile or on post
  cards or direct-post surfaces.
- **FR-033**: The system MUST allow signed-in users to like and unlike posts.
- **FR-034**: Posts MUST show an aggregate like count to signed-in users and
  MUST indicate whether the current signed-in user has liked the post.
- **FR-035**: The identity of other likers MUST not be shown anywhere in the
  MVP.
- **FR-036**: Like and unlike controls MUST be available to signed-in users on
  timeline, profile, and direct-post surfaces wherever a visible post appears.
- **FR-037**: Profiles and posts MUST be visible only to signed-in users.
- **FR-038**: When a signed-out visitor requests a protected timeline,
  profile, or post URL, the system MUST redirect them to sign-in, explain that
  sign-in is required, and return them to the originally requested page after
  successful sign-in.
- **FR-039**: If a protected direct post URL no longer points to available
  content by the time sign-in completes, the system MUST show a not-available
  state instead of protected content.
- **FR-040**: The UI MUST present explicit loading, empty, success, and error
  states for signup, sign-in, timeline, profiles, compose, edit post, delete
  post, follow, unfollow, like, and unlike flows.
- **FR-041**: During signup, sign-in, compose, edit, delete, follow, unfollow,
  like, and unlike submission, the initiating control MUST show a pending state
  and MUST prevent duplicate resubmission until the outcome is known.
- **FR-042**: If timeline or profile loading fails after content was
  previously loaded, the system MUST keep that content visible, show an inline
  error message, and provide a retry action.
- **FR-043**: If timeline or profile loading fails before any content is
  available, the system MUST show a dedicated error state with a retry action.
- **FR-044**: If a compose or interaction submission fails, the system MUST
  preserve the user's current draft or visible content state, explain the
  failure inline, and provide a retry path without forcing the user to start
  over.
- **FR-045**: Successful signup MUST land the user signed in on the home
  timeline.
- **FR-046**: Successful sign-in MUST open the originally requested protected
  destination when one exists, otherwise the home timeline.
- **FR-047**: Successful compose, edit, delete, follow, unfollow, like, and
  unlike actions MUST update the visible state immediately without requiring
  manual refresh.
- **FR-048**: The MVP experience MUST support all core flows on narrow mobile
  web and desktop web, with no horizontal scrolling required for primary
  content or actions.
- **FR-049**: Accessibility basics MUST be treated as required behavior,
  including keyboard access, visible focus, descriptive labels, readable
  feedback, and non-color-only status communication.
- **FR-050**: Signup and sign-in errors MUST be readable and clearly
  associated with the relevant fields or form.
- **FR-051**: Compose and edit flows MUST expose remaining or violated
  post-length limits in readable text.
- **FR-052**: Follow, unfollow, like, and unlike state changes MUST be
  perceivable without color alone.
- **FR-053**: Redirect, error, empty, and not-available states MUST expose
  clear headings and actionable controls.
- **FR-054**: The MVP MUST exclude direct messages, reposts, replies or
  comments, hashtags, trending topics, search, media upload, notifications,
  admin or moderation tools, anonymous browsing, and profile editing after
  signup.
- **FR-055**: The frontend MUST implement the signed-out routes `/signup` and
  `/signin`, and the protected routes `/`, `/u/:username`, and
  `/posts/:postId`.
- **FR-056**: Each route MUST render one primary page heading, one primary
  action area, and one deterministic loading, empty, success, error, or
  unavailable state container as appropriate for that route.
- **FR-057**: The frontend MUST define and preserve stable automation hooks for
  critical controls and state containers, using `data-testid` values that map
  directly to documented screen elements and flow steps.
- **FR-058**: The sign-up screen MUST contain username, display name, bio, and
  password inputs, field-level validation messages, a primary submit button, a
  link to sign-in, and a visible form-level status region.
- **FR-059**: The sign-in screen MUST contain username and password inputs, a
  primary submit button, a link to sign-up, a visible form-level status region,
  and a protected-access return message area when the screen is reached by
  redirect.
- **FR-060**: The home timeline screen MUST contain a signed-in shell, a post
  composer, timeline feed region, timeline empty state, timeline error state,
  and sign-out action.
- **FR-061**: The profile screen MUST contain profile identity, avatar, bio,
  follower and following counts, relationship action region, profile post list,
  and a distinct empty or error state when profile posts are absent or fail to
  load.
- **FR-062**: The direct-post screen MUST contain a back navigation action, the
  single target post, author identity, edited indicator when applicable, like
  count, allowed actions for the viewer, and a dedicated not-available state.
- **FR-063**: Reusable post cards MUST expose author navigation, body text,
  publish timestamp, edited indicator when applicable, like toggle, like count,
  and viewer-permitted edit and delete actions.
- **FR-064**: Post deletion MUST require an explicit confirmation pattern or an
  equally clear accidental-action prevention mechanism consistent across the
  frontend.
- **FR-065**: The frontend requirements, screen inventory, required UI
  elements, and automation hooks MUST be maintained in
  `frontend-requirements.md`, and the user-story interaction sequences MUST be
  maintained in `user-flows.md` as normative companion artifacts to this spec.
- **FR-066**: Non-production environments MUST support a deterministic end-to-
  end test fixture strategy for authentication and social-graph flows, using
  seeded accounts or an equivalent test harness that produces the same stable
  preconditions.
- **FR-067**: The end-to-end fixture strategy MUST provide at least one stable
  acting user and one stable follow target user so sign-in, follow, unfollow,
  like, profile, and protected-route return flows can run repeatably without
  relying on ad hoc data creation.
- **FR-068**: Non-production environments MUST define a deterministic database
  seed named `DataSeed` that creates the minimum users, posts, follows, and
  like-state permutations required by the documented user flows and automated
  tests.
- **FR-069**: `DataSeed` MUST be resettable so each end-to-end test run can
  start from a known state without depending on residual data from previous
  runs.
- **FR-070**: `DataSeed` MUST include stable direct-profile and direct-post
  destinations used by protected-route return, unavailable-content, like,
  follow, and timeline scenarios.
- **FR-071**: The ASP.NET Core application MUST serve the built frontend static
  assets from `wwwroot` in the backend application output for local runs and
  deployable builds.
- **FR-072**: The build pipeline MUST synchronize the frontend build output into
  the backend `wwwroot` so the backend remains the single runtime entry point
  for the full application.
- **FR-073**: Running `dotnet run --project Postly.Api` MUST start the full MVP
  stack needed for end-to-end usage: backend HTTP server, frontend experience
  served by the backend app, and the configured SQLite database with required
  migrations or equivalent startup preparation applied.
- **FR-074**: End-to-end Playwright execution MUST target the application
  started through `dotnet run --project Postly.Api` rather than a separate
  standalone frontend dev server.
- **FR-075**: Repository ignore rules MUST exclude local SQLite database files,
  SQLite journal or WAL sidecar files, and equivalent local DB artifacts that
  are generated by development, tests, or local runs.

### Key Entities *(include if feature involves data)*

- **User Account**: A registered person with sign-in credentials, a unique
  case-insensitive username, a trimmed display name, an optional bio, and a
  system-generated default avatar.
- **Session**: The current signed-in state that determines whether protected
  content and actions are available.
- **Post**: A short text publication created by a user, with author ownership,
  original publish time, current text, edited status, and a direct deep-link
  destination.
- **Profile**: The signed-in view of a user account, including bio, avatar,
  follower count, following count, and authored posts.
- **Follow Relationship**: A directional connection where one user subscribes
  to another user's posts for home timeline inclusion.
- **Like**: A user's positive reaction to a specific post, represented in the
  MVP by current-user liked state and a visible aggregate count.

## UX Consistency Impact *(mandatory)*

- **Existing Pattern Referenced**: Postly MVP MUST establish one consistent web
  pattern for account entry, one shared signed-in shell, one reusable post
  card, one reusable direct-post presentation, and one reusable profile layout
  that are applied across timeline, profile, and direct-post views.
- **States Covered**: Signup, sign-in, sign-out, compose, edit post, delete
  post, timeline loading, timeline empty, protected-access redirect messages,
  profile loading, profile empty, direct-post unavailable, follow/unfollow
  feedback, like/unlike feedback, dedicated first-load error states, pending
  submission states, and inline retry states after partial content or draft data
  already exists.
- **Accessibility/Copy Notes**: User-facing copy MUST use plain language and
  identify the current outcome and next action. Interactive elements MUST be
  keyboard reachable, focus states MUST be visible, and feedback MUST not rely
  on color alone. Profile distinctions such as "Your profile" versus another
  user's profile MUST be explicit in text, not implied only by layout.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 90% of first-time test users can create an account, sign
  in, and publish their first post within 3 minutes.
- **SC-002**: In acceptance testing, 100% of attempts to edit or delete another
  user's post or to follow oneself are blocked with a clear explanation.
- **SC-003**: At least 90% of evaluated desktop and mobile web sessions can
  complete the core flow of sign in, read the home timeline, visit a profile,
  and create a post without loss of functionality.
- **SC-004**: At least 90% of usability-test participants can correctly tell
  within 5 seconds whether they are viewing their own profile or someone else's
  profile.
- **SC-005**: In review of the MVP states, every supported user flow includes a
  defined loading, empty, success, or error outcome wherever that state can
  occur.

## Assumptions

- Signup is open to any regular user who is not already registered.
- Email-based sign-in, email verification, and password recovery are out of
  scope for the MVP.
- All readable product content in the MVP is private to signed-in users; there
  is no anonymous browsing experience for profiles, posts, or the home
  timeline.
- Signed-out attempts to open protected URLs return users through the sign-in
  flow and then back to the originally requested destination.
- Another user's profile is reached from a visible author identity on a visible
  post or by a known direct profile URL only; no search, recommendations, or
  broader discovery experience exists in the MVP.
- Profile editing beyond the initial display name and optional bio entered at
  signup is out of scope for the MVP.
- Every account receives a system-generated default avatar rather than
  user-uploaded media.
- Users can edit only their own posts, with no edit history, and edits do not
  move posts to the top of the timeline.
- Likes are visible only as an aggregate count plus current-user liked state;
  there is no browsable list of who liked a post.
- Direct post view is supported only for a single target post and does not
  include threaded conversation context.
- Replies, comments, direct messages, reposts, hashtags, trending topics,
  search, notifications, moderation tools, anonymous browsing, and profile
  editing after signup are out of scope for the MVP.
