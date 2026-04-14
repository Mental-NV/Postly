---

description: "Task list template for feature implementation"
---

# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), spec.md (required for user stories),
user-flows.md, frontend-requirements.md, openapi.yaml, research.md,
data-model.md

**Tests**: Automated tests are REQUIRED. Every user story with user-visible
behavior changes MUST include the tests needed to prove new behavior and
prevent regressions: backend unit coverage plus backend integration or contract
coverage as needed, frontend component coverage, and Playwright coverage.

**Organization**: Tasks are grouped by user story to enable independent
implementation and testing of each story. Each story MUST include testing,
validation and error handling, UX consistency work, and explicit frontend and
backend delivery tasks that trace back to the planned user flow(s).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions
- Reference the relevant story and, when helpful, the flow ID in the task text
- Prefer tests-first ordering within each user story whenever feasible

## Path Conventions

- **Single project**: `src/`, `tests/` at repository root
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Sample tasks below use web app paths - adjust based on plan.md structure

<!-- 
  ============================================================================
  IMPORTANT: The tasks below are SAMPLE TASKS for illustration purposes only.
  
  The /speckit.tasks command MUST replace these with actual tasks based on:
  - User stories from spec.md (with their priorities P1, P2, P3...)
  - Feature requirements from plan.md
  - Supporting artifacts such as data-model.md, openapi.yaml, and
    frontend-requirements.md
  
  Tasks MUST be organized by user story so each story can be:
  - Implemented independently
  - Tested independently
  - Delivered as an MVP increment
  
  DO NOT keep these sample tasks in the generated tasks.md file.
  ============================================================================
-->

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create project structure per implementation plan
- [ ] T002 Initialize [language] project with [framework] dependencies
- [ ] T003 [P] Configure linting, formatting, and automated test commands

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

Examples of foundational tasks (adjust based on your project):

- [ ] T004 Setup database schema and migrations framework
- [ ] T005 [P] Implement authentication/authorization framework
- [ ] T006 [P] Setup API routing and middleware structure
- [ ] T007 Create base models/entities that all stories depend on
- [ ] T008 Configure validation, predictable error handling, and logging infrastructure
- [ ] T009 Setup environment configuration management

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - [Title] (Priority: P1) 🎯 MVP

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 1 (REQUIRED) ⚠️

> **NOTE: Write these tests FIRST when feasible, ensure they FAIL before
> implementation**

- [ ] T010 [P] [US1] Add backend unit coverage for [business rule / service] in backend/tests/[unit-path]
- [ ] T011 [P] [US1] Add backend integration or contract coverage for [flow / endpoint] in backend/tests/[integration-or-contract-path]
- [ ] T012 [P] [US1] Add frontend component coverage for [surface / state] in frontend/src/[test-path]
- [ ] T013 [P] [US1] Add Playwright coverage for flow [F1] in frontend/tests/e2e/[spec-name].spec.ts

### Implementation for User Story 1

- [ ] T014 [US1] Implement backend behavior for flow [F1], including API, validation, error outcomes, and persistence updates in backend/src/[path]
- [ ] T015 [US1] Implement frontend behavior for flow [F1], including routes and visible states in frontend/src/[path]
- [ ] T016 [US1] Add or confirm stable required elements and shared `data-testid` hooks in frontend/src/[path]
- [ ] T017 [US1] Align loading, empty, success, and error states with existing UX patterns in frontend/src/[path]

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - [Title] (Priority: P2)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 2 (REQUIRED) ⚠️

- [ ] T018 [P] [US2] Add backend unit coverage for [business rule / service] in backend/tests/[unit-path]
- [ ] T019 [P] [US2] Add backend integration or contract coverage for [flow / endpoint] in backend/tests/[integration-or-contract-path]
- [ ] T020 [P] [US2] Add frontend component coverage for [surface / state] in frontend/src/[test-path]
- [ ] T021 [P] [US2] Add Playwright coverage for flow [F2] in frontend/tests/e2e/[spec-name].spec.ts

### Implementation for User Story 2

- [ ] T022 [US2] Implement backend behavior for flow [F2], including API, validation, error outcomes, and persistence updates in backend/src/[path]
- [ ] T023 [US2] Implement frontend behavior for flow [F2], including routes and visible states in frontend/src/[path]
- [ ] T024 [US2] Add or confirm stable required elements and shared `data-testid` hooks in frontend/src/[path]
- [ ] T025 [US2] Integrate with User Story 1 behavior only through defined contracts and shared patterns (if needed)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - [Title] (Priority: P3)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 3 (REQUIRED) ⚠️

- [ ] T026 [P] [US3] Add backend unit coverage for [business rule / service] in backend/tests/[unit-path]
- [ ] T027 [P] [US3] Add backend integration or contract coverage for [flow / endpoint] in backend/tests/[integration-or-contract-path]
- [ ] T028 [P] [US3] Add frontend component coverage for [surface / state] in frontend/src/[test-path]
- [ ] T029 [P] [US3] Add Playwright coverage for flow [F3] in frontend/tests/e2e/[spec-name].spec.ts

### Implementation for User Story 3

- [ ] T030 [US3] Implement backend behavior for flow [F3], including API, validation, error outcomes, and persistence updates in backend/src/[path]
- [ ] T031 [US3] Implement frontend behavior for flow [F3], including routes and visible states in frontend/src/[path]
- [ ] T032 [US3] Add or confirm stable required elements and shared `data-testid` hooks in frontend/src/[path]
- [ ] T033 [US3] Align validation, predictable error handling, and UX state coverage in frontend/src/[path] and backend/src/[path]

**Checkpoint**: All user stories should now be independently functional

---

[Add more user story phases as needed, following the same pattern]

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] TXXX [P] Documentation updates in docs/
- [ ] TXXX Code cleanup and refactoring
- [ ] TXXX Performance optimization across all stories
- [ ] TXXX [P] Additional unit tests in tests/unit/
- [ ] TXXX Security hardening
- [ ] TXXX Document contract/schema compatibility or migration steps
- [ ] TXXX [P] Accessibility and copy consistency review across changed surfaces
- [ ] TXXX Run quickstart.md validation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - May integrate with US1 but should be independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - May integrate with US1/US2 but should be independently testable

### Within Each User Story

- Tests-first task ordering SHOULD be used whenever feasible, and tests MUST be
  added before story completion
- Backend and frontend work MUST both trace back to the planned story and flow
- Stable required elements and `data-testid` hooks MUST be defined before
  Playwright-relevant UI is considered complete
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Add backend unit coverage for [business rule / service] in backend/tests/[unit-path]"
Task: "Add backend integration or contract coverage for [flow / endpoint] in backend/tests/[integration-or-contract-path]"
Task: "Add frontend component coverage for [surface / state] in frontend/src/[test-path]"
Task: "Add Playwright coverage for flow [F1] in frontend/tests/e2e/[spec-name].spec.ts"

# Launch implementation work once tests are in place:
Task: "Implement backend behavior for flow [F1] in backend/src/[path]"
Task: "Implement frontend behavior for flow [F1] in frontend/src/[path]"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story MUST be independently completable and testable
- Each user story MUST include tasks for tests, validation/error handling, and
  UX consistency
- Each user-visible story MUST include backend, frontend, and Playwright test
  coverage tasks
- Reuse the same logical `data-testid` for the same control across surfaces
- Verify tests fail before implementing whenever feasible
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
