# Tasks: Postly Microblog MVP

**Input**: Design documents from `/specs/001-microblog-mvp/`  
**Prerequisites**: [plan.md](/home/mental/projects/Postly/specs/001-microblog-mvp/plan.md), [spec.md](/home/mental/projects/Postly/specs/001-microblog-mvp/spec.md), [research.md](/home/mental/projects/Postly/specs/001-microblog-mvp/research.md), [data-model.md](/home/mental/projects/Postly/specs/001-microblog-mvp/data-model.md), [openapi.yaml](/home/mental/projects/Postly/specs/001-microblog-mvp/contracts/openapi.yaml), [quickstart.md](/home/mental/projects/Postly/specs/001-microblog-mvp/quickstart.md)

**Tests**: Automated tests are required for every user story. Each story includes backend contract/integration coverage, frontend component coverage, and a critical Playwright flow where the spec or plan makes the journey user-critical.

**Organization**: Tasks are grouped by end-to-end slice so each phase produces a reviewable increment with explicit contracts, validation, predictable errors, and consistent UI states.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize the same-repo backend/frontend workspace and baseline quality tooling.

- [ ] T001 Create the backend solution and executable/test projects in `/home/mental/projects/Postly/backend/Postly.sln`, `/home/mental/projects/Postly/backend/src/Postly.Api/Postly.Api.csproj`, `/home/mental/projects/Postly/backend/tests/Postly.Api.UnitTests/Postly.Api.UnitTests.csproj`, `/home/mental/projects/Postly/backend/tests/Postly.Api.IntegrationTests/Postly.Api.IntegrationTests.csproj`, and `/home/mental/projects/Postly/backend/tests/Postly.Api.ContractTests/Postly.Api.ContractTests.csproj`
- [ ] T002 [P] Create the React + Vite frontend workspace with strict TypeScript in `/home/mental/projects/Postly/frontend/package.json`, `/home/mental/projects/Postly/frontend/tsconfig.json`, `/home/mental/projects/Postly/frontend/vite.config.ts`, and `/home/mental/projects/Postly/frontend/src/main.tsx`
- [ ] T003 [P] Configure repository-wide formatting, linting, and ignore defaults in `/home/mental/projects/Postly/.gitignore`, `/home/mental/projects/Postly/.editorconfig`, `/home/mental/projects/Postly/backend/Directory.Build.props`, and `/home/mental/projects/Postly/frontend/eslint.config.js`
- [ ] T004 [P] Add baseline automated test harnesses in `/home/mental/projects/Postly/backend/tests/Postly.Api.IntegrationTests/TestWebAppFactory.cs`, `/home/mental/projects/Postly/frontend/vitest.config.ts`, `/home/mental/projects/Postly/frontend/src/shared/test/setup.ts`, and `/home/mental/projects/Postly/frontend/playwright.config.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the shared architecture and boundaries that every MVP slice depends on.

**⚠️ CRITICAL**: No user story work should begin until this phase is complete.

- [ ] T005 Create the backend host bootstrap with service registration, route-group registration, SQLite wiring, ProblemDetails, request logging, and rate-limit hooks in `/home/mental/projects/Postly/backend/src/Postly.Api/Program.cs`
- [ ] T006 [P] Implement shared backend error codes and ProblemDetails mapping in `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Shared/Errors/ErrorCodes.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Shared/Errors/DomainProblem.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Shared/Errors/ProblemDetailsMapping.cs`
- [ ] T007 [P] Implement shared request-validation helpers and endpoint filters in `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Shared/Validation/ValidationHelpers.cs` and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Shared/Validation/EndpointValidationFilter.cs`
- [ ] T008 [P] Implement cookie-session authentication infrastructure in `/home/mental/projects/Postly/backend/src/Postly.Api/Security/AuthExtensions.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Security/SessionTokenHasher.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Security/CurrentUserAccessor.cs`
- [ ] T009 [P] Create the frontend app providers, router, and protected-shell baseline in `/home/mental/projects/Postly/frontend/src/app/providers/AppProviders.tsx`, `/home/mental/projects/Postly/frontend/src/app/routes/AppRouter.tsx`, `/home/mental/projects/Postly/frontend/src/app/routes/ProtectedRoute.tsx`, and `/home/mental/projects/Postly/frontend/src/app/shell/AppShell.tsx`
- [ ] T010 [P] Create the typed HTTP client boundary and frontend ProblemDetails mapper in `/home/mental/projects/Postly/frontend/src/shared/api/http.ts`, `/home/mental/projects/Postly/frontend/src/shared/api/client.ts`, and `/home/mental/projects/Postly/frontend/src/shared/lib/problemDetails.ts`
- [ ] T011 [P] Create reusable responsive async-state and message primitives in `/home/mental/projects/Postly/frontend/src/shared/components/AsyncState.tsx`, `/home/mental/projects/Postly/frontend/src/shared/components/PageMessage.tsx`, `/home/mental/projects/Postly/frontend/src/shared/styles/tokens.css`, and `/home/mental/projects/Postly/frontend/src/shared/styles/app.css`
- [ ] T012 [P] Create the base EF Core context, design-time factory, and cursor paging contract in `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/AppDbContext.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/DesignTimeDbContextFactory.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Shared/Contracts/CursorPage.cs`

