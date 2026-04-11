# Quickstart: Postly Microblog MVP

## Purpose

This quickstart documents the current local development, validation, and
operational expectations for the MVP as implemented in this repository. The
backend ASP.NET Core app is the single runtime entry point, and the frontend is
served from backend `wwwroot`.

## Prerequisites

- .NET 10 SDK
- Node.js and npm compatible with the frontend toolchain
- SQLite available through the application runtime

## Expected Repository Layout

- Backend project: `backend/src/Postly.Api`
- Frontend app source: `frontend/`
- Backend test projects: `backend/tests/`
- Frontend e2e tests: `frontend/tests/e2e/`
- Feature artifacts: `specs/001-microblog-mvp/`

## Local Startup Workflow

1. Restore/install dependencies if this is the first run:

   ```bash
   dotnet restore
   cd frontend
   npm install
   cd ..
   ```

2. Build frontend assets before starting the backend-hosted app:

   ```bash
   cd frontend
   npm run build
   cd ..
   ```

   Expected result: frontend `dist` assets are generated successfully so the
   backend can sync them into `wwwroot`.

3. Start the full application through the backend entry point:

   ```bash
   dotnet run --project backend/src/Postly.Api
   ```

   Expected result: the backend starts, static frontend assets are served from
   `wwwroot`, and Development startup applies EF Core migrations plus
   non-production `DataSeed`.

4. Open the backend base URL in a browser and verify the public routes load
   through the backend host:

   - `/signup`
   - `/signin`

5. Sign in with a seeded account and verify the protected surfaces:

   - Username: `bob`
   - Password: `TestPassword123`

   Manual verification path:
   - Open `/signin` and verify protected-route return messaging is readable.
   - Sign in as `bob` and confirm the timeline shell and composer load.
   - Navigate to `/u/alice` and confirm profile identity, bio, counts, and post list.
   - Open a direct post permalink and confirm the single-post surface renders.
   - Open `/posts/999999` while signed in and confirm the unavailable state links back home.

## Seeded Non-Production Data

- `DataSeed` creates deterministic non-production users `bob`, `alice`, and `charlie`.
- Baseline seeded posts include one visible post for `alice` and one owned post for `bob`.
- The codebase includes `DataSeed.ResetAsync`, but the runtime app does not expose a
  public reset endpoint or command; reset is currently used through test harnesses and direct DbContext access.
- Startup seeding currently runs only in `Development` and only when the database has no users.

## Automated Validation

The following commands were re-run during Phase 8 and are the current verified
validation path for the implemented MVP:

1. Backend unit coverage for post interaction logic:

   ```bash
   dotnet test backend/tests/Postly.Api.UnitTests/Postly.Api.UnitTests.csproj --filter PostInteractionHandlerTests
   ```

2. Backend contract coverage for direct post and likes:

   ```bash
   dotnet test backend/tests/Postly.Api.ContractTests/Postly.Api.ContractTests.csproj --filter DirectPostAndLikesContractsTests
   ```

3. Backend integration coverage for direct-post and like flows:

   ```bash
   dotnet test backend/tests/Postly.Api.IntegrationTests/Postly.Api.IntegrationTests.csproj --filter DirectPostAndLikesFlowTests
   ```

4. Frontend shared and feature component coverage for route states and post-card behavior:

   ```bash
   cd frontend
   npm run test:ci -- src/shared/test/accessibility-and-copy.test.tsx src/features/timeline/__tests__/TimelinePage.test.tsx src/features/profiles/__tests__/ProfilePage.test.tsx src/features/posts/__tests__/DirectPostPage.test.tsx src/features/posts/__tests__/direct-post-and-likes-ui.test.tsx
   cd ..
   ```

5. Frontend production build:

   ```bash
   cd frontend
   npm run build
   cd ..
   ```

6. Backend-hosted Playwright verification for likes and direct-post availability:

   ```bash
   cd frontend
   npm run test:e2e -- tests/e2e/us5-likes-and-direct-post.spec.ts
   cd ..
   ```

Expected result for each command: exit code `0`.

## Build, Publish, and Compatibility Notes

- The backend is the single runtime entry point. Running a standalone frontend dev server is not part of the documented MVP verification path.
- Local non-Release builds sync `frontend/dist` into backend `wwwroot` via the MSBuild target `SyncSpaAssetsToWwwroot`.
- Publish output includes frontend static assets under `wwwroot` via `IncludeSpaDistInPublish`.
- Because the backend serves static frontend files, a fresh frontend build is required before expecting local backend-hosted UI changes to appear.
- Local runtime and test SQLite database files are generated artifacts and must remain uncommitted.

## Schema, Migration, and Rollback Notes

- Schema changes are migration-first: every database shape change should ship with an EF Core migration committed to the repository.
- Roll-forward is the default strategy for the MVP. Prefer applying a corrective forward migration over attempting ad hoc database edits.
- Any destructive schema change should include explicit rollback notes in the implementation PR or companion documentation before it ships.
- Current startup behavior applies migrations automatically only in `Development`; this is convenient for local work and automated verification, but deployment procedures should not assume production startup performs the same automatic preparation.
- Seed behavior is deterministic for non-production use, but startup seeding is additive-only. If the local database already contains users, startup will not reset it back to the baseline seed state.

## Performance Review

This Phase 8 review is code-based rather than benchmark-driven.

- Startup path:
  `Program.cs` keeps the production request pipeline simple, but Development startup performs migrations plus seed checks on cold start. This is acceptable for local and test environments and should remain explicit in operational expectations.
- Timeline and profile reads:
  `GetTimelineHandler` and `GetProfileHandler` currently materialize candidate post sets into memory with `ToListAsync()` before applying cursor filtering, ordering, and page size. This is the primary performance risk in the current MVP because work scales with all candidate posts, not just the requested page.
- Viewer context enrichment:
  `PostSummaryFactory` computes like counts and liked-by-viewer state with two set-based queries over the visible post IDs. That is acceptable for the current page size once the visible page has been determined.
- Direct post reads:
  `GetPostHandler` performs one single-post lookup plus two lightweight aggregate checks for like count and liked state. This is low risk compared with timeline/profile paths.
- Index baseline:
  Current indexes on posts, likes, follows, sessions, and normalized usernames are a reasonable MVP baseline and should remain the first line of optimization before adding denormalized counters or cache layers.

Recommended next performance step if the dataset grows:

1. Move timeline/profile cursor filtering, ordering, and `Take(PageSize + 1)` into SQL rather than loading the full candidate post set first.
2. Keep the existing indexes and re-evaluate query shape before introducing caching or denormalized aggregates.
3. Treat caches, background jobs, or counter tables as post-MVP complexity unless measured evidence justifies them.

## Known Gaps

- Repo-wide ESLint remains a separate baseline-cleanup problem and is not currently the best signal for MVP feature verification.
- The MVP excludes search, messaging, notifications, moderation, replies, comments, hashtags, trending topics, profile editing after signup, and media upload. Local verification should not assume those features exist.
