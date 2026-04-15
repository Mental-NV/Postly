# Tasks: Postly Round 2 Social Extensions

**Input**: Design documents from `/specs/002-profile-replies-notifications/`  
**Prerequisites**: `plan.md`, `spec.md`, `user-flows.md`, `frontend-requirements.md`, `openapi.yaml`, `research.md`, `data-model.md`, `quickstart.md`, `ui-ux-design.md`

**Tests**: Automated tests are REQUIRED. Every Round 2 user story includes
backend unit coverage, backend integration coverage, backend contract coverage
for changed API surfaces, frontend component coverage, and Playwright coverage.

**Organization**: Tasks are grouped by approved Round 2 user stories for
explicit traceability:
`user story -> user flow -> frontend/backend requirements -> contracts/data model -> tasks`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel
- **[Story]**: User story label for story-phase tasks only
- Every task includes exact file paths

---

## Phase 1: User Story 1 - Profile Editing and Avatar Replacement (Priority: P1) 🎯

**Goal**: A signed-in user can update their display name, bio, and avatar and
see the new identity reflected on their own profile plus the in-scope timeline
and conversation identity surfaces.

**Independent Test**: A signed-in user edits their own profile, saves valid
identity changes, sees the update on `/u/bob`, `/`, and `/posts/:postId`, then
submits invalid edits and confirms the saved identity remains unchanged.

**Covers**: `UF-01`, `UF-02`, `UF-03` -> `FE-07` -> US1 backend/frontend
responsibilities -> `UserAccount`, `ProfileView`, `PostSummary`

### Story-Owned Foundations for User Story 1

- [X] T005 [US1] Extend profile identity persistence for display-name, bio, and avatar state in `backend/src/Postly.Api/Persistence/Entities/UserAccount.cs` and `backend/src/Postly.Api/Persistence/AppDbContext.cs`
- [X] T006 [US1] Add EF Core configuration and indexes for profile identity and avatar state in `backend/src/Postly.Api/Persistence/Configurations/UserAccountConfiguration.cs`
- [X] T007 [US1] Create the EF Core migration for profile identity and avatar schema changes in `backend/src/Postly.Api/Persistence/Migrations/`
- [X] T008 [US1] Expand shared frontend API contracts and client helpers for profile editing and avatar replacement in `frontend/src/shared/api/contracts.ts`, `frontend/src/shared/api/client.ts`, and `frontend/src/shared/api/errors.ts`
- [X] T009 [US1] Create story-specific test roots for profiles in `backend/tests/Postly.Api.UnitTests/Features/Profiles/` and `frontend/src/features/profiles/__tests__/`

### Tests for User Story 1 (REQUIRED) ⚠️

- [X] T010 [P] [US1] Add backend unit coverage for profile validation, owner-only authorization, avatar format/size/dimension rules, avatar normalization to 512x512 high-quality JPEG, metadata stripping, transparency flattening, and avatar fallback projection in `backend/tests/Postly.Api.UnitTests/Features/Profiles/ProfileEditValidationTests.cs` and `backend/tests/Postly.Api.UnitTests/Features/Profiles/ProfileAvatarProjectionTests.cs`
- [X] T011 [P] [US1] Add backend contract coverage for anonymous `401` on `PATCH /api/profiles/me` and `PUT /api/profiles/me/avatar`, avatar upload validation failures, versioned `avatarUrl` responses, anonymous success on preserved public reads `GET /api/profiles/{username}` and `GET /api/profiles/{username}/avatar`, and `image/jpeg` delivery from `GET /api/profiles/{username}/avatar` in `backend/tests/Postly.Api.ContractTests/ProfileEditingContractsTests.cs`
- [X] T012 [P] [US1] Add backend integration coverage for successful profile edits, avatar replacement with normalized JPEG storage, invalid or malformed avatar replacement preserving prior data, and unchanged persisted identity on failure in `backend/tests/Postly.Api.IntegrationTests/ProfileEditingFlowTests.cs`
- [X] T013 [P] [US1] Add frontend component coverage for inline profile edit states, avatar input constraints, validation messaging, pending save, versioned avatar refresh, broken-image fallback, and avatar fallback rendering in `frontend/src/features/profiles/__tests__/profile-editing-ui.test.tsx`
- [X] T014 [P] [US1] Add Playwright coverage for `UF-01`, `UF-02`, and `UF-03` in `frontend/tests/e2e/us6-profile-editing.spec.ts`

