# Implementation Plan: Postly Microblog MVP

**Branch**: `001-microblog-mvp` | **Date**: 2026-04-10 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-microblog-mvp/spec.md`

## Summary

Build Postly as a same-repo full-stack web application with an ASP.NET Core
Minimal API backend and a React + TypeScript + Vite frontend. The backend is
the runtime entry point: it serves the built SPA from `wwwroot`, applies
SQLite migrations/startup preparation, and is the single target for local runs
and Playwright end-to-end execution through
`dotnet run --project backend/src/Postly.Api`.

The smallest architecture-proving slice delivers signup, sign-in, protected
routing, backend-hosted frontend assets, deterministic `DataSeed`, and the
create/read-own-post flow with ProblemDetails-style errors, consistent route
states, and the baseline testing/tooling gates. Later slices add profiles,
follow/unfollow, timeline composition, likes, and direct-post view while
keeping boundaries explicit and avoiding speculative abstraction.

## Technical Context

**Language/Version**: C# on .NET 10 (backend), TypeScript in strict mode (frontend)  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core with SQLite provider and migrations, ASP.NET Core authentication/cookie middleware, ASP.NET Core Identity password hasher, React, React Router, Vite, Vitest, React Testing Library, Playwright  
**Storage**: SQLite for the MVP application database, managed through EF Core migrations and deterministic non-production `DataSeed` preparation  
**Testing**: xUnit for backend unit/integration/contract tests, Vitest + React Testing Library for frontend unit/component tests, Playwright for critical end-to-end flows against the backend entry point  
**Target Platform**: Same-origin web app for desktop and mobile browsers; local development on macOS/Linux/Windows through a single backend entry point  
**Project Type**: Full-stack web application in a single repository  
**Interfaces/Contracts**: REST/JSON API under `/api`, same-origin SPA routes (`/`, `/signin`, `/signup`, `/u/:username`, `/posts/:postId`), cursor pagination for post collections, `application/problem+json` error responses, documented frontend screen and flow contracts  
**Error Handling Strategy**: Boundary validation on every request, domain/application errors mapped to ProblemDetails-style responses with stable error codes, 400/401/403/404/409 responses used consistently, frontend error mapping through a typed API client boundary with inline and dedicated route-state handling  
**UX Surfaces**: Signup, sign-in, sign-out redirect handling, protected home timeline, composer, post cards, direct post view, own profile, other-user profile, follow/unfollow and like/unlike interactions, backend-hosted SPA entry path  
**Performance Goals**: P95 API response under 250 ms for auth and single-post reads and under 400 ms for paginated timeline/profile reads on a single-node MVP dataset; initial pages deliver 20 posts per request; primary content/actions avoid horizontal scrolling on mobile and desktop  
**Constraints**: Same repository for frontend/backend, clear boundary between endpoint/application/persistence and UI/API client layers, minimal dependencies unless they reduce contract drift or quality risk, SQLite single-node limits accepted for MVP, no repository abstraction unless a concrete duplication/problem appears, frontend build output synchronized into backend `wwwroot` during `Postly.Api` pre-build, `dotnet run --project backend/src/Postly.Api` acts as the full local app entry point, no scope beyond approved MVP  
**Scale/Scope**: Single-node MVP for low-thousands of active users and tens of thousands of posts, with newest-first timelines/profiles, deterministic local setup, and five primary SPA routes/surfaces

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Design Gate

- [x] Clear module boundaries are documented, including ownership and allowed
      dependency direction.
      Backend is split into endpoint, application, persistence, and security
      concerns; frontend is split into app shell, feature modules, and a typed
      shared API boundary.
- [x] All affected contracts, validation rules, and predictable error outcomes
      are specified.
      The spec, frontend requirements, user flows, data model, and OpenAPI
      contract lock auth, authorization, state handling, seeded-data
      assumptions, and frontend/backend runtime behavior.
- [x] Automated tests are planned at the lowest useful level plus any impacted
      integration or contract boundaries.
      The plan includes backend unit/integration/contract tests, frontend
      component tests, and Playwright end-to-end flows against the backend
      entry point.
- [x] UX impact is documented, including loading, empty, success, and error
      states plus any intended pattern deviations.
      The spec and frontend requirements define route-level and mutation-level
      loading, empty, success, error, redirect, unavailable, and confirmation
      states.
- [x] The design is the simplest viable approach, and any breaking change
      includes migration or rollback notes.
      The design keeps one backend executable, one frontend app, direct EF
      Core usage, SQLite migrations, and MSBuild-based SPA asset sync without
      adding repositories, multiple services, or speculative shared packages.

### Post-Design Re-Check

- [x] Clear module boundaries are documented, including ownership and allowed
      dependency direction.
      Project structure, research decisions, and contracts keep dependency
      direction explicit: UI -> API client -> HTTP contracts -> API endpoints
      -> application handlers -> EF persistence.
- [x] All affected contracts, validation rules, and predictable error outcomes
      are specified.
      `contracts/openapi.yaml`, `data-model.md`, `frontend-requirements.md`,
      and `user-flows.md` together define requests, responses, seeded data,
      screen elements, and user-visible outcomes.
- [x] Automated tests are planned at the lowest useful level plus any impacted
      integration or contract boundaries.
      Quickstart and delivery phases include unit, integration, contract,
      component, and backend-hosted end-to-end coverage as non-optional
      quality gates.
- [x] UX impact is documented, including loading, empty, success, and error
      states plus any intended pattern deviations.
      The design preserves one shared shell, one reusable post-card contract,
      one profile layout contract, and consistent protected-access behavior.
- [x] The design is the simplest viable approach, and any breaking change
      includes migration or rollback notes.
      Schema evolution stays migration-first with SQLite, asset sync stays
      inside the backend project build/publish path, and no scope beyond the
      approved MVP is introduced.

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
└── src/
    └── Postly.Api/
        ├── Features/
        │   ├── Auth/
        │   │   ├── Application/
        │   │   ├── Contracts/
        │   │   └── Endpoints/
        │   ├── Posts/
        │   │   ├── Application/
        │   │   ├── Contracts/
        │   │   └── Endpoints/
        │   ├── Profiles/
        │   │   ├── Application/
        │   │   ├── Contracts/
        │   │   └── Endpoints/
        │   ├── Timeline/
        │   │   ├── Application/
        │   │   ├── Contracts/
        │   │   └── Endpoints/
        │   └── Shared/
        │       ├── Contracts/
        │       ├── Errors/
        │       └── Validation/
        ├── Persistence/
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
└── dist/                # build output synchronized into backend wwwroot

backend/tests/
├── Postly.Api.UnitTests/
├── Postly.Api.IntegrationTests/
└── Postly.Api.ContractTests/
```

