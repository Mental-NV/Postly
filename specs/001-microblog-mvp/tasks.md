# Tasks: Postly Microblog MVP

**Input**: Design documents from `/specs/001-microblog-mvp/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Automated tests are REQUIRED. Every user story includes the lowest useful backend and frontend automated coverage plus affected contract, integration, and end-to-end coverage.

**Organization**: Tasks are grouped by the approved spec user stories for strict one-to-one traceability. Shared runtime, infrastructure, `DataSeed`, and backend-hosted SPA concerns live in Setup or Foundational phases rather than fake user stories.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel
- **[Story]**: User story label for story-phase tasks only
- Every task includes an exact file path

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize the repository structure, toolchain, and project scaffolding.

- [x] T001 Create the ASP.NET Core backend project skeleton in `backend/src/Postly.Api/`
- [x] T002 Create the frontend project skeleton in `frontend/`
- [x] T003 [P] Initialize the backend project file and package references in `backend/src/Postly.Api/Postly.Api.csproj`
- [x] T004 [P] Initialize the frontend package, TypeScript, and Vite configuration in `frontend/package.json`, `frontend/tsconfig.json`, and `frontend/vite.config.ts`
- [x] T005 [P] Configure frontend linting, formatting, and shared test commands in `frontend/package.json`, `frontend/eslint.config.js`, and `frontend/.prettierrc`
- [x] T006 [P] Create backend test project files in `backend/tests/Postly.Api.UnitTests/Postly.Api.UnitTests.csproj`, `backend/tests/Postly.Api.IntegrationTests/Postly.Api.IntegrationTests.csproj`, and `backend/tests/Postly.Api.ContractTests/Postly.Api.ContractTests.csproj`
- [x] T007 [P] Create frontend end-to-end test scaffolding directories in `frontend/tests/e2e/` and `frontend/tests/e2e/setup/`
- [x] T008 Add baseline solution and startup files in `Postly.sln` and `backend/src/Postly.Api/Program.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared runtime, data, auth/session, backend-hosted SPA, deterministic `DataSeed`, and cross-cutting foundations required by every user story.

**⚠️ CRITICAL**: No user story work starts until this phase is complete

- [x] T009 Implement the EF Core `AppDbContext` and entity sets in `backend/src/Postly.Api/Persistence/AppDbContext.cs`
- [x] T010 [P] Add EF Core entity configurations for `UserAccount`, `Session`, `Post`, `Follow`, and `Like` in `backend/src/Postly.Api/Persistence/Configurations/`
- [x] T011 Create the initial SQLite migrations and migration startup support in `backend/src/Postly.Api/Persistence/Migrations/`
- [x] T012 Implement deterministic non-production `DataSeed` preparation in `backend/src/Postly.Api/Persistence/DataSeed.cs`
- [x] T013 [P] Implement session cookie authentication and current-viewer accessors in `backend/src/Postly.Api/Security/SessionCookieAuthentication.cs` and `backend/src/Postly.Api/Security/CurrentViewerAccessor.cs`
- [x] T014 [P] Implement shared ProblemDetails mapping and stable error codes in `backend/src/Postly.Api/Features/Shared/Errors/`
- [x] T015 [P] Implement shared validation helpers in `backend/src/Postly.Api/Features/Shared/Validation/`
- [x] T016 Add startup wiring for migrations, `DataSeed`, auth, ProblemDetails, and static files in `backend/src/Postly.Api/Program.cs`
- [x] T017 Implement MSBuild targets equivalent to `SyncSpaAssetsToWwwroot` and `IncludeSpaDistInPublish` in `backend/src/Postly.Api/Postly.Api.csproj`
- [x] T018 [P] Create the typed frontend API client foundation in `frontend/src/shared/api/client.ts`, `frontend/src/shared/api/contracts.ts`, and `frontend/src/shared/api/errors.ts`
- [x] T019 [P] Create the shared frontend app providers and route bootstrap in `frontend/src/app/providers/` and `frontend/src/app/routes/index.tsx`
- [x] T020 [P] Create reusable route-state, status, and post-shell components in `frontend/src/shared/components/`
- [x] T021 Configure backend-entry-point Playwright startup, readiness checks, and seeded setup support in `frontend/playwright.config.ts` and `frontend/tests/e2e/setup/`
- [x] T022 Add foundational backend integration tests for startup, migrations, static-file hosting, and `DataSeed` preparation in `backend/tests/Postly.Api.IntegrationTests/StartupAndHostingTests.cs`
- [x] T023 Add foundational backend contract tests for shared ProblemDetails and auth bootstrap behavior in `backend/tests/Postly.Api.ContractTests/SharedContractsTests.cs`

