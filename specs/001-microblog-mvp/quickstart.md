# Quickstart: Postly Microblog MVP

## Purpose

This quickstart describes the expected local development and verification path
for the MVP design artifacts. It assumes the backend project is the single app
entry point and the frontend is served from backend `wwwroot`.

## Prerequisites

- .NET 10 SDK
- Node.js and npm compatible with the frontend toolchain
- SQLite available through the application runtime

## Expected Repository Layout

- Backend project: `backend/src/Postly.Api`
- Frontend app source: `frontend/`
- Feature artifacts: `specs/001-microblog-mvp/`

## Local Workflow

1. Build the frontend assets so the backend can serve them from `wwwroot`.
2. Run the backend entry point:

   ```bash
   dotnet run --project backend/src/Postly.Api
   ```

3. Confirm startup applies required migrations or equivalent database
   preparation and makes the SPA entry point available from the backend host.
4. Open the backend base URL in a browser and verify the public auth routes
   load through backend-hosted static files.

## Seeded Non-Production Data

- The local non-production environment should prepare `DataSeed` as described in
  `data-model.md`.
- At minimum, seeded users `alice` and `bob` exist with deterministic test
  credentials.
- Seed preparation should restore the documented baseline follow and like state
  before Playwright execution.

## Manual Verification Path

1. Open `/signup` and confirm account creation requirements and validation
   messages are present.
2. Open `/signin` and confirm redirect messaging appears for protected-route
   access.
3. Sign in and confirm the home timeline shell, composer, and empty or feed
   state load correctly.
4. Navigate to `/u/alice` and confirm profile identity, follow state, and post
   list behavior.
5. Open a direct post URL and confirm single-post presentation and unavailable
   fallback behavior.

## Automated Verification Expectations

- Backend unit, integration, and contract tests cover validation, auth,
  ownership, and API contract behavior.
- Frontend unit/component tests cover route states, form behavior, and reusable
  UI contracts.
- Playwright launches the app through:

  ```bash
  dotnet run --project backend/src/Postly.Api
  ```

- Playwright should wait for backend readiness, backend-served SPA availability,
  and `DataSeed` preparation before browser scenarios start.

## Build and Publish Expectations

- Local non-Release builds synchronize frontend `dist` assets into backend
  `wwwroot` through an MSBuild target equivalent to `SyncSpaAssetsToWwwroot`.
- Publish output includes SPA assets under `wwwroot` through a publish-stage
  target equivalent to `IncludeSpaDistInPublish`.

## Notes

- Local runtime and test database files are generated artifacts and should
  remain ignored by Git.
- The MVP excludes search, messaging, notifications, moderation, replies, and
  media upload; local verification should not assume those features exist.
