# Frontend Requirements: Postly Microblog MVP

## Purpose

This document makes the MVP frontend testable and implementable by enumerating
every required screen, its route, the UI elements it must contain, its state
variants, and the stable automation hooks that Playwright will depend on.

This document is normative for:

- screen inventory and route ownership
- required UI elements and shared layout patterns
- loading, empty, error, pending, and unavailable states
- `data-testid` hooks used by end-to-end tests

## Screen Inventory

| Screen ID | Route | Access | Purpose |
|-----------|-------|--------|---------|
| `FE-01` | `/signup` | Public | Create a new account and establish a session |
| `FE-02` | `/signin` | Public | Authenticate an existing user and resume protected navigation |
| `FE-03` | `/` | Protected | Show the signed-in home timeline and composer |
| `FE-04` | `/u/:username` | Protected | Show own or other-user profile details and posts |
| `FE-05` | `/posts/:postId` | Protected | Show one post as a deep-link destination |
| `FE-06` | Route-level state | Public or protected | Dedicated first-load error, empty, or unavailable states attached to the owning route |

## Shared UI Contract

### Shared layout regions

Every screen MUST use the following region names consistently where applicable:

- `page-shell`: route-level shell or frame
- `page-heading`: primary page heading
- `page-status`: route-level informational, error, or redirect message region
- `primary-action-region`: most important action area on the screen
- `content-region`: main body content for the route

### Shared automation hook rules

- Critical interactive elements MUST have stable `data-testid` values.
- Test IDs MUST describe function, not visual styling.
- The same logical control MUST use the same test ID on every route where it
  appears.
- Dynamic IDs MUST use predictable suffixes:
  `post-card-<postId>`, `post-like-button-<postId>`, `post-edit-button-<postId>`,
  `post-delete-button-<postId>`, `author-link-<username>`.

### Deterministic e2e fixture contract

Non-production end-to-end environments MUST provide deterministic fixture data
or an equivalent setup harness so Playwright can exercise protected and
multi-user flows repeatably.

The minimum canonical fixture set is:

- `alice`: existing account used as the viewed profile, follow target, and
  author of at least one visible post
- `bob`: existing account used as the primary signed-in actor for most
  authenticated flows

The fixture contract MUST guarantee:

- `alice` can sign in with a known test credential
- `bob` can sign in with a known test credential
- `alice` has at least one visible authored post
- `bob` has at least one owned visible post or the test harness can create one
  before edit/delete assertions
- fixture reset does not require adding test-only public product endpoints to
  the MVP API contract; DB seeding, test harness scripts, or equivalent private
  setup mechanisms are acceptable
- Playwright tests MUST not assume hard-coded numeric post IDs unless the
  fixture reset process guarantees them deterministically
- the seed dataset is defined by the `DataSeed` section in `data-model.md`

### Shared post card contract

Every visible post representation on timeline, profile, and direct-post screens
MUST include:

- `post-card-<postId>`
- `author-link-<username>`
- `post-body-<postId>`
- `post-timestamp-<postId>`
- `post-permalink-<postId>`
- `post-edited-badge-<postId>` when edited
- `post-like-button-<postId>`
- `post-like-count-<postId>`
- `post-edit-button-<postId>` only when `canEdit`
- `post-delete-button-<postId>` only when `canDelete`
- `post-editor-body-input-<postId>` when edit mode is open
- `post-editor-save-button-<postId>` when edit mode is open
- `post-editor-cancel-button-<postId>` when edit mode is open

## Screen Specifications

### FE-01 Sign Up Screen

**Route**: `/signup`  
**Access**: Public  
**Primary goal**: Create an account and enter the signed-in app.

**Depiction**

```text
+--------------------------------------------------+
| Brand / App name                                 |
| Heading: Create your account                     |
| Intro copy                                       |
| [Username input]                                 |
| [Display name input]                             |
| [Bio textarea]                                   |
| [Password input]                                 |
| [Form status / inline error region]              |
| [Create account button]                          |
| Secondary link: Sign in                          |
+--------------------------------------------------+
```

**Required UI elements**

- `signup-page`
- `signup-heading`
- `signup-form`
- `signup-username-input`
- `signup-display-name-input`
- `signup-bio-input`
- `signup-password-input`
- `signup-submit-button`
- `signup-status`
- `signup-signin-link`

**Required behaviors**

- Username, display name, and password are required.
- Bio is optional.
- Password field MUST not preserve its value after a failed submit if the team
  applies the same security posture as sign-in.
- Field-level errors MUST render beside or directly below the relevant input.
- Successful submit navigates to `/`.

**State variants**

- Default form state
- Client/server validation state
- Pending submission state
- Conflict state for duplicate username

### FE-02 Sign In Screen

**Route**: `/signin`  
**Access**: Public  
**Primary goal**: Start a session and continue to a protected destination.

**Depiction**

```text
+--------------------------------------------------+
| Brand / App name                                 |
| Heading: Sign in                                 |
| [Redirect-required message if applicable]        |
| [Username input]                                 |
| [Password input]                                 |
| [Generic auth error region]                      |
| [Sign in button]                                 |
| Secondary link: Create account                   |
+--------------------------------------------------+
```

**Required UI elements**

- `signin-page`
- `signin-heading`
- `signin-redirect-message`
- `signin-form`
- `signin-username-input`
- `signin-password-input`
- `signin-submit-button`
- `signin-status`
- `signin-signup-link`

**Required behaviors**

- Failed auth always uses the same generic error copy.
- Username is preserved on failed auth.
- Password is cleared on failed auth.
- Successful auth returns to the intended protected URL if present.

**State variants**