### Backend Implementation for User Story 1

- [X] T015 [US1] Implement profile-edit request and response contracts in `backend/src/Postly.Api/Features/Profiles/Contracts/ProfileContracts.cs`
- [X] T016 [US1] Implement display name/bio update and avatar replacement handlers with JPEG/PNG validation, deterministic normalization to 512x512 high-quality JPEG, metadata stripping, transparency flattening, and unchanged-on-failure semantics in `backend/src/Postly.Api/Features/Profiles/Application/UpdateProfileHandler.cs` and `backend/src/Postly.Api/Features/Profiles/Application/ReplaceAvatarHandler.cs`
- [X] T017 [US1] Implement owner-only profile-edit and avatar endpoints in `backend/src/Postly.Api/Features/Profiles/Endpoints/ProfileEndpoints.cs` and wire them in `backend/src/Postly.Api/Program.cs`
- [X] T018 [US1] Implement cross-surface identity projection and avatar metadata updates in `backend/src/Postly.Api/Features/Profiles/Application/GetProfileHandler.cs`, `backend/src/Postly.Api/Features/Timeline/Application/GetTimelineHandler.cs`, `backend/src/Postly.Api/Features/Posts/Application/GetPostHandler.cs`, and `backend/src/Postly.Api/Features/Posts/Application/PostSummaryFactory.cs`

### Frontend Implementation for User Story 1

- [X] T019 [US1] Implement inline own-profile edit mode, field validation, form status, and pending-save behavior in `frontend/src/features/profiles/ProfilePage.tsx`
- [X] T020 [US1] Implement avatar upload controls with JPEG/PNG input constraints plus fallback-first avatar rendering in `frontend/src/shared/components/Avatar.tsx` and `frontend/src/features/profiles/ProfilePage.tsx`
- [X] T021 [US1] Implement cross-surface identity refresh on profile, timeline, and conversation views using returned versioned avatar URLs in `frontend/src/features/timeline/TimelinePage.tsx`, `frontend/src/features/posts/DirectPostPage.tsx`, and `frontend/src/shared/components/PostCard.tsx`

### Contracts, Data, Fixtures, and Documentation for User Story 1

- [X] T022 [US1] Add deterministic profile-edit, valid avatar upload, invalid avatar upload, and avatar-fallback fixtures for Bob identity surfaces in `backend/src/Postly.Api/Persistence/DataSeed.cs`, `frontend/src/shared/test/factories.ts`, and `frontend/tests/e2e/helpers.ts`

**Checkpoint**: User Story 1 is independently testable.

---

## Phase 2: User Story 2 - Replies and Conversation-Oriented Direct-Post View (Priority: P1)

**Goal**: A signed-in user can reply from the conversation route, edit and
delete their own replies, see deleted-reply placeholders, and continue viewing
the conversation when the parent post becomes unavailable.

**Independent Test**: A signed-in user opens `/posts/:postId`, creates a reply,
edits it, deletes it into a placeholder, confirms another user's reply has no
edit/delete controls, and verifies an unavailable parent still leaves the route
and visible replies accessible.

**Covers**: `UF-04`, `UF-05`, `UF-06`, `UF-07` -> `FE-08` -> US2
backend/frontend responsibilities -> `Post`, `ConversationView`, `PostSummary`

### Story-Owned Foundations for User Story 2