**Checkpoint**: ✅ Shared runtime, backend-hosted SPA, and seeded environment are ready for user-story implementation.

---

## Phase 3: User Story 1 - Sign Up (Priority: P1) 🎯 MVP

**Goal**: A first-time visitor can create an account from `/signup` and land signed in on the home timeline.

**Independent Test**: A visitor can open `/signup`, submit valid account data, receive field validation when invalid, and land on the signed-in home timeline after success.

**Covers**: UF-01, UF-02

### Tests for User Story 1 (REQUIRED) ⚠️

- [x] T024 [P] [US1] Add contract tests for `POST /api/auth/signup` validation and conflict cases in `backend/tests/Postly.Api.ContractTests/AuthSignupContractsTests.cs`
- [x] T025 [P] [US1] Add backend integration tests for signup success, duplicate username, and invalid field handling in `backend/tests/Postly.Api.IntegrationTests/AuthSignupFlowTests.cs`
- [x] T026 [P] [US1] Add frontend component tests for sign-up form fields, validation, and pending state in `frontend/src/features/auth/__tests__/signup-form.test.tsx`
- [x] T027 [P] [US1] Add Playwright coverage for sign-up success and sign-up validation in `frontend/tests/e2e/us1-sign-up.spec.ts`

### Implementation for User Story 1

- [x] T028 [P] [US1] Implement signup request and response contracts in `backend/src/Postly.Api/Features/Auth/Contracts/SignupContracts.cs`
- [x] T029 [P] [US1] Implement signup application handling in `backend/src/Postly.Api/Features/Auth/Application/SignupHandler.cs`
- [x] T030 [US1] Implement the signup endpoint in `backend/src/Postly.Api/Features/Auth/Endpoints/SignupEndpoints.cs`
- [x] T031 [P] [US1] Implement the `/signup` route screen and form in `frontend/src/features/auth/signup/SignupPage.tsx`
- [x] T032 [US1] Add sign-up field-level errors, form status region, pending state, and success navigation in `frontend/src/features/auth/signup/useSignupForm.ts` and `frontend/src/features/auth/signup/SignupPage.tsx`

**Checkpoint**: ✅ User Story 1 is independently functional and testable.

---

## Phase 4: User Story 2 - Sign In and Resume Protected Navigation (Priority: P1)

**Goal**: A returning user can sign in, resume protected navigation, and remain blocked from protected content after sign-out until signing in again.

**Independent Test**: A signed-out visitor requesting `/`, `/u/:username`, or `/posts/:postId` is redirected to `/signin`, signs in successfully, returns to the original destination, and protected content is blocked again after sign-out.

**Covers**: UF-03, UF-04, UF-12

### Tests for User Story 2 (REQUIRED) ⚠️

- [x] T033 [P] [US2] Add contract tests for `POST /api/auth/signin`, `POST /api/auth/signout`, and `GET /api/auth/session` in `backend/tests/Postly.Api.ContractTests/AuthSessionContractsTests.cs`
- [x] T034 [P] [US2] Add backend integration tests for signin success, signin failure, redirect resume, and signout re-protection in `backend/tests/Postly.Api.IntegrationTests/AuthSigninAndSessionFlowTests.cs`
- [x] T035 [P] [US2] Add frontend component tests for the sign-in form, redirect message, and protected-route guard behavior in `frontend/src/features/auth/__tests__/signin-and-route-guard.test.tsx`
- [x] T036 [P] [US2] Add Playwright coverage for signin, protected redirect return, and signout protection in `frontend/tests/e2e/us2-sign-in-and-protected-navigation.spec.ts`

