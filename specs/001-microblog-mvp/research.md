# Research: Postly Microblog MVP

## Decision 1: Keep the application as a same-repo, same-origin full-stack web app

- **Decision**: Use one repository with separate `backend/` and `frontend/`
  apps. In production, ASP.NET Core serves the API and static frontend assets;
  in development, Vite proxies `/api` to the backend.
- **Rationale**: This keeps frontend/backend boundaries explicit without
  introducing distributed deployment concerns into the MVP. Same-origin
  deployment simplifies cookie-based auth, protected-route redirects, and local
  debugging.
- **Alternatives considered**:
  - Separate repositories and deployments: rejected because it adds release and
    environment complexity before the MVP proves product value.
  - Monolithic server-rendered UI: rejected because the user requested a React +
    TypeScript + Vite frontend and a clear API client boundary.

## Decision 2: Use one ASP.NET Core app with layered folders instead of multiple backend projects

- **Decision**: Implement the backend as a single `Postly.Api` project with
  feature folders for endpoints/contracts/application logic and a dedicated
  persistence folder for EF Core.
- **Rationale**: This preserves the required separation between endpoint,
  application, and persistence concerns while avoiding the ceremony of multiple
  class-library projects for a still-small MVP. Feature folders make the code
  easier to evolve slice by slice.
- **Alternatives considered**:
  - Separate `Api`, `Application`, and `Infrastructure` projects: rejected for
    the MVP because it increases project-count and dependency management without
    solving a concrete problem yet.
  - Pure vertical-slice folders with DbContext access anywhere: rejected
    because it weakens the persistence boundary and makes review harder.

## Decision 3: Use custom cookie-backed sessions with ASP.NET password hashing

- **Decision**: Use username/password authentication with secure same-site
  cookies and a persisted session table. Hash passwords with the ASP.NET Core
  Identity password hasher, but do not adopt the full Identity framework.
- **Rationale**: This matches the spec's username-based auth, keeps dependencies
  smaller than full Identity, supports explicit sign-out and stale-session
  invalidation, and works well with same-origin protected routes.
- **Alternatives considered**:
  - Full ASP.NET Core Identity: rejected because it adds more abstraction and
    schema surface area than the MVP needs.
  - JWT stored in browser storage: rejected because it complicates revocation
    and increases XSS risk without helping the MVP.

## Decision 4: Standardize on REST/JSON contracts with cursor pagination and ProblemDetails

- **Decision**: Expose Minimal API endpoints under `/api`, use consistent JSON
  DTOs, return `application/problem+json` for failures, and paginate timeline
  and profile post collections with `cursor` + `limit`.
- **Rationale**: The approved spec is naturally resource-oriented, and
  cursor-based pagination fits newest-first post streams better than offset
  pagination. ProblemDetails gives a predictable frontend error contract and
  clean REST semantics for 400/401/403/404/409 outcomes.
- **Alternatives considered**:
  - Offset pagination: rejected because feed consistency degrades as writes
    happen between requests.
  - GraphQL: rejected because it adds unnecessary tooling and runtime
    complexity for a narrow MVP surface.

## Decision 5: Use EF Core DbContext directly with explicit query handlers

- **Decision**: Keep persistence simple: one EF Core DbContext, entity
  configurations, migrations, and direct DbContext usage from application
  handlers. Do not add a repository abstraction.
- **Rationale**: The constitution explicitly warns against unnecessary
  abstraction. Direct DbContext usage is easier to trace in review, reduces
  boilerplate, and still allows focused tests and query projections.
- **Alternatives considered**:
  - Generic repositories/unit-of-work wrappers: rejected because EF Core
    already provides those concepts and wrappers would mostly duplicate them.
  - Hand-written SQL for all reads/writes: rejected because it slows delivery
    and adds complexity before query hotspots are proven.

## Decision 6: Build the frontend around feature modules and a typed API client boundary

- **Decision**: Organize the Vite app into `app`, `features`, and `shared`
  modules. All server communication goes through a typed API client layer; UI
  components do not perform ad hoc fetches.
- **Rationale**: This keeps contract usage explicit, makes loading/error/success
  handling reusable across screens, and supports feature-oriented growth without
  introducing a heavyweight state-management framework up front.
- **Alternatives considered**:
  - A global state library from the start: rejected because the MVP state graph
    is still manageable with local state, route loaders/actions, and focused
    hooks.
  - Scattering fetch logic inside components: rejected because it weakens the
    UI/API boundary and makes consistent state/error handling harder.

## Decision 7: Make tests and static quality gates part of the architecture, not polish

- **Decision**: Use xUnit for backend unit and integration tests, WebApplicationFactory-based contract tests for the API, Vitest + React Testing Library for frontend logic/components, and Playwright for critical full-stack flows. Make linting, formatting, and type-checking required gates.
- **Rationale**: The constitution makes testing mandatory, and Postly's risk
  areas are contracts, ownership/visibility rules, and UI state transitions.
  These layers are best protected by multiple focused test levels rather than
  only end-to-end coverage.
- **Alternatives considered**:
  - End-to-end tests only: rejected because failures would be slower and harder
    to localize.
  - No contract tests: rejected because API/frontend drift is a high risk in a
    same-repo full-stack app with strict typing expectations.

## Decision 8: Treat MVP security, performance, and operations as narrow but explicit concerns

- **Decision**: Add built-in rate limiting for auth/write endpoints, use secure
  cookie settings, maintain SQLite indexes for timeline and relationship reads,
  include trace IDs in ProblemDetails, and keep local setup deterministic via EF
  migrations.
- **Rationale**: These controls are proportionate to an MVP social web app.
  They reduce predictable operational and abuse risks without creating new
  product scope such as moderation tooling or distributed infrastructure.
- **Alternatives considered**:
  - No rate limiting or structured logging: rejected because auth/write abuse
    and low-debuggability are avoidable risks even at MVP scale.
  - Early distributed cache or queue adoption: rejected because the scale does
    not justify the added complexity yet.