**Checkpoint**: Backend and frontend boundaries, shared validation/error handling, auth plumbing, and test harnesses are ready for end-to-end feature slices.

---

## Phase 3: User Story 1 - Join and Publish (Priority: P1) 🎯 MVP

**Goal**: Let a new user sign up, sign in, reach protected content, publish short text posts, manage only their own posts, and sign out safely.

**Independent Test**: A first-time visitor can create an account, sign in, publish a post, edit it, delete it, sign out, and sign back in to confirm the account still exists and protected routes stay protected.

### Tests for User Story 1 (REQUIRED)

- [ ] T013 [P] [US1] Add contract tests for signup, signin, signout, session bootstrap, timeline, and post mutation endpoints in `/home/mental/projects/Postly/backend/tests/Postly.Api.ContractTests/AuthContractsTests.cs` and `/home/mental/projects/Postly/backend/tests/Postly.Api.ContractTests/PostsContractsTests.cs`
- [ ] T014 [P] [US1] Add backend integration tests for signup/signin/signout, ownership enforcement, and post create/edit/delete flows in `/home/mental/projects/Postly/backend/tests/Postly.Api.IntegrationTests/AuthFlowTests.cs` and `/home/mental/projects/Postly/backend/tests/Postly.Api.IntegrationTests/PostOwnershipTests.cs`
- [ ] T015 [P] [US1] Add frontend component tests for signup/signin validation, generic signin failure, and protected-route redirects in `/home/mental/projects/Postly/frontend/src/features/auth/AuthForms.test.tsx` and `/home/mental/projects/Postly/frontend/src/app/routes/ProtectedRoute.test.tsx`
- [ ] T016 [P] [US1] Add frontend component tests for composer, author-only post controls, and self-profile post states in `/home/mental/projects/Postly/frontend/src/features/posts/PostComposer.test.tsx` and `/home/mental/projects/Postly/frontend/src/features/profiles/ProfilePage.test.tsx`
- [ ] T017 [P] [US1] Add Playwright coverage for signup, redirect-after-signin, compose, edit, delete, signout, and sign-back-in in `/home/mental/projects/Postly/frontend/tests/e2e/auth-posts.spec.ts`

### Implementation for User Story 1