### Implementation for User Story 2

- [x] T037 [P] [US2] Implement signin, signout, and session contracts in `backend/src/Postly.Api/Features/Auth/Contracts/SigninContracts.cs` and `backend/src/Postly.Api/Features/Auth/Contracts/SessionContracts.cs`
- [x] T038 [P] [US2] Implement signin, signout, and session bootstrap handlers in `backend/src/Postly.Api/Features/Auth/Application/SigninHandler.cs`, `backend/src/Postly.Api/Features/Auth/Application/SignoutHandler.cs`, and `backend/src/Postly.Api/Features/Auth/Application/GetSessionHandler.cs`
- [x] T039 [US2] Implement signin, signout, and session endpoints in `backend/src/Postly.Api/Features/Auth/Endpoints/SigninEndpoints.cs` and `backend/src/Postly.Api/Features/Auth/Endpoints/SessionEndpoints.cs`
- [x] T040 [P] [US2] Implement the `/signin` route screen and sign-in form in `frontend/src/features/auth/signin/SigninPage.tsx`
- [x] T041 [P] [US2] Implement protected-route capture and return behavior in `frontend/src/app/routes/ProtectedRoute.tsx` and `frontend/src/app/routes/navigationState.ts`
- [x] T042 [US2] Add sign-in generic error handling, username preservation, password clearing, and post-signout re-protection in `frontend/src/features/auth/signin/useSigninForm.ts` and `frontend/src/app/routes/ProtectedRoute.tsx`

**Checkpoint**: ✅ User Story 2 is independently functional and testable.

---

## Phase 5: User Story 3 - Publish and Manage Own Posts (Priority: P1)

**Goal**: A signed-in user can create, edit, and delete their own posts from the home timeline and profile surfaces.

**Independent Test**: A signed-in user can publish a valid post, edit it, delete it, and see validation, pending, and confirmation behavior without affecting another user’s content rules.

**Covers**: UF-05, UF-06, UF-07

### Tests for User Story 3 (REQUIRED) ⚠️

- [x] T043 [P] [US3] Add contract tests for `POST /api/posts`, `PATCH /api/posts/{postId}`, and `DELETE /api/posts/{postId}` in `backend/tests/Postly.Api.ContractTests/OwnPostsContractsTests.cs`
- [x] T044 [P] [US3] Add backend integration tests for own-post create, edit, delete, stale delete handling, and ownership rejection in `backend/tests/Postly.Api.IntegrationTests/OwnPostsFlowTests.cs`
- [x] T045 [P] [US3] Add frontend component tests for composer, editor, character-limit messaging, and delete confirmation in `frontend/src/features/posts/__tests__/own-posts-ui.test.tsx`
- [x] T046 [P] [US3] Add Playwright coverage for create, edit, and delete own-post flows in `frontend/tests/e2e/us3-own-posts.spec.ts`

### Implementation for User Story 3

- [x] T047 [P] [US3] Implement post create, update, and delete contracts in `backend/src/Postly.Api/Features/Posts/Contracts/PostMutationContracts.cs`
- [x] T048 [P] [US3] Implement own-post create, update, and delete handlers with ownership enforcement in `backend/src/Postly.Api/Features/Posts/Application/CreatePostHandler.cs`, `backend/src/Postly.Api/Features/Posts/Application/UpdatePostHandler.cs`, and `backend/src/Postly.Api/Features/Posts/Application/DeletePostHandler.cs`
- [x] T049 [US3] Implement post mutation endpoints in `backend/src/Postly.Api/Features/Posts/Endpoints/PostMutationEndpoints.cs`
- [x] T050 [P] [US3] Implement the signed-in home shell, composer, and own-post action controls in `frontend/src/app/shell/AppShell.tsx`, `frontend/src/features/posts/composer/Composer.tsx`, and `frontend/src/features/posts/post-card/PostCard.tsx`
- [x] T051 [P] [US3] Implement own-post edit mode and delete confirmation behavior in `frontend/src/features/posts/editor/PostEditor.tsx` and `frontend/src/shared/components/ConfirmDialog.tsx`
- [x] T052 [US3] Add own-post validation, character-limit feedback, pending states, draft preservation, and timeline/profile state updates in `frontend/src/features/posts/` and `frontend/src/features/timeline/`

