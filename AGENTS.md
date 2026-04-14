# Postly Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-10

## Active Technologies

- C# on .NET 10 (backend), TypeScript in strict mode (frontend) + ASP.NET Core Minimal APIs, EF Core with SQLite provider and migrations, ASP.NET Core authentication/cookie middleware, ASP.NET Core Identity password hasher, React, React Router, Vite, Vitest, React Testing Library, Playwright (001-microblog-mvp)

## Project Structure

```text
backend/
frontend/
tests/
```

## Frequent Commands

- `cd frontend && npm ci`
- `cd frontend && npm run build`
- `cd frontend && npm run lint`
- `cd frontend && npm run test:ci`
- `cd frontend && npm run test:e2e`
- `dotnet restore Postly.sln`
- `dotnet build Postly.sln --configuration Release --no-restore`
- `dotnet test backend/tests/Postly.Api.UnitTests/Postly.Api.UnitTests.csproj --configuration Release --no-build`
- `dotnet test backend/tests/Postly.Api.ContractTests/Postly.Api.ContractTests.csproj --configuration Release --no-build`
- `dotnet test backend/tests/Postly.Api.IntegrationTests/Postly.Api.IntegrationTests.csproj --configuration Release`
- `dotnet publish backend/src/Postly.Api/Postly.Api.csproj --configuration Release`

## Code Style

C# on .NET 10 (backend), TypeScript in strict mode (frontend): Follow standard conventions

## Recent Changes

- 001-microblog-mvp: Added C# on .NET 10 (backend), TypeScript in strict mode (frontend) + ASP.NET Core Minimal APIs, EF Core with SQLite provider and migrations, ASP.NET Core authentication/cookie middleware, ASP.NET Core Identity password hasher, React, React Router, Vite, Vitest, React Testing Library, Playwright

<!-- MANUAL ADDITIONS START -->
## E2E Testing Guidance

- Prefer `data-testid` for app-specific UI targeting in Playwright tests.
- Prefer stable accessible roles and names when the element is naturally unique and user-facing.
- Avoid broad `getByText(...)` selectors for dynamic or repeated values.
- Avoid CSS class selectors for test behavior unless no stable semantic or test id exists.
<!-- MANUAL ADDITIONS END -->