- Default form state
- Redirect-explanation state
- Generic auth error state
- Pending submission state

### FE-03 Home Timeline Screen

**Route**: `/`  
**Access**: Protected  
**Primary goal**: Let the signed-in user compose and browse their home feed.

**Depiction**

```text
+--------------------------------------------------------------+
| Top bar: brand | home | profile | sign out                   |
| Heading: Home                                             |
| [Timeline status / retry region]                            |
| Composer card                                               |
|   [Textarea] [Character counter] [Submit post button]       |
| Timeline content region                                      |
|   [Empty state OR error state OR list of post cards]        |
+--------------------------------------------------------------+
```

**Required UI elements**

- `home-page`
- `home-heading`
- `nav-home-link`
- `nav-profile-link`
- `nav-signout-button`
- `timeline-status`
- `composer-form`
- `composer-body-input`
- `composer-character-count`
- `composer-submit-button`
- `timeline-feed`
- `timeline-empty-state`
- `timeline-error-state`
- `timeline-retry-button`

**Required behaviors**

- Composer stays visible on the home route.
- Post submit preserves draft on failure.
- Empty state copy differs for "no posts and no follows" vs "has posts but no
  follows" if that distinction is known from the loaded data.

**State variants**

- First-load loading
- First-load error with retry
- Empty onboarding state
- Reduced-content state
- Populated timeline state
- Inline reload error after content already exists

### FE-04 Profile Screen

**Route**: `/u/:username`  
**Access**: Protected  
**Primary goal**: Show profile identity, relationship state, and authored posts.

**Depiction**

```text
+--------------------------------------------------------------+
| Top bar: brand | home | profile | sign out                   |
| [Profile status / retry region]                              |
| Profile header                                               |
|   [Avatar] [Display name] [@username]                        |
|   [Your profile badge OR Follow/Unfollow button]             |
|   [Bio]                                                      |
|   [Follower count] [Following count]                         |
| Posts section                                                |
|   [Empty state OR error state OR list of post cards]         |
+--------------------------------------------------------------+
```

**Required UI elements**

- `profile-page`
- `profile-heading`
- `profile-status`
- `profile-avatar`
- `profile-display-name`
- `profile-username`
- `profile-self-badge`
- `profile-follow-button`
- `profile-unfollow-button`
- `profile-bio`
- `profile-follower-count`
- `profile-following-count`
- `profile-posts`
- `profile-empty-state`
- `profile-error-state`
- `profile-retry-button`

**Required behaviors**

- Own profile never shows follow controls.
- Other-user profile never shows a self badge that implies ownership.
- Follow and unfollow actions only exist in the profile header action region.

**State variants**

- Profile loading
- First-load profile error
- Own-profile empty state
- Other-profile empty state
- Populated posts state
- Inline reload error after content already exists

### FE-05 Direct Post Screen

**Route**: `/posts/:postId`  
**Access**: Protected  
**Primary goal**: Show one post as a deep link with only allowed actions.

**Depiction**

```text
+--------------------------------------------------------------+
| Top bar: brand | home | profile | sign out                   |
| [Back to home]                                               |
| [Post status / unavailable state]                            |
| Single post card                                             |
|   [Author] [Timestamp] [Edited badge]                        |
|   [Body]                                                     |
|   [Like toggle] [Like count] [Edit/Delete if owner]          |
+--------------------------------------------------------------+
```

**Required UI elements**

- `post-page`
- `post-back-link`
- `post-status`
- `post-unavailable-state`
- `post-unavailable-home-link`

**Required behaviors**

- Direct post view reuses the shared post card contract.
- If the post is unavailable, the route shows no protected post content and
  offers a clear path back to the home timeline.

**State variants**

- Loading
- Loaded post
- Unavailable
- Inline action failure while post content remains visible

### FE-06 Dedicated Route States

This is a cross-cutting screen class rather than an additional route.

**Required state containers**

- `protected-redirect-message`
- `page-loading-state`
- `page-error-state`
- `page-empty-state`
- `page-unavailable-state`
- `inline-error`
- `inline-success`
- `confirm-dialog`
- `confirm-dialog-confirm`
- `confirm-dialog-cancel`

**Required behaviors**

- First-load errors replace content with a dedicated retry state.
- Post-load errors keep existing content visible and surface an inline retry.
- Unavailable states use a heading, explanatory copy, and a clear next action.

## Navigation Matrix

| From | Action | To |
|------|--------|----|
| `/signup` | Successful submit | `/` |
| `/signup` | Select "Sign in" link | `/signin` |
| `/signin` | Successful submit without return target | `/` |
| `/signin` | Successful submit with return target | Requested protected route |
| `/signin` | Select "Create account" link | `/signup` |
| `/` | Select signed-in user's identity or nav profile link | `/u/:currentUsername` |
| `/` | Select another user's author link | `/u/:username` |
| `/` | Select a post permalink area | `/posts/:postId` |
| `/u/:username` | Select author link on post card | `/u/:username` |
| `/u/:username` | Select post permalink area | `/posts/:postId` |
| `/posts/:postId` | Select author link | `/u/:username` |
| Protected route | Select sign out | `/signin` |

## Testability Notes

- The frontend should prefer semantic HTML first and use `data-testid` only for
  stable automation-critical hooks.
- Playwright assertions should target headings, roles, and visible text when
  practical, and fall back to `data-testid` for repeated interactive controls
  and route-state containers.
- Playwright setup MUST boot and target the application through
  `dotnet run --project Postly.Api` so e2e coverage exercises the backend-hosted
  static-file path instead of a separate standalone frontend server.
- Every flow in `user-flows.md` is expected to map to at least one Playwright
  test case.
