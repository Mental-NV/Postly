# Implementation Plan: Postly Microblog MVP

**Branch**: `001-microblog-mvp` | **Date**: 2026-04-09 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-microblog-mvp/spec.md`

**Note**: This plan covers Phase 0 research and Phase 1 design artifacts for the
approved MVP only.

## Summary

Build Postly as a same-repo full-stack web application with an ASP.NET Core
Minimal API backend and a React + TypeScript + Vite frontend. The smallest
end-to-end slice proves the core architecture by delivering open signup,
username/password sign-in, protected routing, the authenticated home shell, and
create/read-own-post flow on SQLite with EF Core migrations, consistent
ProblemDetails-style errors, a typed frontend API client, and the full testing
and quality-gate baseline.

Subsequent MVP slices add profile viewing plus follow/unfollow and timeline
composition, then likes and direct-post view, while keeping the architecture
simple, feature-oriented, and aligned with the constitution's requirements for
module boundaries, explicit contracts, automated tests, UX consistency, and
safe evolution.

## Technical Context

**Language/Version**: C# on .NET 10 (backend), TypeScript in strict mode (frontend)  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core with SQLite provider and migrations, ASP.NET Core authentication/cookie middleware, ASP.NET Core Identity password hasher, React, React Router, Vite, Vitest, React Testing Library, Playwright  
**Storage**: SQLite for the MVP application database, managed through EF Core migrations  
**Testing**: xUnit for backend unit/integration/contract tests, Vitest + React Testing Library for frontend unit/component tests, Playwright for critical end-to-end flows  
**Target Platform**: Same-origin web app for desktop and mobile browsers; local development on macOS/Linux/Windows  
**Project Type**: Full-stack web application in a single repository  
**Interfaces/Contracts**: REST/JSON API under `/api`, same-origin SPA routes (`/`, `/signin`, `/signup`, `/u/:username`, `/posts/:postId`), cursor pagination for post collections, `application/problem+json` error responses  
**Error Handling Strategy**: Boundary validation on every request, domain/application errors mapped to ProblemDetails-style responses with stable error codes, 400/401/403/404/409 responses used consistently, frontend error mapping through a typed API client boundary  
**UX Surfaces**: Signup, sign-in, sign-out redirect handling, protected home timeline, composer, post cards, direct post view, own profile, other-user profile, follow/unfollow and like/unlike interactions  
**Performance Goals**: P95 API response under 250 ms for auth and single-post reads and under 400 ms for paginated timeline/profile reads on a single-node MVP dataset; initial pages deliver 20 posts per request; primary content/actions avoid horizontal scrolling on mobile and desktop  
**Constraints**: Same repository for frontend/backend, clear boundary between endpoint/application/persistence and UI/API client layers, minimal dependencies unless they reduce contract drift or quality risk, SQLite single-node limits accepted for MVP, no repository abstraction unless a concrete duplication/problem appears, no scope beyond approved MVP  
**Scale/Scope**: Single-node MVP for low-thousands of active users and tens of thousands of posts, with newest-first timelines/profiles and deterministic local setup

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Design Gate

- [x] Clear module boundaries are documented, including ownership and allowed
      dependency direction.
      Backend is split into endpoint, application, and persistence concerns;
      frontend is split into app shell, feature modules, and a shared API
      client boundary.
- [x] All affected contracts, validation rules, and predictable error outcomes
      are specified.
      The spec already locks auth, visibility, ownership, direct-post behavior,
      state handling, and explicit validation/error outcomes that the contracts
      will encode.
- [x] Automated tests are planned at the lowest useful level plus any impacted
      integration or contract boundaries.
      The plan includes backend unit/integration/contract tests, frontend
      Vitest coverage, and Playwright end-to-end flows for the critical paths.
- [x] UX impact is documented, including loading, empty, success, and error
      states plus any intended pattern deviations.
      The spec defines these states for auth, timeline, profile, compose,
      follow, and like flows; the plan preserves one shared component and state
      pattern across surfaces.
- [x] The design is the simplest viable approach, and any breaking change
      includes migration or rollback notes.
      The plan keeps one backend app, one frontend app, SQLite, EF migrations,
      and direct DbContext usage without repository indirection.

### Post-Design Re-Check

- [x] Clear module boundaries are documented, including ownership and allowed
      dependency direction.
      Project structure, research decisions, contracts, and data model all keep
      dependency direction explicit: UI -> API client -> HTTP contracts -> API
      endpoints -> application handlers -> EF persistence.
- [x] All affected contracts, validation rules, and predictable error outcomes
      are specified.
      `contracts/openapi.yaml` defines request/response shapes, pagination, and
      ProblemDetails responses; `data-model.md` captures entity constraints.
- [x] Automated tests are planned at the lowest useful level plus any impacted
      integration or contract boundaries.
      Quickstart and delivery phases include unit, integration, contract,
      component, and end-to-end coverage as non-optional quality gates.
- [x] UX impact is documented, including loading, empty, success, and error
      states plus any intended pattern deviations.
      The plan and quickstart require consistent pending, empty, success, retry,
      and protected-access experiences for each critical surface.
- [x] The design is the simplest viable approach, and any breaking change
      includes migration or rollback notes.
      Schema evolution stays migration-first with SQLite, no speculative shared
      libraries, and no product-scope expansion beyond the approved MVP.

## Project Structure

### Documentation (this feature)

```text
specs/001-microblog-mvp/
├── frontend-requirements.md
├── plan.md
├── research.md
├── data-model.md
├── user-flows.md
├── quickstart.md
├── contracts/
│   └── openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
backend/
├── src/
│   └── Postly.Api/
│       ├── Features/
│       │   ├── Auth/
│       │   │   ├── Application/
│       │   │   ├── Contracts/
│       │   │   └── Endpoints/
│       │   ├── Posts/
│       │   │   ├── Application/
│       │   │   ├── Contracts/
│       │   │   └── Endpoints/
│       │   ├── Profiles/
│       │   │   ├── Application/
│       │   │   ├── Contracts/
│       │   │   └── Endpoints/
│       │   ├── Timeline/
│       │   │   ├── Application/
│       │   │   ├── Contracts/
│       │   │   └── Endpoints/
│       │   └── Shared/
│       │       ├── Contracts/
│       │       ├── Errors/
│       │       └── Validation/
│       ├── Persistence/
│       │   ├── Configurations/
│       │   ├── Migrations/
│       │   └── AppDbContext.cs
│       ├── Security/
│       └── Program.cs
└── tests/
    ├── Postly.Api.UnitTests/
    ├── Postly.Api.IntegrationTests/
    └── Postly.Api.ContractTests/