**Structure Decision**: Use one backend executable project plus clear internal
layering instead of multiple class-library projects. The frontend remains one
Vite app organized by features, but the runtime path is backend-first:
frontend `dist` output is synchronized into `backend/src/Postly.Api/wwwroot`
through MSBuild targets equivalent to `SyncSpaAssetsToWwwroot` and
`IncludeSpaDistInPublish`.

## Delivery Phases

### Phase 0: Architecture-Proving Slice

1. Backend app bootstrap, SQLite connection, EF Core migrations, `DataSeed`,
   and ProblemDetails/error pipeline.
2. Backend-hosted SPA delivery from `wwwroot`, including MSBuild asset sync for
   local build and publish output.
3. Signup, sign-in, sign-out, session bootstrap, and protected-route handling.
4. Frontend authenticated shell, auth forms, typed API client, and route guard.
5. Create post + own timeline read path with loading, empty, success, and
   error states.
6. Baseline quality gates: backend tests, frontend tests, type-checking,
   linting, formatting, and one Playwright happy-path flow executed against
   `dotnet run --project backend/src/Postly.Api`.

### Phase 1: Social Graph and Timeline Composition

1. Profile read model for own/other profiles.
2. Follow and unfollow flows with consistent counts and self-follow rejection.
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
  simple. The backend serves built frontend files from `wwwroot`; local and CI
  end-to-end runs enter through `dotnet run --project backend/src/Postly.Api`
  instead of a separate frontend dev server.
- Synchronize frontend build artifacts into backend `wwwroot` during the
  `Postly.Api` pre-build step so local `dotnet run`, CI runs, and publish
  output all use the same backend-hosted frontend path.
- Implement the asset pipeline with MSBuild targets equivalent to
  `SyncSpaAssetsToWwwroot` for non-Release build sync and
  `IncludeSpaDistInPublish` for publish inclusion.
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
- Configure Playwright to boot and target the backend entry point so e2e
  coverage exercises the same static-file serving path used by real local runs;
  use best-effort readiness checks for backend HTTP availability, SPA entry
  availability, and `DataSeed` reset/preparation before tests start.

## Complexity Tracking

No constitutional violations or unjustified complexity are required for this
plan. Repository abstractions, distributed caching, background jobs, message
buses, separate SPA hosting services, public APIs beyond the MVP contract, and
shared domain packages are intentionally excluded until a concrete need
appears.
