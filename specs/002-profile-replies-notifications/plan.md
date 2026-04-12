# Implementation Plan: Postly Round 2 - Profiles, Replies, Notifications

**Branch**: `002-profile-replies-notifications` | **Date**: 2026-04-13 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-profile-replies-notifications/spec.md`

## Summary

Extend the existing Postly MVP without re-specifying current public profile
read mode or public post permalinks. Round 2 adds four bounded capabilities on
top of the current same-repo architecture: signed-in profile editing with avatar
replacement, replies plus conversation-oriented direct-post views, in-app
notifications for follows/likes/replies, and automatic continuation loading for
timeline, profile, and conversation collections.

The implementation stays within the existing architecture: ASP.NET Core Minimal
API on .NET 10 remains the runtime entry point, SQLite and EF Core migrations
remain the persistence layer, the React + TypeScript SPA continues to be served
from backend `wwwroot`, and frontend/backend communication continues through the
typed shared API client. The design extends existing `Posts`, `Profiles`, and
`Timeline` modules, adds a focused `Notifications` module, reuses current
ProblemDetails and authorization patterns, and preserves current cursor
pagination semantics where possible.

## Technical Context

**Language/Version**: C# on .NET 10 (backend), TypeScript in strict mode (frontend)  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core with SQLite provider and migrations, ASP.NET Core authentication/cookie middleware, ASP.NET Core Identity password hasher, React, React Router, Vite, Vitest, React Testing Library, Playwright  
**Storage**: SQLite via EF Core migrations; persisted session records; additional Round 2 persistence for avatar state, reply relationships, and notifications  
**Testing**: xUnit unit tests, integration tests, and contract tests for backend; Vitest + React Testing Library for frontend; Playwright against the backend-hosted app  
**Target Platform**: Same-origin web application for modern desktop and mobile browsers, served by the ASP.NET Core backend  
**Project Type**: Same-repo full-stack web application  
**Interfaces/Contracts**: REST/JSON endpoints under the existing API surface, backend-served SPA routes (`/`, `/u/:username`, `/posts/:postId`, `/notifications`), cursor-based collection reads, typed frontend API client contracts under `frontend/src/shared/api`, binary avatar delivery via backend-owned endpoint  
**Error Handling Strategy**: Existing ProblemDetails-style responses with stable error codes, consistent validation mapping, and unchanged 400/401/403/404/409 behavior extended to Round 2 endpoints  
**UX Surfaces**: Own profile editing state, identity/avatar rendering on timeline/profile/conversation surfaces, conversation view with replies and unavailable placeholders, notifications list, automatic continuation states on timeline/profile/conversation  
**Performance Goals**: Preserve MVP expectations for newest-first collection reads while extending collection size through cursor pagination; keep avatar, notification, and reply operations within single-node SQLite limits and avoid introducing background infrastructure  
**Constraints**: Backend remains the runtime entry point and serves SPA assets from `backend/src/Postly.Api/wwwroot`; preserve feature/module boundaries; avoid unnecessary dependencies or new architectural layers; preserve current authorization and visibility rules unless the spec explicitly changes them; keep frontend/backend traceability by user story  
**Scale/Scope**: Single-node Round 2 feature phase for low-thousands of active users, focused on one-level replies, in-app notifications, and continuation loading on three existing content surfaces

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

For features with multiple user stories, use `/speckit.analyze` before
`/speckit.implement` unless a maintainer-approved exception is documented.

### Pre-Design Gate

- [x] `spec.md` is story-first, includes acceptance criteria and scope
      boundaries, and excludes UI implementation detail, API shape, database
      design, and test selectors.
      The current Round 2 spec is organized by user story, preserves product
      scope boundaries, and keeps implementation detail in planning artifacts
      rather than in the specification itself.
- [x] Every approved user story maps to at least one end-to-end user flow with
      route transitions, visible states, and verification intent.
      `user-flows.md` defines primary and important edge flows for US1-US4.
- [x] Required UI automation elements and stable `data-testid` hooks are
      defined before implementation, and repeated controls reuse the same test
      ID across surfaces.
      `frontend-requirements.md`, `user-flows.md`, and the automation contract
      below define stable hooks, and Round 1 post-card selectors are reused for
      reply surfaces rather than renamed.
- [x] Frontend responsibilities, backend responsibilities, API contracts,
      validation rules, error outcomes, and persistence changes trace back to
      the relevant user story and flow.
      Each story section below includes explicit frontend/backend work plus
      contract and data-model impact.
- [x] Required backend unit/integration or contract coverage, frontend
      component coverage, and Playwright coverage are planned for each
      user-visible story, with tests-first task ordering preferred when
      feasible.
      Sequencing for each story includes backend, frontend, and Playwright
      coverage as part of completion.
- [x] Clear module boundaries are documented, including ownership and allowed
      dependency direction.
      Round 2 extends existing feature folders and adds a single
      `Notifications` feature without changing dependency direction.
- [x] The design is the simplest viable approach, and any breaking change
      includes migration or rollback notes.
      The plan favors EF-backed entity extensions, reused cursor semantics, and
      no new services or infrastructure tiers.

### Post-Design Re-Check

- [x] `spec.md` remains story-first and technology-agnostic.
- [x] Every user story now maps to documented flows in [user-flows.md](./user-flows.md).
- [x] Stable UI hooks are defined in [frontend-requirements.md](./frontend-requirements.md) and summarized in the automation contract below.
- [x] API, persistence, validation, and visible-state changes are captured in
      [openapi.yaml](./openapi.yaml) and [data-model.md](./data-model.md).
- [x] [quickstart.md](./quickstart.md) documents the intended validation path
      and fixture expectations for Round 2.
- [x] The design stays within the current repo architecture and adds no
      unjustified complexity.

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

Round 2+ planning order MUST remain: user story -> user flow ->
frontend/backend requirements -> tasks -> implementation.

### Source Code (repository root)

```text
backend/
└── src/
    └── Postly.Api/
        ├── Features/
        │   ├── Auth/
        │   ├── Notifications/
        │   ├── Posts/
        │   ├── Profiles/
        │   ├── Timeline/
        │   └── Shared/
        ├── Persistence/
        │   ├── Configurations/
        │   ├── Entities/
        │   ├── Migrations/
        │   └── AppDbContext.cs
        ├── Security/
        ├── wwwroot/
        └── Program.cs

