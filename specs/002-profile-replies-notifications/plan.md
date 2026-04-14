# Implementation Plan: Postly Round 2 Social Extensions

**Branch**: `002-profile-replies-notifications` | **Date**: 2026-04-14 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/002-profile-replies-notifications/spec.md`

## Summary

Extend the existing backend-hosted Postly web app with four bounded Round 2
capabilities: own-profile identity editing with avatar replacement, replies on
conversation-oriented direct-post routes, in-app notifications, and automatic
continuation loading on timeline/profile/conversation collections. The design
stays additive to the MVP architecture: ASP.NET Core Minimal APIs remain the
only server interface, EF Core + SQLite remain the persistence model, the React
SPA continues to run from backend `wwwroot`, and existing public profile/direct
post read behavior remains baseline product behavior rather than new scope.

The implementation keeps one clear traceability chain per story:
`spec.md` -> `user-flows.md` -> `frontend-requirements.md` /
backend responsibilities -> `openapi.yaml` / `data-model.md` -> `tasks.md`.
Round 2 uses the smallest viable changes: extend the current `Post` model for
replies, add first-class notifications, keep cursor pagination semantics, and
project profile identity consistently across the already-existing surfaces that
show it.

## Technical Context

**Language/Version**: C# on .NET 10 (backend), TypeScript in strict mode (frontend)  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core with SQLite provider and migrations, ASP.NET Core authentication/cookie middleware, ASP.NET Core Identity password hasher, React, React Router, Vite, Vitest, React Testing Library, Playwright  
**Storage**: SQLite via EF Core migrations; persisted session records; persisted avatar state, reply relationships, soft-delete markers for reply placeholders, and notifications  
**Testing**: xUnit backend unit/integration/contract tests, Vitest + React Testing Library frontend tests, Playwright backend-hosted end-to-end coverage  
**Target Platform**: Same-origin web app for desktop and mobile browsers, served by the ASP.NET Core backend  
**Project Type**: Same-repo full-stack web application  
**Interfaces/Contracts**: REST/JSON API under `/api`, same-origin SPA routes (`/`, `/u/:username`, `/posts/:postId`, `/notifications`), multipart avatar upload, cursor-based continuation for long lists, `application/problem+json` error responses, normative frontend flow and test-hook contracts  
**Error Handling Strategy**: Boundary validation on every request, domain/application errors mapped to ProblemDetails-style responses with stable error codes, inline validation for form flows, route-level unavailable/error states for destination or continuation failures  
**UX Surfaces**: Own profile inline edit state, profile post list continuation, conversation-oriented direct-post page with replies and placeholders, notifications list, home timeline continuation  
**Performance Goals**: Preserve MVP latency targets for primary reads; keep page sizes and newest-first cursor behavior stable; append additional items without re-fetching or replacing already visible collections  
**Constraints**: Preserve current ownership/access/visibility rules unless the Round 2 spec explicitly changes them; do not re-specify public profile/direct-post read mode as new scope; reuse existing cursor semantics where possible; keep deterministic seeded data for Playwright; avoid speculative abstractions or new infrastructure  
**Scale/Scope**: Single-node SQLite-backed social app for low-thousands of active users and tens of thousands of posts, with four Round 2 user stories and no new deployment tier

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

For features with multiple user stories, use `/speckit.analyze` before
`/speckit.implement` unless a maintainer-approved exception is documented.

### Pre-Design Gate

- [x] `spec.md` is story-first, includes acceptance criteria and scope
      boundaries, and excludes UI implementation detail, API shape, database
      design, and test selectors.
      The clarified spec remains behavior-only and explicitly keeps baseline
      public profile/direct-post reading out of new scope.
- [x] Every approved user story maps to at least one end-to-end user flow with
      route transitions, visible states, and verification intent.
      `user-flows.md` defines primary and recovery flows for all four stories.
- [x] Required UI automation elements and stable `data-testid` hooks are
      defined before implementation, and repeated controls reuse the same test
      ID across surfaces.
      `frontend-requirements.md` defines shared collection hooks plus
      per-surface/profile/notification controls.
- [x] Frontend responsibilities, backend responsibilities, API contracts,
      validation rules, error outcomes, and persistence changes trace back to
      the relevant user story and flow.
      `plan.md`, `openapi.yaml`, `data-model.md`, `user-flows.md`, and
      `frontend-requirements.md` maintain story-by-story traceability.
- [x] Required backend unit/integration or contract coverage, frontend
      component coverage, and Playwright coverage are planned for each
      user-visible story, with tests-first task ordering preferred when
      feasible.
      `quickstart.md` enumerates backend, frontend, and Playwright validation
      expectations for each Round 2 surface.
- [x] Clear module boundaries are documented, including ownership and allowed
      dependency direction.
      The design preserves feature-based backend/frontend ownership and keeps
      API contracts at the boundary.
- [x] The design is the simplest viable approach, and any breaking change
      includes migration or rollback notes.
      Round 2 remains additive to the MVP architecture, uses EF migrations for
      schema evolution, and introduces no new service tier.

### Post-Design Re-Check

- [x] `spec.md` is story-first, includes acceptance criteria and scope
      boundaries, and excludes UI implementation detail, API shape, database
      design, and test selectors.
- [x] Every approved user story maps to at least one end-to-end user flow with
      route transitions, visible states, and verification intent.
- [x] Required UI automation elements and stable `data-testid` hooks are
      defined before implementation, and repeated controls reuse the same test
      ID across surfaces.
- [x] Frontend responsibilities, backend responsibilities, API contracts,
      validation rules, error outcomes, and persistence changes trace back to
      the relevant user story and flow.
- [x] Required backend unit/integration or contract coverage, frontend
      component coverage, and Playwright coverage are planned for each
      user-visible story, with tests-first task ordering preferred when
      feasible.
- [x] Clear module boundaries are documented, including ownership and allowed
      dependency direction.
- [x] The design is the simplest viable approach, and any breaking change
      includes migration or rollback notes.

## Project Structure

### Documentation (this feature)

```text
specs/002-profile-replies-notifications/
├── spec.md
├── plan.md
├── user-flows.md
├── frontend-requirements.md
├── openapi.yaml
├── data-model.md
├── quickstart.md
├── research.md
└── tasks.md
```

Round 2 planning order remains: user story intent and scope -> user flows ->
frontend requirements -> backend requirements -> contracts and data model ->
implementation sequencing -> tasks.

### Source Code (repository root)

```text
backend/
└── src/
    └── Postly.Api/
        ├── Features/
        │   ├── Auth/
        │   ├── Posts/
        │   ├── Profiles/
        │   ├── Timeline/
        │   ├── Shared/
        │   └── Notifications/      # new Round 2 feature area
        ├── Persistence/
        │   ├── Entities/
        │   ├── Configurations/
        │   ├── Migrations/
        │   └── AppDbContext.cs
        ├── Security/
        ├── wwwroot/
        └── Program.cs