- [ ] T018 [US1] Add the initial auth/post schema and EF migration for users, sessions, and posts in `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/AppDbContext.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/Configurations/UserAccountConfiguration.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/Configurations/SessionConfiguration.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/Configurations/PostConfiguration.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/Migrations/InitialAuthPosts.cs`
- [ ] T019 [US1] Implement signup, signin, signout, and session bootstrap handlers with boundary validation and generic auth errors in `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Auth/Application/SignupHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Auth/Application/SigninHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Auth/Application/SignoutHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Auth/Application/GetSessionHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Auth/Contracts/SignupRequest.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Auth/Contracts/SigninRequest.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Auth/Contracts/SessionResponse.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Auth/Endpoints/AuthEndpoints.cs`
- [ ] T020 [US1] Implement the protected self-profile and own-timeline read paths in `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Profiles/Application/GetProfileHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Profiles/Application/GetProfilePostsHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Profiles/Contracts/ProfileResponse.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Profiles/Endpoints/ProfileEndpoints.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Timeline/Application/GetTimelineHandler.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Timeline/Endpoints/TimelineEndpoints.cs`
- [ ] T021 [US1] Implement create, edit, and delete post handlers with ownership enforcement, length validation, and immediate timeline/profile updates in `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Application/CreatePostHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Application/UpdatePostHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Application/DeletePostHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Contracts/CreatePostRequest.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Contracts/UpdatePostRequest.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Contracts/PostSummaryResponse.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Endpoints/PostEndpoints.cs`
- [ ] T022 [US1] Implement auth API modules, session bootstrap, and redirect-aware account pages in `/home/mental/projects/Postly/frontend/src/shared/api/authApi.ts`, `/home/mental/projects/Postly/frontend/src/features/auth/useSession.ts`, `/home/mental/projects/Postly/frontend/src/features/auth/SigninPage.tsx`, and `/home/mental/projects/Postly/frontend/src/features/auth/SignupPage.tsx`
- [ ] T023 [US1] Implement the protected home timeline, composer, and author-only post actions with loading, empty, success, and error states in `/home/mental/projects/Postly/frontend/src/shared/api/postsApi.ts`, `/home/mental/projects/Postly/frontend/src/shared/api/timelineApi.ts`, `/home/mental/projects/Postly/frontend/src/features/timeline/HomeTimelinePage.tsx`, `/home/mental/projects/Postly/frontend/src/features/timeline/useTimeline.ts`, `/home/mental/projects/Postly/frontend/src/features/posts/PostComposer.tsx`, and `/home/mental/projects/Postly/frontend/src/features/posts/PostCard.tsx`
- [ ] T024 [US1] Implement the self-profile route and explicit "Your profile" presentation for managing authored posts in `/home/mental/projects/Postly/frontend/src/shared/api/profilesApi.ts`, `/home/mental/projects/Postly/frontend/src/features/profiles/ProfilePage.tsx`, and `/home/mental/projects/Postly/frontend/src/app/routes/AppRouter.tsx`

**Checkpoint**: User Story 1 is a complete, demoable MVP slice that proves the architecture end to end.

---

## Phase 4: User Story 2 - Build a Personalized Timeline (Priority: P2)

**Goal**: Let a signed-in user visit another profile from visible author links, follow or unfollow that user, and see the home timeline update to newest-first self-plus-followed content.

**Independent Test**: A signed-in user can open another user profile from a visible author identity, follow that user, see followed posts appear in the home timeline, and unfollow to remove that content again.

### Tests for User Story 2 (REQUIRED)

- [ ] T025 [P] [US2] Add contract tests for profile reads, follow/unfollow, and paginated timeline responses in `/home/mental/projects/Postly/backend/tests/Postly.Api.ContractTests/ProfileContractsTests.cs` and `/home/mental/projects/Postly/backend/tests/Postly.Api.ContractTests/TimelineContractsTests.cs`
- [ ] T026 [P] [US2] Add backend integration tests for self-follow rejection, follow/unfollow state transitions, and newest-first self-plus-followed timeline composition in `/home/mental/projects/Postly/backend/tests/Postly.Api.IntegrationTests/FollowFlowTests.cs` and `/home/mental/projects/Postly/backend/tests/Postly.Api.IntegrationTests/TimelineCompositionTests.cs`
- [ ] T027 [P] [US2] Add frontend component tests for other-profile presentation, follow button states, and zero-follow timeline messaging in `/home/mental/projects/Postly/frontend/src/features/profiles/ProfilePage.test.tsx` and `/home/mental/projects/Postly/frontend/src/features/timeline/HomeTimelinePage.test.tsx`
- [ ] T028 [P] [US2] Add Playwright coverage for author-link navigation, follow/unfollow, and timeline recomposition in `/home/mental/projects/Postly/frontend/tests/e2e/follow-timeline.spec.ts`

