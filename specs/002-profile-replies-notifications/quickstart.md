# Quickstart: Postly Round 2

## Purpose

This quickstart extends the MVP runbook for the Round 2 planning bundle. It is
intended to validate that the implementation matches the clarified spec before
task generation and delivery.

## Prerequisites

- .NET 10 SDK
- Node.js/npm compatible with the current Vite setup
- SQLite available through the backend runtime

## Repository Layout

- Backend runtime: `backend/src/Postly.Api`
- Frontend source: `frontend/`
- Backend tests: `backend/tests/`
- Frontend e2e tests: `frontend/tests/e2e/`
- Feature docs: `specs/002-profile-replies-notifications/`

## Local Setup

1. Restore backend dependencies.

```bash
dotnet restore Postly.sln
```

2. Install frontend dependencies.

```bash
cd frontend
npm ci
cd ..
```

3. Build the frontend for backend-hosted serving.

```bash
cd frontend
npm run build
cd ..
```

4. Start the backend entry point.

```bash
dotnet run --project backend/src/Postly.Api/Postly.Api.csproj
```

## Deterministic Seed Expectations

The non-production seed should provide:

- users `bob`, `alice`, and `charlie`
- Bob-owned profile data at `/u/bob`
- at least one Bob identity surface on `/`
- at least one conversation where Bob identity appears on `/posts/:postId`
- one available conversation with multi-page replies
- one unavailable-parent conversation with at least one visible reply
- unread notifications for Bob with:
  - one available destination
  - one unavailable destination
- enough timeline, profile-post, and conversation-reply data to hit both
  continuation retry and explicit end states

## Manual Validation Paths

### 1. Profile Editing and Identity Reflection

- Sign in as `bob`
- Open `/u/bob`
- Enter edit mode
- Save:
  - a valid trimmed display name
  - a valid bio or blank bio
  - a replacement avatar accepted by the backend
- Verify the updated identity appears on:
  - `/u/bob`
  - `/` where Bob identity is already shown
  - `/posts/:postId` where Bob identity is shown
- Attempt an invalid display name or bio and verify the saved identity does not
  change

### 2. Replies and Conversation States

- Open an available `/posts/:postId`
- Create a reply as `bob`
- Edit the reply as `bob`
- Delete the reply and verify a non-interactive placeholder remains
- Verify Bob cannot edit/delete another user's reply
- Open the seeded unavailable-parent conversation and verify:
  - the route stays open
  - the parent placeholder is shown
  - visible replies remain accessible

### 3. Notifications Lifecycle

- Open `/notifications`
- Verify unread and read rows are visually distinct
- Leave without opening any notification and verify unread state is unchanged
- Open one available-destination notification and verify:
  - navigation reaches the correct profile or conversation surface
  - only that selected notification becomes read
- Open one unavailable-destination notification and verify:
  - the app shows the notification-specific unavailable destination
  - only that selected notification becomes read

### 4. Automatic Continuation

- On `/`, scroll until the last currently visible item becomes the continuation
  point and verify more posts append automatically
- On `/u/alice`, force one continuation failure and verify:
  - currently visible posts remain visible
  - retry appears near the failure point
  - retry appends items successfully
- On `/posts/:postId`, continue through replies until `collection-end-state`
  appears and verify it is distinct from an initial empty state

Deterministic automation strategy for continuation failures:

- Frontend component tests should use a shared fetch-mock helper that rejects
  the next continuation request once and then allows the retry request to
  succeed.
- Playwright retry scenarios should use route interception to fail the first
  matching continuation request once and allow subsequent requests through.

## Planned Automated Validation

### Backend unit tests

```bash
dotnet test backend/tests/Postly.Api.UnitTests/Postly.Api.UnitTests.csproj --configuration Release --no-build
```

Focus:

- profile validation and owner-only authorization
- avatar fallback projection rules
- reply validation and placeholder transitions
- notification creation and selected-item read transitions
- cursor helper and continuation ordering behavior

### Backend contract tests

```bash
dotnet test backend/tests/Postly.Api.ContractTests/Postly.Api.ContractTests.csproj --configuration Release --no-build
```

Focus:

- profile update and avatar endpoints
- conversation and reply endpoints
- notification list and open endpoints
- continuation response shape and ProblemDetails outcomes

### Backend integration tests

```bash
dotnet test backend/tests/Postly.Api.IntegrationTests/Postly.Api.IntegrationTests.csproj --configuration Release
```

Focus:

- profile identity reflection across read models
- reply create/edit/delete plus unavailable-parent reads
- notification generation and destination resolution
- retry-safe continuation behavior

### Frontend component tests

```bash
cd frontend
npm run test:ci
cd ..
```

Focus:

- profile edit state variants
- conversation placeholder/unavailable states
- notifications list and destination-open transitions
- shared continuation controller behavior, including one-shot fetch-mock
  continuation failure injection for retry coverage

### Playwright e2e tests

```bash
cd frontend
npm run test:e2e
cd ..
```

Focus:

- all primary and recovery flows from `user-flows.md`, with continuation retry
  scenarios driven by one-shot route interception rather than backend seed
  toggles

## Migration and Rollback Notes

- Round 2 persistence changes should ship through EF Core migrations committed
  with the implementation.
- Roll-forward remains the default recovery strategy.
- Any migration affecting existing posts or identity projection should document
  rollback constraints in the implementation PR before merge.
