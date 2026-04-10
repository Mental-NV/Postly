# User Flows: Postly Microblog MVP

## Purpose

This document translates the MVP user stories into step-by-step UI interaction
flows. Each flow names the required UI elements, the user action, and the
expected result so the sequence can be turned directly into Playwright tests.

## Conventions

- `UI element` values reference required controls from
  `frontend-requirements.md`.
- `Expected result` describes the visible outcome the browser test should
  assert.
- Route transitions are explicit so protected navigation can be tested.

## Deterministic Test Preconditions

- `bob` is the default acting user for authenticated flows unless the flow says
  otherwise.
- `alice` exists as another user with a reachable profile at `/u/alice`.
- `alice` has at least one visible authored post for follow, timeline, like,
  and profile navigation scenarios.
- `bob` has at least one owned visible post in `DataSeed`, or the test may
  create one before edit/delete assertions.
- Protected flows may establish an authenticated session through UI sign-in or a
  storage-state helper backed by the same fixture data.
- All fixture expectations come from the `DataSeed` section in `data-model.md`.

## Flow Index

| Flow ID | Story | Outcome |
|---------|-------|---------|
| `UF-01` | Sign Up | Visitor creates an account and lands on home |
| `UF-02` | Sign Up validation | Visitor receives field-level errors |
| `UF-03` | Sign In | Returning user signs in to home |
| `UF-04` | Protected redirect return | Visitor resumes requested protected route after sign-in |
| `UF-05` | Publish post | Signed-in user creates a post |
| `UF-06` | Edit own post | Author updates post text |
| `UF-07` | Delete own post | Author removes a post |
| `UF-08` | Follow and timeline update | User follows another profile and sees followed content |
| `UF-09` | Unfollow and timeline reduction | User removes relationship and feed content disappears |
| `UF-10` | Like and unlike | User toggles post reaction |
| `UF-11` | Direct post unavailable | User sees not-available state for missing post |
| `UF-12` | Sign out protection | Signed-out user cannot re-open protected content |

## Detailed Flows

### UF-01 Sign Up

**Start route**: `/signup`

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `signup-heading` | Observe page | Heading indicates account creation |
| 2 | `signup-username-input` | Enter valid unique username | Input reflects entered value |
| 3 | `signup-display-name-input` | Enter valid display name | Input reflects entered value |
| 4 | `signup-bio-input` | Enter optional bio | Bio value is visible |
| 5 | `signup-password-input` | Enter valid password | Password field accepts input |
| 6 | `signup-submit-button` | Click submit | Button enters pending state and becomes non-repeatable |
| 7 | `home-page` | Wait for navigation | Route becomes `/` and signed-in shell is visible |
| 8 | `timeline-feed` or `timeline-empty-state` | Observe content area | User sees home timeline surface with signed-in context |

### UF-02 Sign Up Validation

**Start route**: `/signup`

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `signup-submit-button` | Submit empty form | Field-level errors appear for required fields |
| 2 | `signup-username-input` | Enter invalid username with disallowed characters | Username error explains allowed format |
| 3 | `signup-display-name-input` | Enter whitespace-only display name | Display name error indicates trimmed value cannot be empty |
| 4 | `signup-password-input` | Enter short password | Password error states minimum length |
| 5 | `signup-submit-button` | Submit form again | Account is not created and user remains on `/signup` |

### UF-03 Sign In

**Start route**: `/signin`
**Preconditions**: Seeded user `bob` exists

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `signin-username-input` | Enter existing username | Username value is visible |
| 2 | `signin-password-input` | Enter valid password | Password field accepts input |
| 3 | `signin-submit-button` | Click submit | Button enters pending state |
| 4 | `home-page` | Wait for navigation | Route becomes `/` and timeline shell is visible |

### UF-04 Protected Redirect Return

**Start route**: `/u/alice` or a known seeded direct-post URL while signed out
**Preconditions**: Seeded users `alice` and `bob` exist

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `signin-page` | Observe redirected route | User lands on `/signin` instead of protected page |
| 2 | `signin-redirect-message` | Read message | Screen explains sign-in is required to continue |
| 3 | `signin-username-input` | Enter valid username | Username remains visible |
| 4 | `signin-password-input` | Enter valid password | Password input accepts text |
| 5 | `signin-submit-button` | Click submit | Session is established |
| 6 | Protected page heading | Wait for navigation | User lands on originally requested protected route |

