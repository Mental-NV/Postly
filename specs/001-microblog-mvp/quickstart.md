# Quickstart: Postly Microblog MVP

## Prerequisites

- .NET 10 SDK
- Node.js 22 LTS or newer
- npm 10 or newer
- SQLite available through the bundled EF Core provider; standalone `sqlite3`
  CLI is optional

## Repository Layout

- Backend: `backend/src/Postly.Api`
- Backend tests: `backend/tests/*`
- Frontend: `frontend`
- End-to-end tests: `frontend/tests/e2e`

## Initial Setup

1. Restore .NET tools and NuGet packages:

```bash
dotnet tool restore
dotnet restore backend
```

2. Install frontend dependencies:

```bash
npm install --prefix frontend
```

3. Create or confirm the local database connection string:

```bash
export ConnectionStrings__Postly="Data Source=postly.db"
```

4. Apply EF Core migrations:

```bash
dotnet ef database update \
  --project backend/src/Postly.Api \
  --startup-project backend/src/Postly.Api
```

## Run the App

1. Start the backend API:

```bash
dotnet run --project backend/src/Postly.Api
```

2. In a second terminal, start the frontend dev server:

```bash
npm run dev --prefix frontend
```

3. Open the Vite dev URL and use the UI to:
   - sign up
   - sign in
   - create a post
   - open your own profile
   - follow another user from a visible post/profile
   - like/unlike a post

## Expected Local Development Behavior

- Vite proxies `/api` to the backend so auth cookies stay same-site in local
  development.
- Protected routes redirect signed-out users to sign-in and then return them to
  the originally requested destination.
- SQLite is the only local persistence dependency; no Docker or external
  service is required for the MVP.

## Quality Gates

Run these before opening a PR:

```bash
dotnet format backend
dotnet test backend/tests/Postly.Api.UnitTests
dotnet test backend/tests/Postly.Api.IntegrationTests
dotnet test backend/tests/Postly.Api.ContractTests

npm run lint --prefix frontend
npm run typecheck --prefix frontend
npm run test --prefix frontend
npm run e2e --prefix frontend
```

## Critical Manual Smoke Flows

1. Open signup, create an account, and confirm you land signed in on the home
   timeline.
2. Sign out, revisit a protected URL, and confirm redirect back through sign-in.
3. Create, edit, and delete your own post.
4. Open another user's profile from a visible post, follow/unfollow them, and
   verify home timeline composition updates.
5. Like/unlike the same post across timeline/profile/direct-post surfaces.
6. Confirm loading, empty, success, retry, and not-available states appear for
   the supported flows.

## MVP-Specific Operational Notes

- The backend should return ProblemDetails-style JSON for recoverable errors.
- SQLite schema must always be created/updated through EF Core migrations.
- Logging should include request trace identifiers so UI/API failures can be
  correlated during local debugging and CI runs.