**Checkpoint**: ✅ User Story 3 is independently functional and testable.

---

## Phase 6: User Story 4 - Build a Personalized Timeline (Priority: P2)

**Goal**: A signed-in user can visit profiles, follow and unfollow other users, and see the home timeline composed from self plus followed users.

**Independent Test**: A signed-in user can open another user’s profile, follow them, return to the home timeline to see followed content, then unfollow and see it removed.

**Covers**: UF-08, UF-09

### Tests for User Story 4 (REQUIRED) ⚠️

- [ ] T053 [P] [US4] Add contract tests for `GET /api/timeline`, `GET /api/profiles/{username}`, `GET /api/profiles/{username}/posts`, and `POST|DELETE /api/profiles/{username}/follow` in `backend/tests/Postly.Api.ContractTests/TimelineAndProfilesContractsTests.cs`
- [ ] T054 [P] [US4] Add backend integration tests for follow, unfollow, self-follow rejection, timeline composition, and zero-state transitions in `backend/tests/Postly.Api.IntegrationTests/TimelineAndFollowFlowTests.cs`
- [ ] T055 [P] [US4] Add frontend component tests for profile headers, follow state, and home/profile empty/error states in `frontend/src/features/profiles/__tests__/profiles-and-follow-state.test.tsx` and `frontend/src/features/timeline/__tests__/timeline-states.test.tsx`
- [ ] T056 [P] [US4] Add Playwright coverage for profile navigation, follow/unfollow, and timeline updates in `frontend/tests/e2e/us4-timeline-and-follows.spec.ts`

### Implementation for User Story 4

- [ ] T057 [P] [US4] Implement timeline read contracts and profile contracts in `backend/src/Postly.Api/Features/Timeline/Contracts/TimelineContracts.cs` and `backend/src/Postly.Api/Features/Profiles/Contracts/ProfileContracts.cs`
- [ ] T058 [P] [US4] Implement timeline query handling in `backend/src/Postly.Api/Features/Timeline/Application/GetTimelineHandler.cs`
- [ ] T059 [P] [US4] Implement profile read and follow/unfollow handlers in `backend/src/Postly.Api/Features/Profiles/Application/GetProfileHandler.cs`, `backend/src/Postly.Api/Features/Profiles/Application/GetProfilePostsHandler.cs`, and `backend/src/Postly.Api/Features/Profiles/Application/FollowProfileHandler.cs`
- [ ] T060 [US4] Implement timeline and profile endpoints in `backend/src/Postly.Api/Features/Timeline/Endpoints/TimelineEndpoints.cs` and `backend/src/Postly.Api/Features/Profiles/Endpoints/ProfileEndpoints.cs`
- [ ] T061 [P] [US4] Implement the home timeline feed and author navigation in `frontend/src/features/timeline/TimelinePage.tsx` and `frontend/src/features/timeline/TimelineFeed.tsx`
- [ ] T062 [P] [US4] Implement own-profile and other-profile screens with follow/unfollow controls in `frontend/src/features/profiles/ProfilePage.tsx` and `frontend/src/features/profiles/ProfileHeader.tsx`
- [ ] T063 [US4] Add zero-post, zero-follow, retry, and relationship-count update behavior across home and profile surfaces in `frontend/src/features/timeline/` and `frontend/src/features/profiles/`

**Checkpoint**: User Story 4 is independently functional and testable.

---

## Phase 7: User Story 5 - React to Posts and View Profiles (Priority: P3)