- [ ] T023 [US2] Extend reply persistence in `backend/src/Postly.Api/Persistence/Entities/Post.cs` and `backend/src/Postly.Api/Persistence/AppDbContext.cs`
- [ ] T024 [US2] Add EF Core configuration and indexes for reply persistence and placeholder behavior in `backend/src/Postly.Api/Persistence/Configurations/PostConfiguration.cs`
- [ ] T025 [US2] Create the EF Core migration for reply schema changes in `backend/src/Postly.Api/Persistence/Migrations/`
- [ ] T026 [US2] Expand shared frontend API contracts and client helpers for conversation reads and reply mutations in `frontend/src/shared/api/contracts.ts`, `frontend/src/shared/api/client.ts`, and `frontend/src/shared/api/errors.ts`
- [ ] T027 [US2] Create story-specific test roots for posts in `backend/tests/Postly.Api.UnitTests/Features/Posts/` and `frontend/src/features/posts/__tests__/`

### Tests for User Story 2 (REQUIRED) ⚠️

- [ ] T028 [P] [US2] Add backend unit coverage for reply validation, author-only reply ownership, deleted-placeholder transitions, and unavailable-target rejection in `backend/tests/Postly.Api.UnitTests/Features/Posts/ReplyValidationTests.cs` and `backend/tests/Postly.Api.UnitTests/Features/Posts/ReplyOwnershipTests.cs`
- [ ] T029 [P] [US2] Add backend contract coverage for anonymous success on preserved public reads `GET /api/posts/{postId}` and `GET /api/posts/{postId}/replies`, plus anonymous `401` on `POST /api/posts/{postId}/replies`, `PATCH /api/posts/{postId}`, and `DELETE /api/posts/{postId}` in `backend/tests/Postly.Api.ContractTests/ReplyConversationContractsTests.cs`
- [ ] T030 [P] [US2] Add backend integration coverage for reply create/edit/delete, deleted placeholders, unavailable parent reads, anonymous direct-post and reply-read access, anonymous reply-create rejection, and unavailable target submission failures in `backend/tests/Postly.Api.IntegrationTests/ReplyConversationFlowTests.cs`
- [ ] T031 [P] [US2] Add frontend component coverage for conversation target states, reply composer states, non-author action absence, and deleted placeholders in `frontend/src/features/posts/__tests__/reply-conversation-ui.test.tsx`
- [ ] T032 [P] [US2] Add Playwright coverage for `UF-04`, `UF-05`, `UF-06`, and `UF-07` in `frontend/tests/e2e/us7-replies-and-conversation.spec.ts`

### Backend Implementation for User Story 2

- [ ] T033 [US2] Implement reply and conversation contracts in `backend/src/Postly.Api/Features/Posts/Contracts/PostMutationContracts.cs` and `backend/src/Postly.Api/Features/Posts/Contracts/PostQueryContracts.cs`
- [ ] T034 [US2] Implement reply creation and conversation query handlers in `backend/src/Postly.Api/Features/Posts/Application/CreateReplyHandler.cs`, `backend/src/Postly.Api/Features/Posts/Application/GetPostHandler.cs`, and `backend/src/Postly.Api/Features/Posts/Application/GetRepliesHandler.cs`
- [ ] T035 [US2] Implement reply edit/delete placeholder behavior and author-only enforcement in `backend/src/Postly.Api/Features/Posts/Application/UpdatePostHandler.cs`, `backend/src/Postly.Api/Features/Posts/Application/DeletePostHandler.cs`, and `backend/src/Postly.Api/Features/Posts/Application/PostSummaryFactory.cs`
- [ ] T036 [US2] Implement reply and conversation endpoint wiring in `backend/src/Postly.Api/Features/Posts/Endpoints/PostQueryEndpoints.cs` and `backend/src/Postly.Api/Features/Posts/Endpoints/PostMutationEndpoints.cs`

### Frontend Implementation for User Story 2

