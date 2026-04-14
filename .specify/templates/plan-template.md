# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION]  
**Primary Dependencies**: [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION]  
**Storage**: [if applicable, e.g., PostgreSQL, CoreData, files or N/A]  
**Testing**: [e.g., pytest, XCTest, cargo test or NEEDS CLARIFICATION]  
**Target Platform**: [e.g., Linux server, iOS 15+, WASM or NEEDS CLARIFICATION]  
**Project Type**: [e.g., library/cli/web-service/mobile-app/compiler/desktop-app or NEEDS CLARIFICATION]  
**Interfaces/Contracts**: [e.g., REST endpoints, form actions, background jobs or NEEDS CLARIFICATION]  
**Error Handling Strategy**: [e.g., typed domain errors, HTTP problem details, inline validation or NEEDS CLARIFICATION]  
**UX Surfaces**: [e.g., compose flow, feed, settings page or NEEDS CLARIFICATION]  
**Performance Goals**: [domain-specific, e.g., 1000 req/s, 10k lines/sec, 60 fps or NEEDS CLARIFICATION]  
**Constraints**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]  
**Scale/Scope**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

For features with multiple user stories, use `/speckit.analyze` before
`/speckit.implement` unless a maintainer-approved exception is documented.

- [ ] `spec.md` is story-first, includes acceptance criteria and scope
      boundaries, and excludes UI implementation detail, API shape, database
      design, and test selectors.
- [ ] Every approved user story maps to at least one end-to-end user flow with
      route transitions, visible states, and verification intent.
- [ ] Required UI automation elements and stable `data-testid` hooks are
      defined before implementation, and repeated controls reuse the same test
      ID across surfaces.
- [ ] Frontend responsibilities, backend responsibilities, API contracts,
      validation rules, error outcomes, and persistence changes trace back to
      the relevant user story and flow.
- [ ] Required backend unit/integration or contract coverage, frontend
      component coverage, and Playwright coverage are planned for each
      user-visible story, with tests-first task ordering preferred when
      feasible.
- [ ] Clear module boundaries are documented, including ownership and allowed
      dependency direction.
- [ ] The design is the simplest viable approach, and any breaking change
      includes migration or rollback notes.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── spec.md              # Story-first user behavior and acceptance document
├── plan.md              # This file (/speckit.plan command output)
├── user-flows.md        # Optional flow breakdown by story
├── frontend-requirements.md # Optional frontend delivery details by story
├── openapi.yaml         # Optional API contract for affected flows
├── data-model.md        # Optional persistence model changes
├── quickstart.md        # Optional validation/runbook steps
├── research.md          # Optional design research and decisions
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

Round 2+ planning order MUST remain: user story -> user flow ->
frontend/backend requirements -> tasks -> implementation.

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Story-to-Flow Mapping

| User Story | Flow ID(s) | Primary User Outcome | Frontend Responsibility | Backend Responsibility | Supporting Artifacts |
|------------|------------|----------------------|-------------------------|------------------------|----------------------|
| US1 | F1 | [Outcome] | [Pages, components, states] | [API, validation, persistence] | [user-flows.md, openapi.yaml, etc.] |

## User Flows

### Flow F1 - [Name] (Story: US1)

- **Start Route**: [route or entry point]
- **Trigger**: [user action or entry condition]
- **Route Transitions**: [from route/state -> to route/state]
- **Visible States**: [loading, empty, success, error, validation, etc.]
- **Verification Intent**: [What browser automation should prove]

[Add one flow per approved story at minimum. Add more flows as needed.]

## UI Automation Contract

| Surface | Required Element | Purpose | Stable Selector / `data-testid` | Notes |
|---------|------------------|---------|----------------------------------|-------|
| [Route/page] | [Control or region] | [Why automation needs it] | [test id or explicit reason role/name is sufficient] | [Reuse rules across surfaces] |

Use the same logical `data-testid` for the same control across all surfaces
where it appears.

## Frontend Responsibilities

### User Story 1

- [Explicit frontend behavior, routes, visible states, accessibility, and UI
  contract responsibilities]

## Backend Responsibilities

### User Story 1

- [Explicit backend behavior, API/validation/error/persistence responsibilities]

## Traceability Notes

- [Map each API contract, validation rule, error outcome, and persistence change
  back to the relevant story and flow]
- [Note any supporting artifacts created: `user-flows.md`,
  `frontend-requirements.md`, `openapi.yaml`, `data-model.md`,
  `quickstart.md`, `research.md`]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