**Goal**: A signed-in user can like and unlike posts, open direct post URLs, and see profile/direct-post surfaces with consistent ownership, visibility, and unavailable-state behavior.

**Independent Test**: A signed-in user can like and unlike posts across surfaces, open a direct post successfully, and receive the documented unavailable state for a missing or deleted post.

**Covers**: UF-10, UF-11, plus profile/direct-post visibility and cross-surface action consistency from the spec acceptance scenarios

### Tests for User Story 5 (REQUIRED) ⚠️

- [ ] T064 [P] [US5] Add contract tests for `GET /api/posts/{postId}` and `POST|DELETE /api/posts/{postId}/like` in `backend/tests/Postly.Api.ContractTests/DirectPostAndLikesContractsTests.cs`
- [ ] T065 [P] [US5] Add backend integration tests for like/unlike idempotency, direct-post availability, unavailable states after auth, and cross-surface ownership flags in `backend/tests/Postly.Api.IntegrationTests/DirectPostAndLikesFlowTests.cs`
- [ ] T066 [P] [US5] Add frontend component tests for direct-post rendering, like state, and unavailable-state recovery in `frontend/src/features/posts/__tests__/direct-post-and-likes-ui.test.tsx`
- [ ] T067 [P] [US5] Add Playwright coverage for like/unlike and direct-post unavailable behavior in `frontend/tests/e2e/us5-likes-and-direct-post.spec.ts`

### Implementation for User Story 5

- [ ] T068 [P] [US5] Implement direct-post and like contracts in `backend/src/Postly.Api/Features/Posts/Contracts/DirectPostContracts.cs` and `backend/src/Postly.Api/Features/Posts/Contracts/PostInteractionContracts.cs`
- [ ] T069 [P] [US5] Implement direct-post read and like/unlike handlers in `backend/src/Postly.Api/Features/Posts/Application/GetDirectPostHandler.cs`, `backend/src/Postly.Api/Features/Posts/Application/LikePostHandler.cs`, and `backend/src/Postly.Api/Features/Posts/Application/UnlikePostHandler.cs`
- [ ] T070 [US5] Implement direct-post and like/unlike endpoints in `backend/src/Postly.Api/Features/Posts/Endpoints/DirectPostEndpoints.cs` and `backend/src/Postly.Api/Features/Posts/Endpoints/PostInteractionEndpoints.cs`
- [ ] T071 [P] [US5] Implement reusable like/unlike controls and cross-surface post rendering in `frontend/src/features/posts/post-card/PostCard.tsx` and `frontend/src/features/posts/post-card/PostLikeButton.tsx`
- [ ] T072 [P] [US5] Implement the direct-post route and unavailable fallback screen in `frontend/src/features/posts/direct-post/DirectPostPage.tsx`
- [ ] T073 [US5] Add cross-surface action availability, unavailable-state messaging, and profile/direct-post consistency updates in `frontend/src/features/posts/`, `frontend/src/features/profiles/`, and `frontend/src/app/routes/`

**Checkpoint**: User Story 5 is independently functional and testable.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Finalize accessibility, copy consistency, documentation, performance, and release readiness across all stories.