- [ ] T037 [US2] Implement the conversation layout, target/unavailable states, and reply composer flow in `frontend/src/features/posts/DirectPostPage.tsx`
- [ ] T038 [US2] Implement reply-card edit/delete reuse, deleted-reply placeholder rendering, confirmation flow, and draft preservation in `frontend/src/shared/components/PostCard.tsx`, `frontend/src/features/posts/editor/PostEditor.tsx`, and `frontend/src/shared/components/ConfirmDialog.tsx`

### Contracts, Data, Fixtures, and Documentation for User Story 2

- [ ] T039 [US2] Add deterministic reply ownership, unavailable-parent, and multi-page conversation fixtures in `backend/src/Postly.Api/Persistence/DataSeed.cs`, `frontend/src/shared/test/factories.ts`, and `frontend/tests/e2e/helpers.ts`

**Checkpoint**: User Story 2 is independently testable.

---

## Phase 3: User Story 3 - In-App Notifications (Priority: P2)

**Goal**: A signed-in user can view follow, like, and reply notifications,
distinguish unread from read items, open available or unavailable destinations,
and only mark the selected notification as read after its destination opens.

**Independent Test**: A signed-in user opens `/notifications`, confirms unread
rows remain unread when the list is only viewed, opens one available
notification and one unavailable notification, and verifies only the selected
items transition to read.

**Covers**: `UF-08`, `UF-09`, `UF-10` -> `FE-09`, `FE-11` -> US3
backend/frontend responsibilities -> `Notification`, `NotificationSummary`,
`NotificationOpenResult`

### Story-Owned Foundations for User Story 3

- [ ] T040 [US3] Create backend Notifications feature scaffolding in `backend/src/Postly.Api/Features/Notifications/Contracts/NotificationContracts.cs`, `backend/src/Postly.Api/Features/Notifications/Application/GetNotificationsHandler.cs`, `backend/src/Postly.Api/Features/Notifications/Application/OpenNotificationHandler.cs`, and `backend/src/Postly.Api/Features/Notifications/Endpoints/NotificationEndpoints.cs`
- [ ] T041 [P] [US3] Create frontend notifications route scaffolding in `frontend/src/features/notifications/NotificationsPage.tsx`, `frontend/src/features/notifications/NotificationUnavailablePage.tsx`, and `frontend/src/app/routes/index.tsx`
- [ ] T042 [US3] Extend notification persistence in `backend/src/Postly.Api/Persistence/Entities/Notification.cs` and `backend/src/Postly.Api/Persistence/AppDbContext.cs`
- [ ] T043 [US3] Add EF Core configuration and indexes for notifications in `backend/src/Postly.Api/Persistence/Configurations/NotificationConfiguration.cs`
- [ ] T044 [US3] Create the EF Core migration for notification schema changes in `backend/src/Postly.Api/Persistence/Migrations/`
- [ ] T045 [US3] Expand shared frontend API contracts and client helpers for notifications in `frontend/src/shared/api/contracts.ts`, `frontend/src/shared/api/client.ts`, and `frontend/src/shared/api/errors.ts`
- [ ] T046 [US3] Create story-specific test roots for notifications in `backend/tests/Postly.Api.UnitTests/Features/Notifications/` and `frontend/src/features/notifications/__tests__/`

### Tests for User Story 3 (REQUIRED) ⚠️

- [ ] T047 [P] [US3] Add backend unit coverage for notification creation rules, self-action suppression, destination resolution, and selected-item read transitions in `backend/tests/Postly.Api.UnitTests/Features/Notifications/NotificationCreationTests.cs` and `backend/tests/Postly.Api.UnitTests/Features/Notifications/NotificationOpenTests.cs`
- [ ] T048 [P] [US3] Add backend contract coverage for `GET /api/notifications` and `POST /api/notifications/{notificationId}/open` in `backend/tests/Postly.Api.ContractTests/NotificationsContractsTests.cs`
- [ ] T049 [P] [US3] Add backend integration coverage for follow, like, and reply notification generation plus available/unavailable destination opens and list-only unread preservation in `backend/tests/Postly.Api.IntegrationTests/NotificationsFlowTests.cs`
- [ ] T050 [P] [US3] Add frontend component coverage for unread/read row rendering, selected-row pending state, empty state, and unavailable destination UI in `frontend/src/features/notifications/__tests__/notifications-page.test.tsx` and `frontend/src/features/notifications/__tests__/notification-unavailable-page.test.tsx`
- [ ] T051 [P] [US3] Add Playwright coverage for `UF-08`, `UF-09`, and `UF-10` in `frontend/tests/e2e/us8-notifications.spec.ts`