frontend/
├── src/
│   ├── app/
│   │   ├── providers/
│   │   ├── routes/
│   │   └── shell/
│   ├── features/
│   │   ├── auth/
│   │   ├── posts/
│   │   ├── profiles/
│   │   └── timeline/
│   └── shared/
│       ├── api/
│       ├── components/
│       ├── lib/
│       ├── styles/
│       └── test/
```

**Structure Decision**: Use one backend executable project plus clear internal
layering instead of multiple class-library projects. That keeps startup,
dependency management, and feature evolution simple while still enforcing clear
responsibility boundaries through folders/namespaces. The frontend stays as one
Vite app organized by features with a single shared API client boundary so UI
code never calls fetch directly from feature components.

## Delivery Phases

### Phase 0: Architecture-Proving Slice

Deliver the minimum end-to-end slice that proves the chosen architecture:

1. Backend app bootstrap, SQLite connection, EF Core migrations, and
   ProblemDetails/error pipeline.
2. Signup, sign-in, sign-out, session bootstrap, and protected-route handling.
3. Frontend authenticated shell, auth forms, typed API client, and route guard.
4. Create post + own timeline read path with loading, empty, success, and error
   states.
5. Baseline quality gates: backend tests, frontend tests, type-checking,
   linting, formatting, and one Playwright happy-path flow.

### Phase 1: Social Graph and Timeline Composition

1. Profile read model for own/other profiles.
2. Follow and unfollow flows with consistent counts.
3. Timeline composition from self + followed users, newest-first, with cursor
   pagination and empty-state handling.
4. Zero-post and zero-follow experiences on home and profile surfaces.

### Phase 2: Engagement and Detail Views

1. Like and unlike flows across timeline, profile, and direct-post surfaces.
2. Direct-post read endpoint and page, including unavailable-state handling.
3. Cross-surface ownership/visibility consistency checks.
4. Accessibility and mobile-layout polish required by the spec, not deferred.

## Operational Considerations

- Use same-origin deployment for the SPA and API so cookie-based auth stays
  simple and CORS remains a local-dev concern only; Vite dev server proxies
  `/api` to ASP.NET Core.
- Use secure, HTTP-only, same-site cookies for authenticated sessions. Persist
  session records in SQLite so sign-out and stale-session invalidation are
  explicit rather than purely stateless.
- Apply fixed-window rate limits to signup and sign-in endpoints and lighter
  write-endpoint limits to post creation/update/delete to reduce abuse without
  introducing non-MVP moderation features.
- Keep SQLite in WAL mode and add the minimal indexes needed for username
  lookup, newest-first post queries, follows, and likes.
- Emit structured request logs and include trace identifiers in ProblemDetails
  responses for supportability without introducing a separate observability
  stack.
- Treat schema changes as migration-first changes with rollback notes in the
  migration PR. Local setup uses deterministic migration application rather than
  ad hoc manual SQL.

## Complexity Tracking

No constitutional violations or unjustified complexity are required for this
plan. Repository abstractions, distributed caching, background jobs, message
buses, public APIs beyond the MVP contract, and shared domain packages are
intentionally excluded until a concrete need appears.