### UF-05 Publish Post

**Start route**: `/`
**Preconditions**: Signed in as `bob`

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `composer-body-input` | Enter valid post text | Draft text is visible |
| 2 | `composer-character-count` | Observe helper text | Remaining count reflects current draft |
| 3 | `composer-submit-button` | Click submit | Submit control enters pending state |
| 4 | `timeline-feed` | Observe updated feed | New post appears at the top of the home timeline |
| 5 | `nav-profile-link` | Click profile navigation | Route changes to own profile |
| 6 | `profile-posts` | Observe posts list | Newly created post is also visible on profile |

### UF-06 Edit Own Post

**Start route**: `/` or `/u/:currentUsername`
**Preconditions**: Signed in as `bob`; at least one own visible post exists

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `post-edit-button-<postId>` | Click edit action on own post | Edit UI opens for that post |
| 2 | `post-editor-body-input-<postId>` | Replace text with valid updated text | Edited draft is visible |
| 3 | `post-editor-save-button-<postId>` | Click save | Save control enters pending state |
| 4 | `post-body-<postId>` | Observe post content | Post body updates in place |
| 5 | `post-edited-badge-<postId>` | Observe status | Edited indicator is visible |

### UF-07 Delete Own Post

**Start route**: `/` or `/u/:currentUsername`
**Preconditions**: Signed in as `bob`; at least one own visible post exists

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `post-delete-button-<postId>` | Click delete action | Confirmation pattern appears |
| 2 | `confirm-dialog-confirm` | Confirm deletion | Delete action enters pending state |
| 3 | `timeline-feed` or `profile-posts` | Observe updated list | Deleted post is no longer visible |
| 4 | Empty or reduced state container | Observe screen | Route transitions correctly if the deleted post was last visible content |

### UF-08 Follow and Timeline Update

**Start route**: `/u/alice`
**Preconditions**: Signed in as `bob`; `alice` is not currently followed by `bob`

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `profile-follow-button` | Click follow | Button enters pending state |
| 2 | `profile-unfollow-button` | Observe profile header | Relationship state changes to followed |
| 3 | `profile-follower-count` | Observe counts | Follower count updates immediately |
| 4 | `nav-home-link` | Click home navigation | Route changes to `/` |
| 5 | `timeline-feed` | Observe feed | Followed user's posts appear in newest-first order |

### UF-09 Unfollow and Timeline Reduction

**Start route**: `/u/alice`
**Preconditions**: Signed in as `bob`; `alice` is currently followed by `bob`

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `profile-unfollow-button` | Click unfollow | Button enters pending state |
| 2 | `profile-follow-button` | Observe profile header | Relationship state changes to unfollowed |
| 3 | `nav-home-link` | Click home navigation | Route changes to `/` |
| 4 | `timeline-feed` or `timeline-empty-state` | Observe feed | Unfollowed user's posts are absent |

### UF-10 Like and Unlike

**Start route**: `/`, `/u/:username`, or `/posts/:postId`
**Preconditions**: Signed in as `bob`; a visible post authored by `alice` exists

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `post-like-button-<postId>` | Click like | Control enters pending state |
| 2 | `post-like-count-<postId>` | Observe count | Like count increases by one |
| 3 | `post-like-button-<postId>` | Observe state | Control shows liked state without relying only on color |
| 4 | `post-like-button-<postId>` | Click again to unlike | Control enters pending state |
| 5 | `post-like-count-<postId>` | Observe count | Like count decreases by one |

### UF-11 Direct Post Unavailable

**Start route**: `/posts/999999`
**Preconditions**: Signed in as `bob`

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `post-unavailable-state` | Observe route content | Missing/deleted post state is shown |
| 2 | `post-unavailable-home-link` | Click recovery action | User can navigate back to home timeline |

### UF-12 Sign Out Protection

**Start route**: `/`
**Preconditions**: Signed in as `bob`

| Step | UI element | Interaction | Expected result |
|------|------------|-------------|-----------------|
| 1 | `nav-signout-button` | Click sign out | Session ends and app navigates to `/signin` |
| 2 | Browser back or direct protected URL | Attempt to revisit `/`, `/u/:username`, or `/posts/:postId` | User is redirected back to `/signin` |
| 3 | `signin-redirect-message` | Observe message | Screen explains protected content requires sign-in |