frontend/
├── src/
│   ├── app/
│   │   ├── providers/
│   │   └── routes/
│   ├── features/
│   │   ├── auth/
│   │   ├── posts/
│   │   ├── profiles/
│   │   ├── timeline/
│   │   └── notifications/         # new Round 2 feature area
│   └── shared/
│       ├── api/
│       ├── components/
│       └── test/
└── tests/
    └── e2e/

backend/tests/
├── Postly.Api.UnitTests/
├── Postly.Api.IntegrationTests/
└── Postly.Api.ContractTests/
```

**Structure Decision**: Keep the current same-repo full-stack architecture and
feature-oriented folders. Round 2 extends `Profiles`, `Posts`, and `Timeline`,
adds `Notifications`, and reuses shared contracts/utilities instead of creating
new cross-cutting libraries or a separate frontend runtime.

## 1. User Story Intent and Scope

### US1 - Profile Editing and Avatar Replacement

**Intent**: Let a signed-in user change their own display name, bio, and
current avatar while keeping the rest of the product aligned to that updated
identity.

**In scope**

- Inline edit state on the signed-in user's own `/u/:username` profile route
- Display name validation using trimmed 1-50 character semantics
- Bio validation allowing blank or up to 160 characters
- Avatar replacement when Postly accepts the new image as the current avatar
- Avatar upload validation bounded to still JPEG/PNG input, deterministic
  normalization, and one-current-avatar replacement only
- Fallback to a generated default avatar when no custom avatar exists or a
  custom avatar later becomes unavailable
- Immediate identity reflection on:
  - the user's own profile
  - places that already show that user's identity on the home timeline
  - direct-post or conversation surfaces where that user's identity appears

**Out of scope**

- Re-specifying public profile reading
- Avatar history, galleries, cropping workflows, or non-avatar media handling
- Editing another user's profile

### US2 - Replies and Conversation-Oriented Direct-Post View

**Intent**: Turn the direct-post page into a conversation surface where users
can reply and authors can manage their own replies without changing baseline
post visibility rules.

**In scope**

- Create replies from the direct-post view when the target remains interactable
- Show the conversation target plus visible replies on `/posts/:postId`
- Edit and delete only replies authored by the signed-in user
- Preserve deleted-reply placeholders in the conversation
- Keep the route open with an unavailable-parent placeholder when the parent
  post becomes unavailable and visible replies still exist
- Apply existing post visibility and ownership rules to replies unless the spec
  explicitly changes them

**Out of scope**

- Re-specifying the baseline public direct-post permalink read behavior
- Nested reply trees beyond parent-post attachment
- Expanding edit/delete permissions beyond current ownership rules

### US3 - In-App Notifications

**Intent**: Surface follow, like, and reply activity to the affected user and
let them open the corresponding destination with a deterministic read/unread
lifecycle.

**In scope**

- Notification creation for follow, like, and reply events initiated by another
  user
- Unread/read distinction in the notifications list
- Destination resolution for both available and unavailable targets
- Read transition only for the selected notification whose destination is
  opened
- Empty-state handling for users with no notifications

**Out of scope**

- Email, push, or realtime delivery channels
- Bulk mark-read, archive, delete, or preference management

### US4 - Automatic Continuation Loading

**Intent**: Let users continue through long lists without losing already loaded
content or guessing whether a list is still loading, recoverable, or finished.

**In scope**

- Automatic continuation on home timeline, profile post lists, and conversation
  reply lists
- Explicit continuation trigger at the last currently visible item
- Explicit loading, retry, failure, and end-of-list states
- Stable visible-content preservation across failures and retries
- Reuse of existing cursor semantics where possible

**Out of scope**

- Replacing newest-first ordering or introducing live merge of newly-created
  content into already-scrolled older pages
- Offset pagination or manual “load more” as the primary continuation pattern

## 2. User Flows Suitable for Playwright

Detailed deterministic flows live in [user-flows.md](./user-flows.md). Each
story defines at least one primary e2e flow plus any required failure/recovery
flows.

## Story-to-Flow Mapping

| User Story | Flow ID(s) | Primary User Outcome | Frontend Responsibility | Backend Responsibility | Supporting Artifacts |
|------------|------------|----------------------|-------------------------|------------------------|----------------------|
| US1 | `UF-01`, `UF-02`, `UF-03` | User updates own identity and sees consistent profile/timeline/conversation reflection | Inline profile edit state, avatar picker, validation, cross-surface re-render, avatar fallback | Profile update validation, avatar persistence, identity projection refresh, authorization | `user-flows.md`, `frontend-requirements.md`, `openapi.yaml`, `data-model.md` |
| US2 | `UF-04`, `UF-05`, `UF-06`, `UF-07` | User replies in conversation, edits/deletes own reply, sees placeholder/unavailable-parent recovery | Conversation page states, reply composer, reuse of post-card edit/delete controls, placeholder rendering | Reply create/edit/delete rules, conversation query model, placeholder projections, notification trigger for replies | `user-flows.md`, `frontend-requirements.md`, `openapi.yaml`, `data-model.md` |
| US3 | `UF-08`, `UF-09`, `UF-10` | User distinguishes unread/read notifications and opens available or unavailable destinations correctly | Notifications route, row click/open handling, unread/read styling, destination navigation | Notification creation, list query, destination resolution, atomic selected-item read transition | `user-flows.md`, `frontend-requirements.md`, `openapi.yaml`, `data-model.md` |
| US4 | `UF-11`, `UF-12`, `UF-13` | User reaches continuation point, gets appended items, and can recover from failure or observe the final end state | Shared continuation contract, sentinel, loading/error/retry/end states, route-stable append behavior | Cursor reuse, continuation endpoints/read models, retry-safe pagination, no-duplicate ordering guarantees | `user-flows.md`, `frontend-requirements.md`, `openapi.yaml`, `data-model.md` |

## 3. Frontend Requirements

Normative UI/testability detail lives in
[frontend-requirements.md](./frontend-requirements.md). The plan-level frontend
responsibilities below stay technical and implementation-oriented.

### US1 Frontend Responsibilities

- Extend `frontend/src/features/profiles` to support inline edit mode on
  `/u/:username` only when `isSelf` is true.
- Preserve existing read mode for other users and for unauthenticated/public
  baseline behavior already defined by MVP.
- Validate and present server-side errors without discarding the in-progress
  draft when save fails.
- Use the same identity view model and rendering path for profile, timeline,
  and conversation surfaces so one successful save updates all currently visible
  in-scope identity surfaces.
- Render either `profile-avatar-image` or `profile-avatar-fallback`; never show
  a broken avatar state.
- Restrict `profile-avatar-input` to still JPEG/PNG selection, avoid manual
  crop UI, and switch to generated fallback immediately if a custom avatar
  image fails to load.
- Keep the same logical edit controls and profile test IDs across all own
  profile sessions.
- After successful avatar replacement, refresh visible profile, timeline, and
  conversation identity surfaces using the returned versioned avatar URL.

### US2 Frontend Responsibilities

- Extend `frontend/src/features/posts/DirectPostPage.tsx` into a conversation
  page that has distinct regions for target post, unavailable-parent
  placeholder, replies collection, and reply composer.
- Reuse existing post-card edit/delete controls and test IDs for reply cards so
  author actions remain consistent across top-level posts and replies.
- Render deleted replies as non-interactive placeholders that preserve their
  position in the conversation list.
- Hide or disable reply-authoring actions when the target becomes unavailable or
  the viewer lacks permission under existing rules.
- Preserve failed reply drafts where possible.

### US3 Frontend Responsibilities

- Add `frontend/src/features/notifications` and register `/notifications` in
  app routes and shared navigation.
- Render unread and read states distinctly without mutating read state on list
  load.
- Implement notification selection through one deterministic open flow that
  resolves destination metadata and then routes to:
  - a profile route when the profile destination is available
  - a conversation/direct-post route when post or conversation content is
    available
  - a dedicated unavailable destination state for that notification when the
    target no longer exists
- Update only the selected row to read after the open flow succeeds.

### US4 Frontend Responsibilities

- Add one shared continuation controller/hook used by timeline, profile posts,
  and conversation replies.
- Trigger continuation automatically when the last currently visible item is
  observed as the sentinel.
- Keep already visible items rendered while a continuation request is pending,
  fails, or is retried.
- Place retry UI near the failure point and keep the end-of-list state visible
  once no more items remain.
- Prevent duplicate appends and suppress overlapping continuation requests for
  the same collection state.
- Keep continuation retry scenarios deterministic in automated tests by using:
  - a shared frontend test helper that fails the next continuation fetch once
    in component tests
  - Playwright route interception that fails the first matching continuation
    request once in end-to-end tests

## 4. Backend Requirements

### US1 Backend Responsibilities

- Extend `Profiles` feature endpoints/application handlers to support updating
  the signed-in user's own display name and bio plus replacing the current
  avatar.
- Enforce:
  - trimmed display name length of 1-50 characters
  - bio length of 0-160 characters
  - owner-only authorization
  - avatar replacement accepted only when it becomes the user's current avatar
  - still JPEG/PNG input only
  - file size at or below 5 MB
  - minimum oriented source dimensions of 256x256
  - maximum decoded width/height of 4096
- Normalize accepted avatars by:
  - applying image orientation before validation and crop
  - center-cropping to a square
  - resizing to 512x512
  - flattening transparency onto white when needed
  - stripping metadata
  - storing the result as a high-quality JPEG and setting a new
    `AvatarUpdatedAtUtc`
- Preserve the previously saved profile on validation or processing failure.
- Project updated identity consistently through profile, timeline, and
  conversation read models.
- Return ProblemDetails-style validation and authorization outcomes consistent
  with MVP patterns.

### US2 Backend Responsibilities

- Extend `Posts` feature handlers so posts can optionally reference a parent
  post as a reply target.
- Keep the existing post body rules for replies and reject empty/invalid reply
  bodies with deterministic validation errors.
- Reject reply creation when the target post becomes unavailable before the
  mutation commits.
- Reuse existing update/delete endpoints for authored replies where possible,
  but enforce author-only permissions and preserve deleted-reply placeholders in
  conversation projections.
- Make conversation reads return:
  - target state (`available` or `unavailable`)
  - target post payload when available
  - visible replies under current rules
  - deleted reply placeholders
  - continuation cursor for additional replies
- Trigger reply notifications for the affected target owner when business rules
  allow and the actor is not the recipient.

### US3 Backend Responsibilities

- Add a `Notifications` feature area with list and open-destination behavior.
- Create notifications synchronously inside successful follow, like, and reply
  mutations when the actor and recipient differ.
- Expose notification summaries with actor identity, kind, timestamp, read
  state, and destination metadata.
- Resolve destination routing for both available and unavailable targets at open
  time so the user never lands on stale or unrelated content.
- Mark only the selected notification as read as part of the notification-open
  behavior; do not mutate read state when listing notifications.
- Keep unauthorized and not-found/unavailable handling aligned with existing
  ProblemDetails conventions.

### US4 Backend Responsibilities

- Reuse existing cursor semantics for timeline and profile pages and extend the
  same opaque cursor model to conversation replies.
- Ensure timeline, profile, and conversation reads all return stable ordering,
  append-safe pagination, and `nextCursor` semantics that distinguish:
  - initial empty list
  - continuation available
  - continuation exhausted
- Keep continuation requests retry-safe and free of duplicate or missing items
  when the same cursor is requested again after a failure.
- Preserve already visible items on the client by keeping page responses
  additive instead of replacement-oriented.

## 5. Contract and Data-Model Impact

### API Contract Impact

- `GET /api/profiles/{username}`, `GET /api/profiles/{username}/avatar`,
  `GET /api/posts/{postId}`, and `GET /api/posts/{postId}/replies` remain
  publicly readable baseline behavior in Round 2; the phase extends their
  payloads and signed-in affordances without adding authentication
  requirements for read access.
- `PATCH /api/profiles/me`
  - update display name and bio for the signed-in user
- `PUT /api/profiles/me/avatar`
  - replace the signed-in user's current avatar using multipart upload,
    validate accepted still-image constraints, and store normalized
    `image/jpeg` output
- `GET /api/profiles/{username}`
  - continue returning profile read mode, now with avatar metadata and profile
    post continuation support aligned to Round 2
- `GET /api/profiles/{username}/avatar`
  - serve the current normalized custom avatar as `image/jpeg` when it exists
- `GET /api/posts/{postId}`
  - evolve from direct-post read to conversation read with target state,
    visible replies, placeholders, and continuation cursor
- `POST /api/posts/{postId}/replies`
  - create a reply to the target post
- `GET /api/posts/{postId}/replies`
  - fetch continuation pages for conversation replies
- `PATCH /api/posts/{postId}`
  - continue handling authored post edits and now support authored reply edits
- `DELETE /api/posts/{postId}`
  - continue handling authored post deletes and now support authored reply
    soft-delete placeholder behavior
- `GET /api/notifications`
  - list the signed-in user's notifications
- `POST /api/notifications/{notificationId}/open`
  - atomically resolve destination metadata for the selected notification and
    mark only that notification as read

### Data Model Impact

- Extend `UserAccount` with current-avatar persistence metadata and payload
  needed for avatar replacement plus fallback projection support.
- Treat `AvatarUpdatedAtUtc` as the cache-busting version source for current
  custom avatar delivery.
- Extend `Post` with nullable `ReplyToPostId` and nullable `DeletedAtUtc` so
  replies remain first-class posts and deleted replies can stay visible as
  placeholders in conversations.
- Add `Notification` as a first-class persisted entity with recipient, actor,
  kind, target references, creation time, and `ReadAtUtc`.
- Keep `Session`, `Follow`, and `Like` schemas largely unchanged, but wire
  follow/like writes into notification creation.

### Validation and Error Outcomes

- Validation errors use ProblemDetails with stable field-level messages for:
  - invalid display name
  - invalid bio
  - invalid avatar replacement
  - unsupported avatar format
  - empty avatar file
  - oversized avatar file
  - avatar dimensions outside accepted bounds
- invalid reply body
- Authorization errors remain deterministic for:
  - editing another user's profile
  - editing/deleting another user's reply
- Unavailable/not-found outcomes stay explicit for:
  - reply target unavailable during submission
  - unavailable parent post in conversation reads
  - unavailable notification destinations at open time

## 6. Implementation Sequencing

### Sequence 1: Shared Foundations

1. Finalize Round 2 contracts and data model artifacts.
2. Add EF Core entities/configurations/migrations for avatar persistence,
   replies, and notifications.
3. Extend deterministic seed data for profile, reply, notification, and
   continuation coverage.
4. Add shared frontend continuation and avatar/identity contract types.

### Sequence 2: US1 Profile Editing

1. Backend profile update/avatar endpoints and validation
2. Frontend inline profile edit flow and avatar fallback rendering
3. Cross-surface identity projection refresh on profile, timeline, and
   conversation surfaces
4. Backend/frontend/Playwright coverage for valid and invalid profile updates

### Sequence 3: US2 Replies and Conversation

1. Reply persistence and conversation query model
2. Conversation-oriented direct-post response shape and reply continuation
3. Frontend conversation page, composer, author-only edit/delete reuse
4. Deleted-reply placeholder and unavailable-parent recovery states
5. Backend/frontend/Playwright coverage for create, edit, delete, and
   unavailable-target scenarios

### Sequence 4: US3 Notifications

1. Notification creation in follow/like/reply mutation paths
2. Notifications list and open-destination endpoint
3. Frontend notifications page and selected-item read transition
4. Available and unavailable destination coverage plus empty state coverage

### Sequence 5: US4 Automatic Continuation

1. Shared backend pagination reuse for conversation replies
2. Shared frontend continuation controller, state contract, and one-shot
   continuation failure test helper
3. Apply continuation behavior to timeline, profile posts, and conversation
   replies
4. Failure/retry/end-state tests across all three surfaces, using fetch-mock
   failure injection for frontend component tests and route interception for
   Playwright retry scenarios

### Sequence 6: Cross-Cutting Verification

1. Contract tests for new and changed APIs
2. Integration tests for profile edits, replies, notifications, and cursor
   behavior
3. Frontend component tests for new route states and shared hooks
4. Playwright flows for all primary and recovery scenarios
5. Run `/speckit.analyze` before `/speckit.implement`

## Traceability Notes

- US1 traceability:
  - flows: `UF-01`, `UF-02`, `UF-03`
  - frontend requirements: `FE-07`
  - backend/contracts: profile update + avatar endpoints
  - data model: `UserAccount`
- US2 traceability:
  - flows: `UF-04`, `UF-05`, `UF-06`, `UF-07`
  - frontend requirements: `FE-08`
  - backend/contracts: conversation and reply endpoints plus existing authored
    post mutation reuse
  - data model: `Post`
- US3 traceability:
  - flows: `UF-08`, `UF-09`, `UF-10`
  - frontend requirements: `FE-09`
  - backend/contracts: notifications list + open endpoint
  - data model: `Notification`
- US4 traceability:
  - flows: `UF-11`, `UF-12`, `UF-13`
  - frontend requirements: `FE-10`
  - backend/contracts: paginated timeline/profile/conversation reads
  - data model: cursor-bearing read models over `Post` and `Notification`

Supporting artifacts created or refreshed:
`user-flows.md`, `frontend-requirements.md`, `openapi.yaml`, `data-model.md`,
`quickstart.md`, and `research.md`.

## Complexity Tracking

No constitutional violations or justified complexity exceptions are required for
this plan. Deliberately excluded from Round 2: background jobs, websocket or
push delivery, offset pagination, avatar history/media management, separate
reply aggregates, and any redesign of already-established public profile/direct
post reading behavior.