- [ ] T074 [P] Add backend unit tests for validation helpers, ownership rules, and error code mapping in `backend/tests/Postly.Api.UnitTests/`
- [ ] T075 [P] Add frontend accessibility and copy-consistency tests for shared route states and post cards in `frontend/src/shared/test/accessibility-and-copy.test.tsx`
- [ ] T076 Run and document quickstart validation updates in `specs/001-microblog-mvp/quickstart.md`
- [ ] T077 Document compatibility, migration, and rollback notes for schema, static-asset sync, and seeded-data behavior in `specs/001-microblog-mvp/quickstart.md`
- [ ] T078 Perform performance review for auth, timeline, and direct-post flows in `backend/src/Postly.Api/Program.cs` and `backend/src/Postly.Api/Persistence/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories.
- **User Stories (Phase 3 onward)**: Depend on Foundational completion.
- **Polish (Phase 8)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1**: Starts after Foundational; no dependency on other user stories.
- **US2**: Starts after Foundational; reuses shared auth/session infrastructure but remains independently testable.
- **US3**: Starts after Foundational; depends on signed-in behavior from shared infrastructure, not on other story completion.
- **US4**: Starts after Foundational; reuses shared auth/session and post-card infrastructure, but remains independently testable.
- **US5**: Starts after Foundational; reuses shared post/profile infrastructure, but remains independently testable.

### Within Each User Story

- Tests should be written first and fail before implementation.
- Contracts before handlers/endpoints.
- Handlers before frontend integration.
- Validation, predictable error handling, and UX state coverage before story sign-off.

### Parallel Opportunities

- Setup tasks marked `[P]` can run in parallel.
- Foundational tasks marked `[P]` can run in parallel once the project skeleton exists.
- Within each story, tests and file-isolated implementation tasks marked `[P]` can run in parallel.
- After Foundational, different user stories can proceed in parallel if staffing allows, while still delivering in spec priority order.

---

## Parallel Example: User Story 3

```bash
# Launch User Story 3 tests together:
Task: "Add contract tests in backend/tests/Postly.Api.ContractTests/OwnPostsContractsTests.cs"
Task: "Add backend integration tests in backend/tests/Postly.Api.IntegrationTests/OwnPostsFlowTests.cs"
Task: "Add frontend component tests in frontend/src/features/posts/__tests__/own-posts-ui.test.tsx"

# Launch User Story 3 implementation tasks together:
Task: "Implement own-post handlers in backend/src/Postly.Api/Features/Posts/Application/"
Task: "Implement the home shell and composer in frontend/src/app/shell/AppShell.tsx and frontend/src/features/posts/composer/Composer.tsx"
Task: "Implement post editor and confirmation dialog in frontend/src/features/posts/editor/PostEditor.tsx and frontend/src/shared/components/ConfirmDialog.tsx"
```

---

## Parallel Example: User Story 4

```bash
# Launch User Story 4 tests together:
Task: "Add contract tests in backend/tests/Postly.Api.ContractTests/TimelineAndProfilesContractsTests.cs"
Task: "Add frontend component tests in frontend/src/features/profiles/__tests__/profiles-and-follow-state.test.tsx and frontend/src/features/timeline/__tests__/timeline-states.test.tsx"

# Launch User Story 4 implementation tasks together:
Task: "Implement timeline query handling in backend/src/Postly.Api/Features/Timeline/Application/GetTimelineHandler.cs"
Task: "Implement profile read and follow handlers in backend/src/Postly.Api/Features/Profiles/Application/"
Task: "Implement frontend timeline and profile screens in frontend/src/features/timeline/ and frontend/src/features/profiles/"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Complete Phase 4: User Story 2
5. Complete Phase 5: User Story 3
6. **STOP and VALIDATE**: Confirm signup, signin, protected-route return, own-post CRUD, backend-hosted SPA startup, and seeded environment through quickstart and Playwright

### Incremental Delivery

1. Setup + Foundational create the shared runtime and backend-hosted SPA baseline.
2. Deliver US1
3. Deliver US2
4. Deliver US3
5. Deliver US4
6. Deliver US5
7. Finish with Polish and cross-cutting improvements

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together.
2. After Foundational:
   - Developer A: US1 then US2
   - Developer B: US3
   - Developer C: US4 then US5
3. Integrate after each independently testable story checkpoint.

---

## Notes

- `[P]` means the task is parallelizable because it touches separate files and does not depend on unfinished sibling tasks.
- Story labels map exactly to the approved spec user stories.
- Backend-hosted SPA runtime, MSBuild sync, Playwright startup, and `DataSeed` remain shared infrastructure, not separate user stories.
- Suggested MVP scope for the first usable increment is **Setup + Foundational + US1 + US2 + US3** because the approved MVP’s core first slice requires account access plus own-post management on the backend-hosted runtime.