### Backend Implementation for User Story 3

- [ ] T052 [US3] Implement notification request and response contracts in `backend/src/Postly.Api/Features/Notifications/Contracts/NotificationContracts.cs`
- [ ] T053 [US3] Implement notification list and notification-open handlers in `backend/src/Postly.Api/Features/Notifications/Application/GetNotificationsHandler.cs` and `backend/src/Postly.Api/Features/Notifications/Application/OpenNotificationHandler.cs`
- [ ] T054 [US3] Implement synchronous notification creation inside follow, like, and reply mutations in `backend/src/Postly.Api/Features/Profiles/Application/FollowUserHandler.cs`, `backend/src/Postly.Api/Features/Posts/Application/LikePostHandler.cs`, and `backend/src/Postly.Api/Features/Posts/Application/CreateReplyHandler.cs`
- [ ] T055 [US3] Implement notification endpoints and startup registration in `backend/src/Postly.Api/Features/Notifications/Endpoints/NotificationEndpoints.cs` and `backend/src/Postly.Api/Program.cs`

### Frontend Implementation for User Story 3

- [ ] T056 [US3] Implement the notifications route, list states, and selected-row read transition behavior in `frontend/src/features/notifications/NotificationsPage.tsx` and `frontend/src/app/routes/index.tsx`
- [ ] T057 [US3] Implement the notification unavailable destination screen and return navigation in `frontend/src/features/notifications/NotificationUnavailablePage.tsx` and `frontend/src/shared/components/MainLayout.tsx`
- [ ] T058 [US3] Implement notification badge/count display and notification-open client flow in `frontend/src/shared/api/client.ts`, `frontend/src/shared/api/contracts.ts`, and `frontend/src/shared/components/MainLayout.tsx`

### Contracts, Data, Fixtures, and Documentation for User Story 3

- [ ] T059 [US3] Add deterministic follow, like, and reply notification fixtures for both available and unavailable destinations in `backend/src/Postly.Api/Persistence/DataSeed.cs`, `frontend/src/shared/test/factories.ts`, and `frontend/tests/e2e/helpers.ts`

**Checkpoint**: User Story 3 is independently testable.

---

## Phase 4: User Story 4 - Automatic Continuation Loading (Priority: P3)

**Goal**: A signed-in user can keep loading older content on timeline, profile,
and conversation surfaces at the continuation point, recover from failures
without losing visible items, and reach an explicit end-of-list state.

**Independent Test**: A signed-in user triggers automatic continuation on `/`,
recovers from a profile continuation failure through retry, and reaches an
explicit end state on a multi-page conversation without losing or duplicating
already visible items.

**Covers**: `UF-11`, `UF-12`, `UF-13` -> `FE-10` -> US4 backend/frontend
responsibilities -> cursor-bearing `TimelineResponse`, `ProfileResponse`,
`ConversationResponse`

### Story-Owned Foundations for User Story 4

- [ ] T060 [US4] Expand shared frontend API contracts and client helpers for continuation response shapes on timeline, profile, and conversation surfaces in `frontend/src/shared/api/contracts.ts`, `frontend/src/shared/api/client.ts`, and `frontend/src/shared/api/errors.ts`
- [ ] T061 [US4] Add the one-shot continuation failure helper and shared continuation-specific frontend test plumbing in `frontend/src/shared/test/fetch-mock.ts`, `frontend/src/shared/test/factories.ts`, and `frontend/tests/e2e/helpers.ts`
- [ ] T062 [US4] Create story-specific timeline/continuation test roots in `backend/tests/Postly.Api.UnitTests/Features/Timeline/`, `frontend/src/features/timeline/__tests__/`, and continuation-specific posts/profile test folders where missing

