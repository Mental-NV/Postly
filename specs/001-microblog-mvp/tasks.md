# Tasks: Postly Microblog MVP

**Input**: Design documents from `/specs/001-microblog-mvp/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Automated tests are REQUIRED. Every user story includes backend and/or frontend tests at the lowest useful level plus affected integration, contract, and end-to-end coverage.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. Each story includes testing, validation/error handling, and UX consistency work.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g. `US1`, `US2`)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the repository structure, tool configuration, and backend-hosted frontend runtime skeleton.

- [ ] T001 Create backend and frontend project skeletons in `backend/src/Postly.Api/` and `frontend/`
- [ ] T002 Initialize the ASP.NET Core project file and package references in `backend/src/Postly.Api/Postly.Api.csproj`
- [ ] T003 [P] Initialize the frontend package, TypeScript, and Vite configuration in `frontend/package.json`, `frontend/tsconfig.json`, and `frontend/vite.config.ts`
- [ ] T004 [P] Configure frontend linting, formatting, and test commands in `frontend/eslint.config.js`, `frontend/.prettierrc`, and `frontend/package.json`
- [ ] T005 [P] Configure backend test projects in `backend/tests/Postly.Api.UnitTests/Postly.Api.UnitTests.csproj`, `backend/tests/Postly.Api.IntegrationTests/Postly.Api.IntegrationTests.csproj`, and `backend/tests/Postly.Api.ContractTests/Postly.Api.ContractTests.csproj`
- [ ] T006 Add the baseline backend entry point and solution wiring in `backend/src/Postly.Api/Program.cs` and `Postly.sln`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared runtime, data, auth, validation, seeded-data, and backend-hosted SPA infrastructure required by all user stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T007 Implement the EF Core `AppDbContext` and entity sets in `backend/src/Postly.Api/Persistence/AppDbContext.cs`
- [ ] T008 [P] Add EF Core entity configurations for `UserAccount`, `Session`, `Post`, `Follow`, and `Like` in `backend/src/Postly.Api/Persistence/Configurations/`
- [ ] T009 Create the initial SQLite migration and migration runner wiring in `backend/src/Postly.Api/Persistence/Migrations/`
- [ ] T010 Implement deterministic non-production `DataSeed` preparation in `backend/src/Postly.Api/Persistence/DataSeed.cs`
- [ ] T011 [P] Implement the session cookie and authentication infrastructure in `backend/src/Postly.Api/Security/SessionCookieAuthentication.cs` and `backend/src/Postly.Api/Security/CurrentViewerAccessor.cs`
- [ ] T012 [P] Implement shared ProblemDetails error mapping and stable error codes in `backend/src/Postly.Api/Features/Shared/Errors/`
- [ ] T013 [P] Implement shared request validation helpers in `backend/src/Postly.Api/Features/Shared/Validation/`
- [ ] T014 Add startup wiring for SQLite migrations, `DataSeed`, auth, ProblemDetails, rate limiting, and static file hosting in `backend/src/Postly.Api/Program.cs`
- [ ] T015 Implement MSBuild SPA asset synchronization and publish inclusion in `backend/src/Postly.Api/Postly.Api.csproj`
- [ ] T016 [P] Create the typed frontend API client foundation in `frontend/src/shared/api/client.ts`, `frontend/src/shared/api/contracts.ts`, and `frontend/src/shared/api/errors.ts`
- [ ] T017 [P] Create the shared frontend app providers and router bootstrap in `frontend/src/app/providers/` and `frontend/src/app/routes/index.tsx`
- [ ] T018 [P] Create reusable route-state and post-shell UI primitives in `frontend/src/shared/components/`
- [ ] T019 Add foundational backend integration coverage for startup, migrations, and `DataSeed` preparation in `backend/tests/Postly.Api.IntegrationTests/StartupTests.cs`
- [ ] T020 Add backend contract coverage for ProblemDetails and auth bootstrap behavior in `backend/tests/Postly.Api.ContractTests/SharedContractsTests.cs`

**Checkpoint**: Foundation ready. User story implementation can now proceed in priority order.

---

## Phase 3: User Story 1 - Sign Up, Sign In, and Manage Own Posts (Priority: P1) 🎯 MVP

**Goal**: A new or returning user can create an account, sign in, sign out, publish their own posts, edit them, delete them, and remain protected by session rules.

**Independent Test**: A visitor can sign up or sign in, land on the backend-hosted home timeline, create a post, edit it, delete it, sign out, and confirm protected routes redirect back through sign-in.

### Tests for User Story 1 (REQUIRED) ⚠️

- [ ] T021 [P] [US1] Add auth contract tests for `POST /api/auth/signup`, `POST /api/auth/signin`, `POST /api/auth/signout`, and `GET /api/auth/session` in `backend/tests/Postly.Api.ContractTests/AuthContractsTests.cs`
- [ ] T022 [P] [US1] Add post contract tests for `POST /api/posts`, `PATCH /api/posts/{postId}`, and `DELETE /api/posts/{postId}` in `backend/tests/Postly.Api.ContractTests/PostsContractsTests.cs`
- [ ] T023 [P] [US1] Add backend integration tests for signup, sign-in failure, sign-out, protected-route redirect state, and own-post CRUD in `backend/tests/Postly.Api.IntegrationTests/AuthAndOwnPostsFlowTests.cs`
- [ ] T024 [P] [US1] Add frontend component tests for sign-up/sign-in forms, composer validation, and own-post editing states in `frontend/src/features/auth/__tests__/auth-forms.test.tsx` and `frontend/src/features/posts/__tests__/composer-and-editor.test.tsx`
- [ ] T025 [P] [US1] Add Playwright happy-path coverage for signup/signin and own-post CRUD in `frontend/tests/e2e/us1-auth-and-own-posts.spec.ts`

### Implementation for User Story 1

- [ ] T026 [P] [US1] Implement auth request/response contracts in `backend/src/Postly.Api/Features/Auth/Contracts/`
- [ ] T027 [P] [US1] Implement auth application handlers for signup, signin, signout, and session bootstrap in `backend/src/Postly.Api/Features/Auth/Application/`
- [ ] T028 [US1] Implement auth endpoints in `backend/src/Postly.Api/Features/Auth/Endpoints/`
- [ ] T029 [P] [US1] Implement post create, update, and delete handlers with ownership enforcement in `backend/src/Postly.Api/Features/Posts/Application/`
- [ ] T030 [US1] Implement post mutation endpoints in `backend/src/Postly.Api/Features/Posts/Endpoints/`
- [ ] T031 [P] [US1] Implement frontend auth screens and protected-route resume behavior in `frontend/src/features/auth/` and `frontend/src/app/routes/`
- [ ] T032 [P] [US1] Implement the home shell, composer, own-post card actions, and sign-out control in `frontend/src/app/shell/`, `frontend/src/features/posts/`, and `frontend/src/features/timeline/`
- [ ] T033 [US1] Add field validation, generic auth error handling, pending states, delete confirmation, and draft preservation across auth and own-post flows in `frontend/src/features/auth/` and `frontend/src/features/posts/`

**Checkpoint**: User Story 1 should be fully functional and testable on its own.

---

## Phase 4: User Story 2 - Build a Personalized Timeline (Priority: P2)

**Goal**: A signed-in user can view profiles, follow and unfollow other users, and see a newest-first home timeline composed from their own posts plus followed authors.

**Independent Test**: A signed-in user can open another user’s profile, follow them, return home to see their posts in the timeline, then unfollow and see those posts disappear.

### Tests for User Story 2 (REQUIRED) ⚠️

- [ ] T034 [P] [US2] Add contract tests for `GET /api/timeline`, `GET /api/profiles/{username}`, `GET /api/profiles/{username}/posts`, and `POST|DELETE /api/profiles/{username}/follow` in `backend/tests/Postly.Api.ContractTests/TimelineAndProfilesContractsTests.cs`
- [ ] T035 [P] [US2] Add backend integration tests for follow, unfollow, self-follow rejection, and timeline composition in `backend/tests/Postly.Api.IntegrationTests/TimelineAndFollowFlowTests.cs`
- [ ] T036 [P] [US2] Add frontend component tests for profile header relationship state and home/profile empty states in `frontend/src/features/profiles/__tests__/profile-header.test.tsx` and `frontend/src/features/timeline/__tests__/timeline-states.test.tsx`
- [ ] T037 [P] [US2] Add Playwright coverage for follow/unfollow and timeline updates in `frontend/tests/e2e/us2-follow-and-timeline.spec.ts`

### Implementation for User Story 2

- [ ] T038 [P] [US2] Implement timeline read models and query handlers in `backend/src/Postly.Api/Features/Timeline/Application/`
- [ ] T039 [P] [US2] Implement profile read and follow/unfollow handlers in `backend/src/Postly.Api/Features/Profiles/Application/`
- [ ] T040 [US2] Implement timeline and profile endpoints in `backend/src/Postly.Api/Features/Timeline/Endpoints/` and `backend/src/Postly.Api/Features/Profiles/Endpoints/`
- [ ] T041 [P] [US2] Implement frontend timeline feed, author navigation, and zero-follow/zero-post state handling in `frontend/src/features/timeline/`
- [ ] T042 [P] [US2] Implement frontend own-profile and other-profile screens with follow/unfollow controls in `frontend/src/features/profiles/`
- [ ] T043 [US2] Add inline retry handling, follower/following count updates, and route-state consistency for home/profile surfaces in `frontend/src/features/timeline/` and `frontend/src/features/profiles/`

**Checkpoint**: User Stories 1 and 2 should both work independently.

---

## Phase 5: User Story 3 - React to Posts and View Direct Post Details (Priority: P3)

**Goal**: A signed-in user can like and unlike posts across all surfaces, open direct post URLs, and see unavailable states for missing or deleted posts.

**Independent Test**: A signed-in user can like and unlike posts from timeline, profile, and direct-post views, and a deleted or missing direct post shows the documented unavailable state.

### Tests for User Story 3 (REQUIRED) ⚠️

- [ ] T044 [P] [US3] Add contract tests for `GET /api/posts/{postId}` and `POST|DELETE /api/posts/{postId}/like` in `backend/tests/Postly.Api.ContractTests/DirectPostAndLikesContractsTests.cs`
- [ ] T045 [P] [US3] Add backend integration tests for like/unlike idempotency, direct-post availability, and cross-surface ownership flags in `backend/tests/Postly.Api.IntegrationTests/DirectPostAndLikesFlowTests.cs`
- [ ] T046 [P] [US3] Add frontend component tests for direct-post unavailable state and like toggle rendering in `frontend/src/features/posts/__tests__/direct-post-and-like-state.test.tsx`
- [ ] T047 [P] [US3] Add Playwright coverage for like/unlike and direct-post unavailable behavior in `frontend/tests/e2e/us3-likes-and-direct-post.spec.ts`

### Implementation for User Story 3

- [ ] T048 [P] [US3] Implement direct-post read and like/unlike handlers in `backend/src/Postly.Api/Features/Posts/Application/`
- [ ] T049 [US3] Implement direct-post read and like/unlike endpoints in `backend/src/Postly.Api/Features/Posts/Endpoints/`
- [ ] T050 [P] [US3] Implement frontend like/unlike interactions across reusable post cards in `frontend/src/features/posts/`
- [ ] T051 [P] [US3] Implement the direct-post route and unavailable fallback screen in `frontend/src/features/posts/` and `frontend/src/app/routes/`
- [ ] T052 [US3] Add consistent action availability, inline failure handling, and not-available recovery links across timeline, profile, and direct-post surfaces in `frontend/src/features/posts/`, `frontend/src/features/timeline/`, and `frontend/src/features/profiles/`

**Checkpoint**: User Stories 1, 2, and 3 should now be independently functional.

---

## Phase 6: User Story 4 - Frontend Route Contract and Backend-Hosted SPA Runtime (Priority: P1 Supporting Delivery)

**Goal**: The backend serves the SPA from `wwwroot`, local runs use a single entry point, and the documented route/screen contract is implemented consistently.

**Independent Test**: Running `dotnet run --project backend/src/Postly.Api` serves the SPA entry document, protected and public routes resolve correctly, and frontend assets are synchronized into `wwwroot` for local runs and publish output.

### Tests for User Story 4 (REQUIRED) ⚠️

- [ ] T053 [P] [US4] Add backend integration tests for static-file serving, SPA fallback routing, and publish-ready asset resolution in `backend/tests/Postly.Api.IntegrationTests/SpaHostingTests.cs`
- [ ] T054 [P] [US4] Add frontend component tests for route shell and shared state containers in `frontend/src/app/routes/__tests__/route-shell.test.tsx`
- [ ] T055 [P] [US4] Add Playwright smoke coverage for backend-hosted SPA startup and route navigation in `frontend/tests/e2e/us4-backend-hosted-spa.spec.ts`

### Implementation for User Story 4

- [ ] T056 [P] [US4] Implement MSBuild targets equivalent to `SyncSpaAssetsToWwwroot` and `IncludeSpaDistInPublish` in `backend/src/Postly.Api/Postly.Api.csproj`
- [ ] T057 [P] [US4] Implement SPA static-file middleware and fallback route handling in `backend/src/Postly.Api/Program.cs`
- [ ] T058 [P] [US4] Implement the documented route tree and shared shell composition in `frontend/src/app/routes/` and `frontend/src/app/shell/`
- [ ] T059 [US4] Add stable `data-testid` hooks, shared route-state containers, and backend-entry-point Playwright configuration in `frontend/src/` and `frontend/playwright.config.ts`

**Checkpoint**: The approved backend-hosted frontend runtime is functional and testable independently.

---

## Phase 7: User Story 5 - Seeded Non-Production Data and E2E Reliability (Priority: P1 Supporting Delivery)

**Goal**: Deterministic `DataSeed` supports the documented Alice/Bob flows and Playwright can rely on repeatable baseline state.

**Independent Test**: A non-production startup prepares deterministic `alice` and `bob` fixture data, baseline follow/like state is restored, and Playwright can run from a known state without test-only public endpoints.

### Tests for User Story 5 (REQUIRED) ⚠️

- [ ] T060 [P] [US5] Add backend integration tests for `DataSeed` idempotency and baseline state restoration in `backend/tests/Postly.Api.IntegrationTests/DataSeedTests.cs`
- [ ] T061 [P] [US5] Add Playwright setup validation for backend readiness and seeded baseline assumptions in `frontend/tests/e2e/setup/global.setup.ts`

### Implementation for User Story 5

- [ ] T062 [P] [US5] Finalize seeded users, seeded posts, and baseline relationship/like state in `backend/src/Postly.Api/Persistence/DataSeed.cs`
- [ ] T063 [P] [US5] Add non-production startup gating for best-effort `DataSeed` preparation in `backend/src/Postly.Api/Program.cs`
- [ ] T064 [US5] Configure Playwright startup, readiness checks, and seeded environment assumptions in `frontend/playwright.config.ts` and `frontend/tests/e2e/setup/`

**Checkpoint**: Seeded-data preparation and e2e startup are stable enough to support all core flows.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Finalize cross-story quality, accessibility, documentation, and release readiness.

- [ ] T065 [P] Add backend unit tests for validation helpers, ownership rules, and error code mapping in `backend/tests/Postly.Api.UnitTests/`
- [ ] T066 [P] Add frontend accessibility and copy-consistency tests for shared route states and post cards in `frontend/src/shared/test/`
- [ ] T067 Run and document quickstart validation updates in `specs/001-microblog-mvp/quickstart.md`
- [ ] T068 Document compatibility, migration, and rollback notes for schema and seed changes in `specs/001-microblog-mvp/quickstart.md` and pull request notes
- [ ] T069 Perform performance and rate-limit review for auth, timeline, and direct-post surfaces in `backend/src/Postly.Api/Program.cs` and `backend/src/Postly.Api/Persistence/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion; blocks all story work.
- **User Story phases (Phase 3 onward)**: Depend on Foundational completion.
- **Polish (Phase 8)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1**: Starts after Foundational; this is the MVP slice.
- **US2**: Starts after Foundational and benefits from US1 auth/session work, but remains independently testable once implemented.
- **US3**: Starts after Foundational and reuses the shared post-card and auth/session behavior from earlier work.
- **US4**: Starts after Foundational; may proceed alongside US1 because backend-hosted SPA delivery underpins local and e2e execution.
- **US5**: Starts after Foundational; should complete before relying on stable end-to-end automation across all stories.