### Implementation for User Story 2

- [ ] T029 [US2] Add the follow relationship schema, indexes, and EF migration in `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/AppDbContext.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/Configurations/FollowConfiguration.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/Migrations/AddFollows.cs`
- [ ] T030 [US2] Implement profile relationship reads and follow/unfollow handlers with self-follow prevention and predictable conflict handling in `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Profiles/Application/GetProfileHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Profiles/Application/FollowUserHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Profiles/Application/UnfollowUserHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Profiles/Contracts/ProfileRelationshipState.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Profiles/Endpoints/ProfileEndpoints.cs`
- [ ] T031 [US2] Implement newest-first timeline composition, cursor pagination, and zero-follow or zero-post query outcomes in `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Timeline/Application/GetTimelineHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Timeline/Contracts/TimelinePageResponse.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Timeline/Endpoints/TimelineEndpoints.cs`
- [ ] T032 [US2] Implement profile API integration and author-link navigation without search or recommendation features in `/home/mental/projects/Postly/frontend/src/shared/api/profilesApi.ts`, `/home/mental/projects/Postly/frontend/src/features/posts/PostAuthorLink.tsx`, and `/home/mental/projects/Postly/frontend/src/features/profiles/ProfilePage.tsx`
- [ ] T033 [US2] Implement follow/unfollow interactions, relationship counts, and own-vs-other profile treatment in `/home/mental/projects/Postly/frontend/src/features/profiles/ProfileHeader.tsx`, `/home/mental/projects/Postly/frontend/src/features/profiles/useProfile.ts`, and `/home/mental/projects/Postly/frontend/src/features/profiles/ProfilePage.tsx`
- [ ] T034 [US2] Implement zero-follow, zero-post, retry, and reduced-content timeline states with responsive guidance in `/home/mental/projects/Postly/frontend/src/features/timeline/HomeTimelinePage.tsx`, `/home/mental/projects/Postly/frontend/src/features/timeline/useTimeline.ts`, and `/home/mental/projects/Postly/frontend/src/shared/components/PageMessage.tsx`

**Checkpoint**: User Stories 1 and 2 work together while User Story 2 remains independently testable through profile, follow, and timeline composition flows.

---

## Phase 5: User Story 3 - React to Posts and View Profiles (Priority: P3)

**Goal**: Let signed-in users like or unlike posts on every visible surface, open direct post links safely, and keep profile and ownership presentation consistent.

**Independent Test**: A signed-in user can like and unlike posts on the home timeline, profile, and direct-post view, see aggregate like counts update, and get redirected through signin before protected direct-post content is shown.

### Tests for User Story 3 (REQUIRED)

- [ ] T035 [P] [US3] Add contract tests for direct-post read and like/unlike endpoints in `/home/mental/projects/Postly/backend/tests/Postly.Api.ContractTests/PostDetailContractsTests.cs` and `/home/mental/projects/Postly/backend/tests/Postly.Api.ContractTests/LikeContractsTests.cs`
- [ ] T036 [P] [US3] Add backend integration tests for like idempotency, unavailable direct-post behavior, and cross-surface ownership consistency in `/home/mental/projects/Postly/backend/tests/Postly.Api.IntegrationTests/LikeFlowTests.cs` and `/home/mental/projects/Postly/backend/tests/Postly.Api.IntegrationTests/DirectPostTests.cs`
- [ ] T037 [P] [US3] Add frontend component tests for like controls, direct-post unavailable states, and profile-detail rendering in `/home/mental/projects/Postly/frontend/src/features/posts/LikeButton.test.tsx`, `/home/mental/projects/Postly/frontend/src/features/posts/PostDetailPage.test.tsx`, and `/home/mental/projects/Postly/frontend/src/features/profiles/ProfileHeader.test.tsx`
- [ ] T038 [P] [US3] Add Playwright coverage for like/unlike across timeline, profile, and direct-post surfaces plus protected deep-link redirects in `/home/mental/projects/Postly/frontend/tests/e2e/likes-and-direct-post.spec.ts`