### Tests for User Story 4 (REQUIRED) ⚠️

- [ ] T063 [P] [US4] Add backend unit coverage for retry-safe cursor semantics, duplicate prevention, and exhausted-list behavior in `backend/tests/Postly.Api.UnitTests/Features/Timeline/ContinuationCursorTests.cs` and `backend/tests/Postly.Api.UnitTests/Features/Posts/ReplyContinuationTests.cs`
- [ ] T064 [P] [US4] Add backend contract coverage for continuation response shape and error outcomes on timeline, profile, and conversation reads in `backend/tests/Postly.Api.ContractTests/ContinuationContractsTests.cs`
- [ ] T065 [P] [US4] Add backend integration coverage for timeline/profile/conversation continuation success, retry-after-failure, and explicit exhaustion behavior in `backend/tests/Postly.Api.IntegrationTests/ContinuationFlowTests.cs`
- [ ] T066 [P] [US4] Add frontend component coverage for the shared continuation hook plus timeline, profile, and conversation loading/retry/end states using the one-shot fetch-mock continuation failure helper in `frontend/src/shared/test/use-continuation-collection.test.tsx`, `frontend/src/features/timeline/__tests__/continuation-state-ui.test.tsx`, `frontend/src/features/profiles/__tests__/profile-continuation-ui.test.tsx`, and `frontend/src/features/posts/__tests__/conversation-continuation-ui.test.tsx`
- [ ] T067 [P] [US4] Add Playwright coverage for `UF-11`, `UF-12`, and `UF-13` in `frontend/tests/e2e/us9-continuation-loading.spec.ts` using route interception to fail the first matching continuation request once for retry scenarios

### Backend Implementation for User Story 4

- [ ] T068 [US4] Implement continuation contract fields for timeline, profile, and conversation reads in `backend/src/Postly.Api/Features/Timeline/Contracts/TimelineContracts.cs`, `backend/src/Postly.Api/Features/Profiles/Contracts/ProfileContracts.cs`, and `backend/src/Postly.Api/Features/Posts/Contracts/PostQueryContracts.cs`
- [ ] T069 [US4] Implement retry-safe cursor pagination and exhausted-state behavior in `backend/src/Postly.Api/Features/Timeline/Application/GetTimelineHandler.cs`, `backend/src/Postly.Api/Features/Profiles/Application/GetProfileHandler.cs`, `backend/src/Postly.Api/Features/Posts/Application/GetPostHandler.cs`, and `backend/src/Postly.Api/Features/Posts/Application/GetRepliesHandler.cs`

### Frontend Implementation for User Story 4

- [ ] T070 [US4] Implement the shared automatic continuation hook and sentinel state handling in `frontend/src/shared/hooks/useContinuationCollection.ts` and `frontend/src/shared/components/LoadingState.tsx`
- [ ] T071 [US4] Integrate continuation loading, retry, and end-of-list states into `frontend/src/features/timeline/TimelinePage.tsx`, `frontend/src/features/profiles/ProfilePage.tsx`, and `frontend/src/features/posts/DirectPostPage.tsx`

### Contracts, Data, Fixtures, and Documentation for User Story 4

- [ ] T072 [US4] Add deterministic continuation and exhaustion fixtures for timeline, profile, and conversation surfaces in `backend/src/Postly.Api/Persistence/DataSeed.cs` and align the retry test helpers in `frontend/tests/e2e/helpers.ts` with route interception rather than backend fault toggles

**Checkpoint**: User Story 4 is independently testable.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Finalize shared regressions, visual consistency, documentation, and
delivery readiness across all Round 2 stories.