frontend/
├── src/
│   ├── app/
│   ├── features/
│   │   ├── auth/
│   │   ├── notifications/
│   │   ├── posts/
│   │   ├── profiles/
│   │   └── timeline/
│   └── shared/
│       ├── api/
│       ├── components/
│       ├── lib/
│       └── test/
└── tests/
    └── e2e/

backend/tests/
├── Postly.Api.UnitTests/
├── Postly.Api.IntegrationTests/
└── Postly.Api.ContractTests/
```

**Structure Decision**: Preserve the current backend-served SPA model and the
existing feature-oriented folder boundaries. Profile edits and avatar delivery
extend `Features/Profiles`, replies and conversation behavior extend
`Features/Posts`, automatic continuation updates `Features/Timeline` plus
shared frontend collection primitives, and notifications are introduced as one
new backend/frontend feature area rather than a new architectural layer.

## Story-to-Flow Mapping

| User Story | Flow ID(s) | Primary User Outcome | Frontend Responsibility | Backend Responsibility | Supporting Artifacts |
|------------|------------|----------------------|-------------------------|------------------------|----------------------|
| US1 | UF-01, UF-02 | User updates identity and avatar, then sees consistent identity across owned profile, home timeline, and conversation surfaces | Profile edit mode, avatar replacement UI contract, cross-surface identity refresh | Profile mutation validation, avatar persistence/delivery, read-model projection updates | `user-flows.md`, `frontend-requirements.md`, `openapi.yaml`, `data-model.md`, `quickstart.md` |
| US2 | UF-03, UF-04, UF-05 | User replies to a post, manages own replies, and sees a conversation-oriented direct-post surface | Conversation page, reply composer, reply edit/delete states, placeholder rendering | Reply create/edit/delete rules, conversation read model, authorization and availability handling | `user-flows.md`, `frontend-requirements.md`, `openapi.yaml`, `data-model.md`, `quickstart.md` |
| US3 | UF-06, UF-07 | User reviews notifications and reaches the correct destination with correct read/unread behavior | Notifications route, destination navigation, read-state transitions | Notification generation/read lifecycle, destination resolution, unavailable-target handling | `user-flows.md`, `frontend-requirements.md`, `openapi.yaml`, `data-model.md`, `quickstart.md` |
| US4 | UF-08, UF-09, UF-10 | User continues loading more timeline/profile/conversation content with explicit loading, error, retry, and end states | Automatic continuation observers/states, reusable collection state contract | Cursor endpoint reuse or extension, stable continuation semantics for each collection | `user-flows.md`, `frontend-requirements.md`, `openapi.yaml`, `data-model.md`, `quickstart.md` |

## User Flows

### Flow UF-01 - Edit profile identity and avatar (Story: US1)

- **Start Route**: `/u/:currentUsername`
- **Trigger**: Signed-in user enters profile edit mode on their own profile
- **Route Transitions**: `/u/:currentUsername` -> same route in edit state -> same route in saved state
- **Visible States**: default profile, edit form, validation errors, pending save, successful refreshed identity
- **Verification Intent**: Prove valid edits succeed, invalid edits block save, and updated identity appears consistently on profile, home timeline, and conversation surfaces

### Flow UF-03 - Reply and manage own reply (Story: US2)

- **Start Route**: `/posts/:postId`
- **Trigger**: Signed-in user submits a reply to the conversation target
- **Route Transitions**: `/posts/:postId` -> same route with pending composer -> same route with appended reply -> same route with reply edit/delete states
- **Visible States**: available target, composer validation, pending submit, reply list success, deleted reply placeholder
- **Verification Intent**: Prove reply creation, author-only mutation controls, placeholder behavior for deleted replies, and consistent post-card contract reuse

### Flow UF-06 - Open notification destination (Story: US3)

- **Start Route**: `/notifications`
- **Trigger**: Signed-in user selects one unread notification
- **Route Transitions**: `/notifications` -> destination route (`/u/:username` or `/posts/:postId`) -> optional return to `/notifications`
- **Visible States**: notifications list, unread/read indicators, available destination, unavailable destination placeholder
- **Verification Intent**: Prove list-only viewing does not mark read, destination open does mark only the opened item, and unavailable targets still resolve to a deterministic destination state

### Flow UF-08 - Continue loading content automatically (Story: US4)

- **Start Route**: `/`, `/u/:username`, or `/posts/:postId`
- **Trigger**: User scrolls until the last currently visible item becomes the continuation point for that surface
- **Route Transitions**: same route only
- **Visible States**: initial page, continuation loading, continuation failure with retry, end-of-list state
- **Verification Intent**: Prove automatic continuation starts at the defined continuation point, preserves visible content on failure, and exposes explicit retry and end-state affordances

Full step-by-step flows appear in [user-flows.md](./user-flows.md).

## UI Automation Contract

| Surface | Required Element | Purpose | Stable Selector / `data-testid` | Notes |
|---------|------------------|---------|----------------------------------|-------|
| Own profile | Edit entry action | Enter profile edit mode | `profile-edit-button` | Own profile only |
| Own profile | Edit form shell | Scope profile mutations | `profile-edit-form` | Visible only in edit state |
| Own profile | Display name input | Validate display name edits | `profile-display-name-input` | Reused only for this form |
| Own profile | Bio input | Validate bio edits | `profile-bio-input` | Reused only for this form |
| Own profile | Avatar chooser | Replace avatar | `profile-avatar-input` | Product-facing file chooser hook |
| Own profile | Save action | Submit profile changes | `profile-save-button` | Save remains route-local |
| Shared identity surfaces | Profile avatar | Assert avatar/fallback rendering | `profile-avatar-image`, `profile-avatar-fallback`, `post-avatar-<postId>` | `post-avatar-<postId>` reuses post-card pattern on timeline/conversation |
| Conversation | Reply composer | Create reply | `reply-composer`, `reply-composer-input`, `reply-submit-button` | Only on available conversations |
| Conversation | Target/unavailable states | Assert conversation availability | `conversation-target`, `conversation-target-unavailable` | Conversation route remains stable |
| Shared post surfaces | Edit/delete reply controls | Reuse existing post ownership controls | `post-edit-button-<postId>`, `post-delete-button-<postId>` | Same logical control across posts and replies |
| Notifications | Notifications list | Assert notification visibility and read state | `notifications-list`, `notification-item-<notificationId>`, `notification-unread-indicator-<notificationId>`, `notification-read-indicator-<notificationId>` | Applies only to `/notifications` |
| Collection surfaces | Continuation states | Assert automatic loading lifecycle | `collection-continuation-sentinel`, `collection-continuation-loading`, `collection-continuation-error`, `collection-continuation-retry`, `collection-end-state` | Shared contract reused on home/profile/conversation |

Use the same logical `data-testid` for the same control across all surfaces
where it appears.

## Frontend Responsibilities

### User Story 1

- Extend the existing profile route so the signed-in user can enter an own-only
  edit state on `/u/:currentUsername`.
- Add form state management for display name, bio, and avatar replacement while
  preserving route-level loading/error behavior already used on profiles.
- Refresh identity projections on save so the updated display name, bio, and
  avatar appear on own profile, home timeline author surfaces, and
  direct-post/conversation author surfaces without a full app reload.
- Reuse the shared API client and existing avatar rendering patterns, falling
  back to the generated default avatar when no custom avatar is present.

### User Story 2

- Evolve the direct-post page into a conversation route that renders the target
  post plus reply list and reply composer states.
- Reuse the shared post card contract for replies so author, timestamp,
  permalink, edit, delete, and avatar hooks stay consistent where applicable.
- Render deleted replies and unavailable parent states as explicit placeholders
  rather than collapsing the route or silently removing conversational context.
- Preserve author-only mutation controls and current visibility/ownership rules.

### User Story 3

- Add a dedicated `/notifications` route and navigation entry inside the
  existing authenticated shell.
- Render unread/read state explicitly and keep list-only viewing side-effect
  free.
- Navigate to the destination associated with a notification and transition the
  selected notification to read only after destination open succeeds from the
  product perspective.
- Surface unavailable-target outcomes with a deterministic destination state
  rather than a silent failure.

### User Story 4

- Replace manual-only continuation on timeline/profile with an automatic
  continuation pattern and apply the same pattern to conversation replies.
- Keep loading, empty, error, retry, and end-of-list states explicit and
  visually consistent across all three surfaces.
- Preserve visible content during continuation failures and allow retry from
  the same route without resetting already loaded content.
- Centralize the shared continuation hook/component contract under `frontend/src/shared`
  where practical, without introducing a new global state dependency.

## Backend Responsibilities

### User Story 1

- Add a profile update endpoint for display name and bio with explicit trimmed
  validation rules already approved in the spec.
- Add avatar replacement support with a minimal persistence and delivery model
  appropriate for SQLite-backed MVP infrastructure.
- Extend profile, timeline, and conversation read models so identity surfaces
  resolve the current avatar and updated display name consistently.
- Preserve current authorization boundaries so only the signed-in owner can
  mutate their own profile.

### User Story 2

- Extend the post model and handlers to support replies tied to a target post
  while preserving current ownership and visibility rules.
- Return a conversation-oriented read model for `/posts/:postId`, including the
  target post state, reply list, pagination cursor, and unavailable/deleted
  placeholder behavior required by the spec.
- Apply author-only edit/delete authorization to replies using the same
  semantics as existing post mutations.
- Preserve current ProblemDetails mapping for invalid, unauthorized, forbidden,
  conflict, and not-found outcomes.

### User Story 3

- Introduce notification persistence and generation for follow, like, and reply
  events with actor, recipient, target, and read-state tracking.
- Expose a notifications read endpoint and a focused state transition endpoint
  for marking a notification as read after destination open.
- Resolve each notification to a stable destination contract even when the
  original target later becomes unavailable.
- Avoid adding background infrastructure; notification creation occurs inside
  the existing request pipeline where the triggering mutation succeeds.

### User Story 4

- Reuse existing cursor pagination semantics for timeline and profile reads and
  extend the same semantics to conversation replies.
- Keep limit/cursor validation and response contracts consistent with existing
  collection endpoints.
- Ensure continuation reads are deterministic, newest-first, and safe to retry.
- Preserve existing backend route ownership: timeline logic stays in
  `Features/Timeline`, profile post collections stay in `Features/Profiles`,
  and conversation replies stay in `Features/Posts`.

## User Story Plans

### US1 Profile Editing and Avatar Management

#### 1. User Story Intent and Scope

Allow a signed-in user to update their own display name, bio, and profile
avatar without changing current public profile read behavior. The story covers
mutation of own identity and consistent reflection of that identity on the own
profile route, home timeline identity surfaces, and direct-post/conversation
identity surfaces. It does not expand media support beyond avatar replacement.

#### 2. User Flow Suitable for Playwright

- **Primary flow**: Open own profile, edit display name/bio/avatar, save
  successfully, then verify updated identity on own profile, the home timeline,
  and a conversation route that shows the same user.
- **Failure/edge flows**:
  - Submit invalid display name or bio and verify inline validation with no
    persisted change.
  - Attempt an invalid avatar replacement and verify the current avatar/fallback
    remains unchanged.

#### 3. Frontend Requirements

- Own profile header must expose `profile-edit-button` only when `isSelf`.
- Edit mode must expose `profile-edit-form`, `profile-display-name-input`,
  `profile-bio-input`, `profile-avatar-input`, `profile-save-button`,
  `profile-cancel-button`, and `profile-form-status`.
- The saved identity must update shared author renderers used by timeline and
  conversation post cards.
- Existing default avatar fallback behavior must remain available when the user
  has no custom avatar or the avatar has been cleared/replaced by the current
  Round 2 rules.

#### 4. Backend Requirements

- Add `PATCH /profiles/me` for display name and bio updates.
- Add `PUT /profiles/me/avatar` for avatar replacement and backend-owned avatar
  delivery/read-model projection support.
- Extend profile and post summary contracts with avatar metadata needed by the
  frontend, while keeping the frontend on typed shared contracts.
- Enforce owner-only access and existing validation/error mapping.

#### 5. Contract/Data Model Impact

- `UserAccount` gains persisted avatar state.
- Shared read models gain avatar URL/presence metadata.
- `openapi.yaml` adds profile mutation and avatar endpoints and updates profile
  and post summary schemas.

#### 6. Implementation Sequencing

1. Add backend validation and contract tests for profile update and avatar
   replacement.
2. Add persistence changes and read-model projection updates.
3. Add frontend profile edit state and shared identity refresh behavior.
4. Add frontend component tests for profile edit success/failure and avatar
   fallback behavior.
5. Add Playwright coverage for valid edit and invalid edit flows.

### US2 Replies and Threaded Conversation View

#### 1. User Story Intent and Scope

Allow a signed-in user to reply to a post and use the direct-post route as a
conversation view that shows the target post and its replies. Authors may edit
or delete only their own replies, and reply visibility/ownership remains
consistent with current post rules.

#### 2. User Flow Suitable for Playwright

- **Primary flow**: Open a known post, create a reply, verify it appears in the
  conversation, edit it, then delete it and verify a non-interactive placeholder.
- **Failure/edge flows**:
  - Open a conversation whose parent is unavailable and verify the route stays
    open with an unavailable-parent placeholder plus any still-visible replies.
  - Confirm non-authors never see edit/delete controls for replies they do not own.

#### 3. Frontend Requirements

- `/posts/:postId` must become a conversation page with target region, reply
  composer, reply collection, and unavailable/placeholder states.
- Reply cards reuse shared post-card hooks where the same logical control
  already exists.
- Deleted replies render as non-interactive placeholders inside the conversation.
- Conversation pagination/continuation hooks match the shared collection
  contract used by home/profile.

#### 4. Backend Requirements

- Add reply creation and reply list endpoints or equivalent expanded
  conversation read support.
- Extend post mutation rules to handle replies with author-only edit/delete.
- Add unavailable parent and deleted reply projection behavior to the
  conversation read model.
- Reuse existing ProblemDetails codes and visibility rules.

#### 5. Contract/Data Model Impact

- `Post` gains nullable parent reference and soft-delete support for
  conversation-safe placeholder behavior.
- Conversation response contract adds target availability and reply collection
  metadata.
- Shared `PostSummary` gains reply-related state fields used only where needed.

#### 6. Implementation Sequencing

1. Add backend entity/configuration changes and migration for replies.
2. Add unit/integration/contract coverage for reply create/edit/delete and
   unavailable conversation cases.
3. Build conversation page and reply composer/edit/delete states.
4. Add frontend tests for reply ownership controls and placeholder rendering.
5. Add Playwright coverage for happy-path and unavailable-parent flows.

### US3 In-App Notifications

#### 1. User Story Intent and Scope

Allow a signed-in user to review notifications for follows, likes, and replies,
distinguish unread from read items, and navigate to the relevant destination.
Read-state changes must follow the clarified lifecycle in the spec.

#### 2. User Flow Suitable for Playwright

- **Primary flow**: Open `/notifications`, verify unread state, open one
  notification, land on the correct destination, and verify only that
  notification becomes read.
- **Failure/edge flows**:
  - View the list without opening a destination and verify nothing becomes read.
  - Open a notification whose target later became unavailable and verify the
    app lands on a deterministic unavailable destination while marking that item
    read.

#### 3. Frontend Requirements

- Add `nav-notifications-link` to the authenticated shell.
- Add a notifications screen with list, empty, loading, error, and retry states.
- Render notification items with stable hooks for read and unread indicators.
- Handle destination navigation plus post-open read transition without silently
  mutating other notifications.

#### 4. Backend Requirements

- Add notification persistence and generation on follow/like/reply success.
- Add notifications list endpoint and mark-read endpoint.
- Return destination contracts that identify the target route and target kind.
- Preserve current auth, ProblemDetails, and no-extra-infrastructure posture.

#### 5. Contract/Data Model Impact

- New `Notification` entity with recipient, actor, kind, destination, and
  `ReadAtUtc`.
- New notification list and mark-read contracts.
- Existing follow/like/reply write paths gain notification side effects.

#### 6. Implementation Sequencing

1. Add notification entity, migration, and generation rules.
2. Add contract/integration coverage for follow, like, and reply notification creation plus read transitions.
3. Add `/notifications` route and typed client support.
4. Add frontend component tests for read/unread and unavailable-target states.
5. Add Playwright coverage for available-destination and unavailable-destination flows.

### US4 Frontend Pagination / Infinite Scroll

#### 1. User Story Intent and Scope

Allow the user to continue loading additional content on the home timeline,
profile post lists, and conversation views without changing current collection
ownership or backend entry-point architecture. This story only covers frontend
continuation behavior and aligned backend cursor semantics, not a broader feed
ranking change.

#### 2. User Flow Suitable for Playwright

- **Primary flow**: Scroll through a seeded collection until the continuation
  point is reached, observe automatic loading, and verify new items append
  without replacing existing content.
- **Failure/edge flows**:
  - Simulate a continuation failure and verify visible content remains, a retry
    action appears, and retry resumes from the same route.
  - Reach the end of a seeded collection and verify an explicit end state.

#### 3. Frontend Requirements

- Home timeline, profile post list, and conversation reply list must each
  define the continuation point as the last currently visible item on that
  surface, matching the clarified spec.
- A shared continuation contract must define loading, failure, retry, and end
  states using stable hooks.
- Existing manual load-more affordances may remain only if they do not replace
  the required automatic continuation behavior.

#### 4. Backend Requirements

- Reuse current cursor semantics for timeline and profile collections.
- Add equivalent reply cursor semantics for conversation replies.
- Preserve deterministic ordering and safe retries for repeated continuation
  requests.

#### 5. Contract/Data Model Impact

- `TimelineResponse`, `ProfileResponse`, and `ConversationResponse` each expose
  `nextCursor` using consistent semantics.
- No new entity type is required for continuation; the primary impact is on
  collection contracts and query behavior.

#### 6. Implementation Sequencing

1. Normalize collection contract expectations and backend cursor tests.
2. Introduce shared frontend continuation utilities and state contract.
3. Apply the contract to timeline, profile, and conversation surfaces.
4. Add frontend tests for loading/retry/end states on each surface.
5. Add Playwright coverage for automatic continuation and retry behavior.

## Traceability Notes

- [user-flows.md](./user-flows.md) provides the browser-automation-oriented
  route transitions, visible states, and assertion intent required by the
  constitution.
- [frontend-requirements.md](./frontend-requirements.md) defines screen
  ownership, UI elements, state variants, and required `data-testid` hooks.
- [openapi.yaml](./openapi.yaml) captures the new and changed backend
  contracts for profiles, conversations, replies, notifications, and
  cursor-backed collections.
- [data-model.md](./data-model.md) captures avatar persistence, reply
  relationships, notification lifecycle fields, validation, and indexing.
- [quickstart.md](./quickstart.md) captures Round 2 setup, fixture
  expectations, and the intended validation path.
- Because this feature contains multiple user stories, `/speckit.analyze`
  should run before `/speckit.implement`.

## Complexity Tracking

No constitutional violations or unjustified complexity are required for this
plan.