### Implementation for User Story 3

- [ ] T039 [US3] Add the like schema, indexes, and EF migration in `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/AppDbContext.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/Configurations/LikeConfiguration.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/Migrations/AddLikes.cs`
- [ ] T040 [US3] Implement direct-post reads and unavailable-state mapping in `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Application/GetPostDetailHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Contracts/PostDetailResponse.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Endpoints/PostEndpoints.cs`
- [ ] T041 [US3] Implement like and unlike handlers that return aggregate counts and current-user state only in `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Application/LikePostHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Application/UnlikePostHandler.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Application/PostProjection.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Posts/Contracts/PostSummaryResponse.cs`
- [ ] T042 [US3] Extend timeline and profile post queries to project viewer-liked state and cross-surface action permissions in `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Timeline/Application/GetTimelineHandler.cs` and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Profiles/Application/GetProfilePostsHandler.cs`
- [ ] T043 [US3] Implement the direct-post route, unavailable page state, and deep-link API flow in `/home/mental/projects/Postly/frontend/src/shared/api/postsApi.ts`, `/home/mental/projects/Postly/frontend/src/features/posts/PostDetailPage.tsx`, and `/home/mental/projects/Postly/frontend/src/app/routes/AppRouter.tsx`
- [ ] T044 [US3] Add reusable like/unlike controls and cross-surface ownership consistency on timeline, profile, and detail views in `/home/mental/projects/Postly/frontend/src/features/posts/LikeButton.tsx`, `/home/mental/projects/Postly/frontend/src/features/posts/PostCard.tsx`, `/home/mental/projects/Postly/frontend/src/features/profiles/ProfilePage.tsx`, and `/home/mental/projects/Postly/frontend/src/features/timeline/HomeTimelinePage.tsx`

**Checkpoint**: All approved MVP user stories are implemented and independently testable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish the MVP with the smallest necessary security, performance, accessibility, and verification work.

- [ ] T045 [P] Add secure cookie settings, auth/write rate limits, and trace-id ProblemDetails logging in `/home/mental/projects/Postly/backend/src/Postly.Api/Program.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Security/AuthExtensions.cs`, and `/home/mental/projects/Postly/backend/src/Postly.Api/Features/Shared/Errors/ProblemDetailsMapping.cs`
- [ ] T046 [P] Tune SQLite WAL usage, verify feed/query indexes, and document migration rollback/setup notes in `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/AppDbContext.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/Migrations/InitialAuthPosts.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/Migrations/AddFollows.cs`, `/home/mental/projects/Postly/backend/src/Postly.Api/Persistence/Migrations/AddLikes.cs`, and `/home/mental/projects/Postly/specs/001-microblog-mvp/quickstart.md`
- [ ] T047 [P] Audit and fix mobile layout, visible focus, labels, and non-color-only state feedback across `/home/mental/projects/Postly/frontend/src/shared/styles/app.css`, `/home/mental/projects/Postly/frontend/src/features/auth/SigninPage.tsx`, `/home/mental/projects/Postly/frontend/src/features/posts/PostComposer.tsx`, `/home/mental/projects/Postly/frontend/src/features/posts/PostDetailPage.tsx`, `/home/mental/projects/Postly/frontend/src/features/profiles/ProfilePage.tsx`, and `/home/mental/projects/Postly/frontend/src/features/timeline/HomeTimelinePage.tsx`
- [ ] T048 Run the full backend/frontend quality gates and quickstart smoke validation through `/home/mental/projects/Postly/backend/Postly.sln`, `/home/mental/projects/Postly/frontend/package.json`, and `/home/mental/projects/Postly/specs/001-microblog-mvp/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** has no dependencies and starts immediately.
- **Phase 2: Foundational** depends on T001-T004 and blocks all user story work.
- **Phase 3: US1** depends on T005-T012 and is the smallest end-to-end slice that proves the architecture.
- **Phase 4: US2** depends on US1 because it extends authenticated post surfaces, profile routing, and the shared timeline shell.
- **Phase 5: US3** depends on US1 and US2 because likes and direct-post behavior must be consistent across the existing timeline and profile surfaces.
- **Phase 6: Polish** depends on the user stories targeted for MVP completion.