- [ ] T073 [P] Add shared frontend regression coverage for avatar rendering, unavailable-state copy, and reused post-card selectors in `frontend/src/shared/test/accessibility-and-copy.test.tsx`
- [ ] T074 [P] Add shared backend regression coverage for ProblemDetails codes and seeded-scenario builders in `backend/tests/Postly.Api.UnitTests/TestHelpers/TestDataBuilder.cs` and `backend/tests/Postly.Api.ContractTests/TestWebApplicationFactory.cs`
- [ ] T075 [P] Verify Round 2-affected shared surfaces use the monochrome minimalistic icon style and remove remaining colorful filled icon treatments in `frontend/src/shared/components/`, `frontend/src/shared/components/MainLayout.tsx`, `frontend/src/features/profiles/ProfilePage.tsx`, and `frontend/src/features/notifications/`
- [ ] T076 Run the Round 2 quickstart validation paths and capture any implementation-driven updates in `specs/002-profile-replies-notifications/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Shared Foundations (Phase 1)**: No dependencies.
- **US1 (Phase 2)**: Depends on Shared Foundations only.
- **US2 (Phase 3)**: Depends on Shared Foundations only.
- **US3 (Phase 4)**: Depends on Shared Foundations and reuses reply creation from US2 for full reply-notification coverage.
- **US4 (Phase 5)**: Depends on Shared Foundations and reuses the US2 conversation surface for conversation continuation coverage.
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1**: Independent after Phase 1.
- **US2**: Independent after Phase 1.
- **US3**: Can start after Phase 1 for follow/like notification work, but should not be marked complete until reply-trigger coverage from US2 is in place.
- **US4**: Can start after Phase 1 for timeline/profile continuation work, but should not be marked complete until conversation continuation from US2 is in place.

### Within Each User Story

- Write tests first and verify they fail before implementation when feasible.
- Complete story-owned foundations before story-specific backend handlers and endpoints.
- Complete backend contracts before backend handlers and endpoints.
- Complete backend behavior before frontend integration that depends on it.
- Complete fixture/seed updates before final Playwright stabilization.
- Shared harness work may exist in Phase 1, but story-specific unit, contract, integration, component, fixture, and Playwright files MUST be created only inside the corresponding user story phase.
- Do not mark a story complete until its backend unit, integration, contract, frontend component, and Playwright coverage all pass.

### Parallel Opportunities

- Shared Foundations tasks `T001`, `T002`, `T003`, and `T004` can run in parallel.
- After Phase 1, US1 and US2 can proceed in parallel if capacity allows.
- After US2 lands, US3 and US4 can proceed in parallel because they touch different main file sets.
- Within each story, all test tasks marked `[P]` can run in parallel.

---

## Implementation Strategy

### Suggested MVP Scope

1. Complete Phase 1: Shared Foundations
2. Complete Phase 2: User Story 1
3. Complete Phase 3: User Story 2
4. **Stop and validate** profile editing, avatar fallback, reply creation, reply ownership rules, deleted placeholders, and unavailable-parent conversation behavior

### Incremental Delivery

1. Shared Foundations establish only shared test and error-handling infrastructure.
2. Deliver US1 for profile identity editing.
3. Deliver US2 for replies and conversation behavior.
4. Deliver US3 for notifications.
5. Deliver US4 for continuation loading.
6. Finish with cross-cutting regression coverage and quickstart validation.

### Parallel Team Strategy

1. Team completes Shared Foundations together.
2. After Shared Foundations:
   - Developer A: US1
   - Developer B: US2
3. After US2 lands:
   - Developer A: US3
   - Developer B: US4
4. Run `/speckit.analyze` as the final planning gate before `/speckit.implement`, then finish delivery with Polish during implementation.

---

## Notes

- `[P]` means the task touches separate files and can proceed without waiting on sibling tasks in the same checkpoint.
- Story labels map exactly to the approved Round 2 stories.
- Existing public profile read mode and public direct-post permalink reading are baseline behavior, not new implementation scope for these tasks.
- Notifications and continuation tasks must preserve the clarified unavailable, retry, and selected-item-only read semantics from the planning bundle.