### Within Each User Story

- Tests first, and they should fail before implementation.
- Contracts and models before handlers/services.
- Handlers/services before endpoints and UI integration.
- Validation, predictable errors, and UX states before story sign-off.

### Parallel Opportunities

- Setup tasks marked `[P]` can run in parallel.
- Foundational tasks marked `[P]` can run in parallel after the core project skeleton exists.
- Within each story, tests and file-isolated implementation tasks marked `[P]` can run in parallel.
- US2, US4, and US5 can overlap after the foundational phase if staffing allows.

---

## Parallel Example: User Story 1

```bash
# Launch User Story 1 tests together:
Task: "Add auth contract tests in backend/tests/Postly.Api.ContractTests/AuthContractsTests.cs"
Task: "Add post contract tests in backend/tests/Postly.Api.ContractTests/PostsContractsTests.cs"
Task: "Add frontend component tests in frontend/src/features/auth/__tests__/auth-forms.test.tsx and frontend/src/features/posts/__tests__/composer-and-editor.test.tsx"

# Launch User Story 1 implementation tasks that touch different files together:
Task: "Implement auth request/response contracts in backend/src/Postly.Api/Features/Auth/Contracts/"
Task: "Implement frontend auth screens in frontend/src/features/auth/"
Task: "Implement the home shell and composer in frontend/src/app/shell/ and frontend/src/features/posts/"
```