### Within Each User Story

- Write the story tests first and confirm they fail before implementation.
- Land schema and migration tasks before the handlers that depend on them.
- Complete backend contracts and application logic before wiring the matching frontend API modules and pages.
- Finish UX-state and authorization consistency work before closing the story.

### Story Dependency Summary

- **US1 (P1)**: Starts after Foundational only.
- **US2 (P2)**: Starts after US1 is functional enough to reuse auth, post cards, and protected routing.
- **US3 (P3)**: Starts after US1 and US2 provide the timeline, profile, and post surfaces that likes and direct-post behavior extend.

---

## Parallel Opportunities

### Setup

- T002, T003, and T004 can run in parallel after T001 establishes the repository structure.

### Foundational

- T006, T007, T008, T009, T010, T011, and T012 can run in parallel after T005 defines the host/bootstrap direction.

### User Story 1

- T013, T014, T015, T016, and T017 can run in parallel.
- After T018, T019, T020, and T021 can run in parallel across backend feature folders.
- After T019-T021 are stable, T022, T023, and T024 can run in parallel across frontend auth, timeline/posts, and profile files.

### User Story 2

- T025, T026, T027, and T028 can run in parallel.
- After T029, T030 and T031 can run in parallel.
- After T030-T031 are available, T032, T033, and T034 can run in parallel.

### User Story 3

- T035, T036, T037, and T038 can run in parallel.
- After T039, T040, T041, and T042 can run in parallel across backend post, timeline, and profile handlers.
- After T040-T042 are available, T043 and T044 can run in parallel.

### Polish

- T045, T046, and T047 can run in parallel before the final verification task T048.

---

## Parallel Example: User Story 1

```bash
# Tests first
T013 Auth/post contract tests
T014 Auth/post integration tests
T015 Auth and protected-route component tests
T016 Composer and profile component tests
T017 Auth/post Playwright flow

# Backend implementation after schema
T019 Auth handlers
T020 Profile/timeline reads
T021 Post mutation handlers
```

## Parallel Example: User Story 2

```bash
# Tests first
T025 Profile/timeline contract tests
T026 Follow/timeline integration tests
T027 Profile/timeline component tests
T028 Follow/timeline Playwright flow

# After T029
T030 Profile relationship + follow handlers
T031 Timeline composition + pagination
```

## Parallel Example: User Story 3

```bash
# Tests first
T035 Direct-post and like contract tests
T036 Like/direct-post integration tests
T037 Like/direct-post component tests
T038 Like/direct-post Playwright flow

# After T039
T040 Direct-post read path
T041 Like/unlike handlers
T042 Timeline/profile like projections
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Deliver Phase 3 (US1) as the first end-to-end slice.
3. Run T048 quality gates against the US1 slice before expanding scope.

### Incremental Delivery

1. Add US2 to extend the authenticated shell into profile navigation, follow/unfollow, and personalized timeline composition.
2. Add US3 to complete likes, direct-post deep links, and final ownership/visibility consistency.
3. Finish Phase 6 for MVP security, accessibility, performance, and final verification.

### Recommended Team Split

1. One developer drives T005-T012 because those decisions define the shared boundaries.
2. After foundation, split backend and frontend within each story using the `[P]` tasks.
3. Keep Playwright and contract test tasks moving in parallel with implementation so drift is caught early.

---

## Notes

- `[P]` marks tasks that can proceed in parallel because they touch different files and do not require unfinished predecessors.
- Every user story includes explicit contract, validation/error-handling, UX-state, and automated-test work to stay aligned with the constitution.
- The recommended stopping point for the first implementation pass is the end of **Phase 3 / US1**.
