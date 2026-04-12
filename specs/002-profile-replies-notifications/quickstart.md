# Quickstart: Postly Round 2

## Purpose

This quickstart extends the MVP runbook for the Round 2 planning bundle. The
backend ASP.NET Core app remains the single runtime entry point, the frontend
still builds separately and is served from backend `wwwroot`, and Round 2
verification adds profile editing, conversation replies, notifications, and
automatic continuation states.

## Prerequisites

- .NET 10 SDK
- Node.js and npm compatible with the existing Vite toolchain
- SQLite available through the application runtime

## Expected Repository Layout

- Backend project: `backend/src/Postly.Api`
- Frontend app source: `frontend/`
- Backend tests: `backend/tests/`
- Playwright tests: `frontend/tests/e2e/`
- Feature docs: `specs/002-profile-replies-notifications/`

## Local Startup Workflow

1. Restore and install dependencies:

   ```bash
   dotnet restore Postly.sln
   cd frontend
   npm ci
   cd ..
   ```

2. Build frontend assets before starting the backend-hosted app:

   ```bash
   cd frontend
   npm run build
   cd ..
   ```

3. Start the full application through the backend entry point:

   ```bash
   dotnet run --project backend/src/Postly.Api/Postly.Api.csproj
   ```

4. Open the backend base URL and verify the app is being served by the backend:

   - `/signin`
   - `/`
   - `/u/alice`
   - `/posts/{seededPostId}`
   - `/notifications`

## Seeded Non-Production Data Expectations

Round 2 assumes the deterministic non-production seed now includes:

- users `bob`, `alice`, and `charlie`
- at least one Bob-authored profile and conversation identity surface
- at least one available conversation with replies
- at least one unavailable-parent conversation scenario
- unread notifications for `bob` covering follow, like, and reply events
- enough timeline, profile, and conversation items to exercise continuation
  beyond the initial page

## Manual Validation Paths

### 1. Profile Edit and Avatar Replacement

- Sign in as `bob`
- Open `/u/bob`
- Enter edit mode, update display name and bio, replace the avatar, and save
- Verify the updated identity is visible on:
  - `/u/bob`
  - `/`
  - a conversation route under `/posts/:postId` where `bob` appears

### 2. Reply and Conversation Management

- Open a seeded conversation at `/posts/:postId`
- Create a reply as `bob`
- Edit that reply and verify the edited state
- Delete that reply and verify a non-interactive placeholder remains visible
- Open a seeded unavailable-parent conversation route and verify the route stays
  open with a placeholder target plus any still-visible replies

### 3. Notifications Lifecycle

- Open `/notifications`
- Confirm unread items are visible
- Leave the list without opening an item and verify unread state remains
- Open one notification and verify:
  - the app navigates to the relevant profile or conversation route
  - only the opened notification becomes read
- Open a notification whose target is unavailable and verify the destination
  still resolves to a clear unavailable state

### 4. Automatic Continuation

- On `/`, scroll until the last currently visible timeline item becomes the
  continuation point and verify more content loads automatically
- On `/u/alice`, trigger a continuation failure through the test harness and
  verify visible content remains plus a retry action appears
- On `/posts/:postId`, continue loading replies until the explicit end-of-list
  state appears

## Planned Automated Validation

The intended Round 2 validation stack remains layered:

1. Backend unit tests for reply rules, notification generation, profile
   validation, and cursor helpers

   ```bash
   dotnet test backend/tests/Postly.Api.UnitTests/Postly.Api.UnitTests.csproj --configuration Release --no-build
   ```

2. Backend contract tests for new or changed endpoints

   ```bash
   dotnet test backend/tests/Postly.Api.ContractTests/Postly.Api.ContractTests.csproj --configuration Release --no-build
   ```

3. Backend integration tests for end-to-end API behavior across replies,
   notifications, and profile edits

   ```bash
   dotnet test backend/tests/Postly.Api.IntegrationTests/Postly.Api.IntegrationTests.csproj --configuration Release
   ```

4. Frontend component and route-state tests

   ```bash
   cd frontend
   npm run test:ci
   cd ..
   ```

5. Backend-hosted Playwright flows for Round 2 user stories

   ```bash
   cd frontend
   npm run test:e2e
   cd ..
   ```

## Build, Publish, and Compatibility Notes

- The backend remains the single runtime entry point and serves the built SPA
  from `backend/src/Postly.Api/wwwroot`.
- A fresh frontend build is still required before expecting backend-hosted UI
  changes to appear locally.
- No separate frontend runtime or new infrastructure tier is introduced in
  Round 2.

## Schema and Rollback Notes

- All persistence changes for replies, avatar state, and notifications should
  ship as EF Core migrations committed with the implementation.
- Roll-forward remains the default fix strategy.
- Any destructive change to existing data shape should include explicit rollback
  notes in the implementation PR before shipping.