---

## Parallel Example: User Story 2

```bash
# Launch User Story 2 tests together:
Task: "Add contract tests in backend/tests/Postly.Api.ContractTests/TimelineAndProfilesContractsTests.cs"
Task: "Add frontend component tests in frontend/src/features/profiles/__tests__/profile-header.test.tsx and frontend/src/features/timeline/__tests__/timeline-states.test.tsx"

# Launch User Story 2 implementation tasks together:
Task: "Implement timeline read models in backend/src/Postly.Api/Features/Timeline/Application/"
Task: "Implement profile read and follow handlers in backend/src/Postly.Api/Features/Profiles/Application/"
Task: "Implement frontend profile screens in frontend/src/features/profiles/"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Complete Phase 6: User Story 4
5. Complete Phase 7: User Story 5
6. **STOP and VALIDATE**: Confirm backend-hosted SPA startup, seeded environment, and User Story 1 behavior through quickstart and Playwright

### Incremental Delivery

1. Foundation ready
2. Deliver US1 + US4 + US5 as the first end-to-end usable increment
3. Add US2 and validate follow/timeline composition independently
4. Add US3 and validate likes/direct-post behavior independently
5. Finish with Polish and cross-cutting improvements

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: US1 auth and own-post flows
   - Developer B: US4 backend-hosted SPA runtime
   - Developer C: US5 seeded-data and Playwright startup
3. After those land:
   - Developer A: US2 profiles and follows
   - Developer B: US3 likes and direct-post

---

## Notes

- `[P]` tasks indicate different files and no dependency on incomplete sibling work.
- Every user story phase includes explicit testing, validation/error handling, and UX consistency work.
- The suggested MVP scope for the first deliverable is **US1 + US4 + US5** because Postly’s approved runtime model requires backend-hosted frontend assets and deterministic seeded startup for usable end-to-end verification.
